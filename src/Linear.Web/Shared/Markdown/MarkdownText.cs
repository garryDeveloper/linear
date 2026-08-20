using System.Text;

namespace Linear.Web.Shared.Markdown;

/// <summary>
/// Codificación de texto para el HTML que produce el renderizador.
/// </summary>
/// <remarks>
/// Se escapan exactamente los cinco caracteres que tienen significado en HTML. Son toda la
/// superficie de ataque en texto y en valores de atributo: sin <c>&lt;</c> no se abre una
/// etiqueta, sin comillas no se cierra un atributo, y sin <c>&amp;</c> no se cuela una
/// entidad que el navegador vuelva a decodificar.
///
/// No se usa <c>WebUtility.HtmlEncode</c> porque además convierte todo lo que no sea ASCII
/// en entidades numéricas: en una aplicación en castellano eso llena la salida de
/// <c>&amp;#225;</c> donde había una "á". Se ve igual en pantalla, pero el HTML se vuelve
/// ilegible al depurar y pesa bastante más sin ganar nada en seguridad — los acentos no
/// tienen ningún significado en HTML.
/// </remarks>
internal static class MarkdownText
{
    public static string Encode(string text)
    {
        // Se recorre primero para no reservar un StringBuilder cuando no hay nada que escapar,
        // que es el caso más común.
        if (!NeedsEncoding(text))
        {
            return text;
        }

        var builder = new StringBuilder(text.Length + 16);

        foreach (var character in text)
        {
            builder.Append(character switch
            {
                // El '&' va primero por definición: si se escapara después, volvería a
                // escapar los '&' de las entidades ya escritas.
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                '\'' => "&#39;",
                _ => character.ToString()
            });
        }

        return builder.ToString();
    }

    private static bool NeedsEncoding(string text)
    {
        foreach (var character in text)
        {
            if (character is '&' or '<' or '>' or '"' or '\'')
            {
                return true;
            }
        }

        return false;
    }
}
