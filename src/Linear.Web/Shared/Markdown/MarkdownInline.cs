using System.Text;

namespace Linear.Web.Shared.Markdown;

/// <summary>
/// Convierte el contenido de una línea —negrita, itálica, código y enlaces— en HTML.
/// </summary>
/// <remarks>
/// Todo el texto pasa por <see cref="MarkdownText.Encode"/> antes de salir. Las únicas
/// etiquetas del resultado son las que este código decide emitir: lo que el usuario escriba
/// como HTML se codifica y se ve como texto literal, no se interpreta.
///
/// Eso es lo que hace que no haga falta sanear después. Un sanitizador recibe HTML ya armado
/// y trata de sacarle lo peligroso, y ahí es donde se le escapan cosas; acá el HTML peligroso
/// nunca llega a existir.
/// </remarks>
internal static class MarkdownInline
{
    public static string Render(string text)
    {
        var builder = new StringBuilder(text.Length);
        var literal = new StringBuilder();

        var index = 0;

        while (index < text.Length)
        {
            // El escape de Markdown: una barra invertida vuelve literal al carácter que sigue.
            if (text[index] == '\\' && index + 1 < text.Length && IsEscapable(text[index + 1]))
            {
                literal.Append(text[index + 1]);
                index += 2;
                continue;
            }

            var consumed = TryRead(text, index, out var html);

            if (consumed == 0)
            {
                literal.Append(text[index]);
                index++;
                continue;
            }

            Flush(builder, literal);
            builder.Append(html);
            index += consumed;
        }

        Flush(builder, literal);

        return builder.ToString();
    }

    /// <summary>
    /// Intenta leer una construcción en <paramref name="start"/>. Devuelve cuántos caracteres
    /// consumió, o 0 si ahí no empieza ninguna.
    /// </summary>
    /// <remarks>
    /// El código va primero a propósito: lo que está entre acentos graves es literal, así que
    /// un <c>**</c> ahí adentro no abre una negrita. Después los enlaces, y al final los
    /// énfasis, del delimitador más largo al más corto para que <c>**</c> gane sobre <c>*</c>.
    /// </remarks>
    private static int TryRead(string text, int start, out string html)
    {
        html = string.Empty;

        return text[start] switch
        {
            '`' => TryReadCode(text, start, out html),
            '[' => TryReadLink(text, start, out html),
            '*' or '_' => TryReadEmphasis(text, start, out html),
            _ => 0
        };
    }

    private static int TryReadCode(string text, int start, out string html)
    {
        html = string.Empty;

        var closing = text.IndexOf('`', start + 1);

        if (closing < 0)
        {
            return 0;
        }

        var content = text[(start + 1)..closing];

        html = $"<code>{MarkdownText.Encode(content)}</code>";

        return closing - start + 1;
    }

    private static int TryReadLink(string text, int start, out string html)
    {
        html = string.Empty;

        var closingText = text.IndexOf(']', start + 1);

        if (closingText < 0 || closingText + 1 >= text.Length || text[closingText + 1] != '(')
        {
            return 0;
        }

        var closingUrl = text.IndexOf(')', closingText + 2);

        if (closingUrl < 0)
        {
            return 0;
        }

        var label = text[(start + 1)..closingText];
        var url = text[(closingText + 2)..closingUrl];
        var safeUrl = MarkdownText.Encode(MarkdownUrl.Sanitize(url));

        // rel="noopener noreferrer" junto con target: sin noopener, la página abierta puede
        // redirigir a la que la abrió a través de window.opener.
        html = $"""<a href="{safeUrl}" target="_blank" rel="noopener noreferrer">{Render(label)}</a>""";

        return closingUrl - start + 1;
    }

    /// <summary>
    /// Lee negrita o itálica.
    /// </summary>
    /// <remarks>
    /// El cierre se busca como la próxima aparición del mismo delimitador, que resuelve bien
    /// el anidado normal —<c>**fuerte con *suave* adentro**</c>— pero no la carrera de tres
    /// asteriscos pegados: en <c>**a *b***</c> el cierre de la itálica y el de la negrita
    /// comparten caracteres, y desenredarlo pide el algoritmo de carreras de delimitadores de
    /// CommonMark, bastante más grande que todo este renderizador. Ahí la itálica interna
    /// queda como texto. Es una degradación visible y acotada, no un riesgo: el resultado
    /// sigue estando codificado.
    /// </remarks>
    private static int TryReadEmphasis(string text, int start, out string html)
    {
        html = string.Empty;

        var marker = text[start];
        var isStrong = start + 1 < text.Length && text[start + 1] == marker;
        var delimiter = isStrong ? new string(marker, 2) : marker.ToString();
        var contentStart = start + delimiter.Length;

        if (contentStart >= text.Length)
        {
            return 0;
        }

        var closing = text.IndexOf(delimiter, contentStart, StringComparison.Ordinal);

        if (closing < 0)
        {
            return 0;
        }

        var content = text[contentStart..closing];

        // Un delimitador sin nada adentro —"**" suelto— no es énfasis: se deja como texto.
        if (content.Length == 0)
        {
            return 0;
        }

        var tag = isStrong ? "strong" : "em";

        html = $"<{tag}>{Render(content)}</{tag}>";

        return closing - start + delimiter.Length;
    }

    private static void Flush(StringBuilder builder, StringBuilder literal)
    {
        if (literal.Length > 0)
        {
            builder.Append(MarkdownText.Encode(literal.ToString()));
            literal.Clear();
        }
    }

    private static bool IsEscapable(char character) =>
        character is '\\' or '`' or '*' or '_' or '[' or ']' or '(' or ')' or '#' or '|' or '>';
}
