using System.Text;

namespace Linear.Web.Shared.Markdown;

/// <summary>
/// Convierte el Markdown que escriben los usuarios en HTML seguro.
/// </summary>
/// <remarks>
/// Está escrito a mano y no apoyado en una librería porque el proyecto no incorpora
/// dependencias nuevas (CLAUDE.md). La restricción termina empujando a un diseño más seguro
/// que el habitual: en vez de armar HTML y después pasarle un sanitizador —que recibe el
/// daño ya hecho y trata de quitarlo—, acá **el HTML peligroso nunca llega a existir**. Todo
/// texto se codifica al salir y las únicas etiquetas del resultado son las que este código
/// decide emitir. El único dato que va a un atributo es la dirección de un enlace, y esa se
/// valida aparte en <see cref="MarkdownUrl"/>.
///
/// Cubre la sintaxis que pide la task 012: títulos, negrita, itálica, enlaces, listas con y
/// sin orden, código en línea y en bloque, citas y tablas. No pretende ser CommonMark
/// completo: lo que no reconoce queda como texto, que es la forma segura de fallar.
/// </remarks>
public static class MarkdownRenderer
{
    /// <summary>Nivel máximo de título, como en Markdown.</summary>
    private const int MaxHeadingLevel = 6;

    /// <summary>
    /// Renderiza el documento. Un texto vacío da una cadena vacía, no un párrafo en blanco.
    /// </summary>
    public static string Render(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        // Se normalizan los finales de línea para no tener que contemplar \r\n en cada regla.
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

        var builder = new StringBuilder();
        var index = 0;

        while (index < lines.Length)
        {
            index = RenderBlock(lines, index, builder);
        }

        return builder.ToString();
    }

    /// <summary>Renderiza el bloque que empieza en <paramref name="start"/> y devuelve la línea siguiente.</summary>
    private static int RenderBlock(string[] lines, int start, StringBuilder builder)
    {
        var line = lines[start];

        if (string.IsNullOrWhiteSpace(line))
        {
            return start + 1;
        }

        if (IsFence(line))
        {
            return RenderCodeBlock(lines, start, builder);
        }

        if (TryReadHeading(line, out var level, out var headingText))
        {
            builder.Append($"<h{level}>{MarkdownInline.Render(headingText)}</h{level}>");
            return start + 1;
        }

        if (IsQuote(line))
        {
            return RenderQuote(lines, start, builder);
        }

        if (IsTableStart(lines, start))
        {
            return RenderTable(lines, start, builder);
        }

        if (IsUnorderedItem(line))
        {
            return RenderList(lines, start, builder, ordered: false);
        }

        if (IsOrderedItem(line))
        {
            return RenderList(lines, start, builder, ordered: true);
        }

        return RenderParagraph(lines, start, builder);
    }

    // ---- bloques -------------------------------------------------------------------------

    /// <summary>
    /// Bloque de código delimitado por vallas.
    /// </summary>
    /// <remarks>
    /// El contenido se codifica y no se interpreta: adentro de un bloque de código, el
    /// Markdown no significa nada — y el HTML, tampoco.
    /// </remarks>
    private static int RenderCodeBlock(string[] lines, int start, StringBuilder builder)
    {
        var language = lines[start].Trim().TrimStart('`', '~').Trim();
        var content = new List<string>();
        var index = start + 1;

        while (index < lines.Length && !IsFence(lines[index]))
        {
            content.Add(lines[index]);
            index++;
        }

        // La clase del lenguaje se arma con un nombre saneado, no con lo que vino: es lo
        // único de este bloque que sale del área de texto.
        var cssClass = SafeLanguage(language) is { Length: > 0 } safe
            ? $" class=\"language-{safe}\""
            : string.Empty;

        builder.Append($"<pre><code{cssClass}>{MarkdownText.Encode(string.Join('\n', content))}</code></pre>");

        // Se saltea la valla de cierre, si la hay: un bloque sin cerrar llega hasta el final.
        return index < lines.Length ? index + 1 : index;
    }

    private static int RenderQuote(string[] lines, int start, StringBuilder builder)
    {
        var content = new List<string>();
        var index = start;

        while (index < lines.Length && IsQuote(lines[index]))
        {
            content.Add(StripQuote(lines[index]));
            index++;
        }

        builder.Append("<blockquote>");

        // El contenido se renderiza como bloques: una cita puede tener párrafos y listas.
        var inner = 0;
        var quoted = content.ToArray();

        while (inner < quoted.Length)
        {
            inner = RenderBlock(quoted, inner, builder);
        }

        builder.Append("</blockquote>");

        return index;
    }

    private static int RenderList(string[] lines, int start, StringBuilder builder, bool ordered)
    {
        var tag = ordered ? "ol" : "ul";
        var index = start;

        builder.Append($"<{tag}>");

        while (index < lines.Length &&
               (ordered ? IsOrderedItem(lines[index]) : IsUnorderedItem(lines[index])))
        {
            builder.Append($"<li>{MarkdownInline.Render(StripItem(lines[index], ordered))}</li>");
            index++;
        }

        builder.Append($"</{tag}>");

        return index;
    }

    private static int RenderTable(string[] lines, int start, StringBuilder builder)
    {
        var alignments = ParseAlignments(lines[start + 1]);

        builder.Append("<table><thead><tr>");

        foreach (var (cell, column) in SplitRow(lines[start]).Select((cell, column) => (cell, column)))
        {
            builder.Append($"<th{AlignmentOf(alignments, column)}>{MarkdownInline.Render(cell)}</th>");
        }

        builder.Append("</tr></thead><tbody>");

        var index = start + 2;

        while (index < lines.Length && IsTableRow(lines[index]))
        {
            builder.Append("<tr>");

            foreach (var (cell, column) in SplitRow(lines[index]).Select((cell, column) => (cell, column)))
            {
                builder.Append($"<td{AlignmentOf(alignments, column)}>{MarkdownInline.Render(cell)}</td>");
            }

            builder.Append("</tr>");
            index++;
        }

        builder.Append("</tbody></table>");

        return index;
    }

    /// <summary>
    /// Párrafo: todo lo que sigue hasta una línea en blanco o hasta que empiece otro bloque.
    /// </summary>
    /// <remarks>
    /// Los saltos de línea de adentro se conservan como <c>&lt;br&gt;</c>. En un gestor de
    /// issues la gente escribe listas sueltas y renglones cortos esperando que se respeten,
    /// no el comportamiento de CommonMark de unir todo en una sola línea.
    /// </remarks>
    private static int RenderParagraph(string[] lines, int start, StringBuilder builder)
    {
        var content = new List<string>();
        var index = start;

        while (index < lines.Length && !string.IsNullOrWhiteSpace(lines[index]) && !StartsBlock(lines, index))
        {
            content.Add(lines[index].Trim());
            index++;
        }

        // StartsBlock nunca es verdadero en la primera línea —ya se descartó antes de llegar
        // acá—, pero si el párrafo quedara vacío igual hay que avanzar para no ciclar.
        if (content.Count == 0)
        {
            return index + 1;
        }

        var rendered = content.Select(MarkdownInline.Render);

        builder.Append($"<p>{string.Join("<br>", rendered)}</p>");

        return index;
    }

    // ---- reconocimiento --------------------------------------------------------------------

    private static bool StartsBlock(string[] lines, int index)
    {
        var line = lines[index];

        return IsFence(line)
            || IsQuote(line)
            || IsUnorderedItem(line)
            || IsOrderedItem(line)
            || TryReadHeading(line, out _, out _)
            || IsTableStart(lines, index);
    }

    private static bool IsFence(string line)
    {
        var trimmed = line.TrimStart();

        return trimmed.StartsWith("```", StringComparison.Ordinal)
            || trimmed.StartsWith("~~~", StringComparison.Ordinal);
    }

    private static bool TryReadHeading(string line, out int level, out string text)
    {
        level = 0;
        text = string.Empty;

        var trimmed = line.TrimStart();

        while (level < trimmed.Length && trimmed[level] == '#')
        {
            level++;
        }

        // Hace falta el espacio: "#etiqueta" es texto, no un título.
        if (level is 0 or > MaxHeadingLevel || level >= trimmed.Length || trimmed[level] != ' ')
        {
            level = 0;
            return false;
        }

        text = trimmed[(level + 1)..].Trim();

        return true;
    }

    private static bool IsQuote(string line) => line.TrimStart().StartsWith('>');

    private static string StripQuote(string line)
    {
        var trimmed = line.TrimStart()[1..];

        return trimmed.StartsWith(' ') ? trimmed[1..] : trimmed;
    }

    private static bool IsUnorderedItem(string line)
    {
        var trimmed = line.TrimStart();

        return trimmed.Length >= 2 && trimmed[0] is '-' or '*' or '+' && trimmed[1] == ' ';
    }

    private static bool IsOrderedItem(string line) => OrderedMarkerLength(line) > 0;

    /// <summary>Largo del marcador <c>"12. "</c>, o 0 si la línea no es un ítem numerado.</summary>
    private static int OrderedMarkerLength(string line)
    {
        var trimmed = line.TrimStart();
        var digits = 0;

        while (digits < trimmed.Length && char.IsAsciiDigit(trimmed[digits]))
        {
            digits++;
        }

        if (digits == 0 || digits + 1 >= trimmed.Length)
        {
            return 0;
        }

        return trimmed[digits] == '.' && trimmed[digits + 1] == ' ' ? digits + 2 : 0;
    }

    private static string StripItem(string line, bool ordered)
    {
        var trimmed = line.TrimStart();

        return ordered ? trimmed[OrderedMarkerLength(line)..] : trimmed[2..];
    }

    /// <summary>
    /// Una tabla se reconoce por su segunda línea: la de guiones. Sin ella, una línea con
    /// barras es texto común.
    /// </summary>
    private static bool IsTableStart(string[] lines, int index) =>
        index + 1 < lines.Length && IsTableRow(lines[index]) && IsTableSeparator(lines[index + 1]);

    private static bool IsTableRow(string line) => line.Contains('|', StringComparison.Ordinal)
        && !string.IsNullOrWhiteSpace(line);

    private static bool IsTableSeparator(string line)
    {
        var trimmed = line.Trim();

        if (!trimmed.Contains('|', StringComparison.Ordinal))
        {
            return false;
        }

        var hasDash = false;

        foreach (var character in trimmed)
        {
            if (character == '-')
            {
                hasDash = true;
            }
            else if (character is not ('|' or ':' or ' '))
            {
                return false;
            }
        }

        return hasDash;
    }

    private static string[] SplitRow(string line)
    {
        var trimmed = line.Trim();

        // Las barras de los extremos son decorativas: no delimitan una celda vacía.
        if (trimmed.StartsWith('|'))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.EndsWith('|'))
        {
            trimmed = trimmed[..^1];
        }

        return [.. trimmed.Split('|').Select(cell => cell.Trim())];
    }

    private static string[] ParseAlignments(string separator) =>
    [
        .. SplitRow(separator).Select(cell => cell switch
        {
            _ when cell.StartsWith(':') && cell.EndsWith(':') => "center",
            _ when cell.EndsWith(':') => "right",
            _ when cell.StartsWith(':') => "left",
            _ => string.Empty
        })
    ];

    private static string AlignmentOf(string[] alignments, int column) =>
        column < alignments.Length && alignments[column].Length > 0
            ? $" style=\"text-align: {alignments[column]}\""
            : string.Empty;

    /// <summary>
    /// Nombre de lenguaje reducido a letras, dígitos, guiones y signos de más — suficiente
    /// para "csharp" o "c++", y sin nada que pueda cerrar el atributo.
    /// </summary>
    private static string SafeLanguage(string language) =>
        new([.. language.Where(character => char.IsAsciiLetterOrDigit(character)
            || character is '-' or '+' or '#').Take(24)]);
}
