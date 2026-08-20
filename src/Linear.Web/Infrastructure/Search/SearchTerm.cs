using System.Globalization;
using System.Text;

namespace Linear.Web.Infrastructure.Search;

/// <summary>
/// Lo que el usuario escribió en el buscador, ya convertido en algo que PostgreSQL puede
/// ejecutar.
/// </summary>
/// <remarks>
/// La consulta se arma a mano en lugar de usar <c>websearch_to_tsquery</c> porque esa
/// función no sabe hacer coincidencias por prefijo, y acá hacen falta: el buscador responde
/// mientras se escribe, así que "auten" tiene que encontrar "autenticación" antes de
/// terminar la palabra. El precio es perder la búsqueda por frase exacta y los operadores
/// de <c>websearch</c>, que en una paleta de comandos importan bastante menos.
///
/// Cada palabra se reduce a letras y dígitos, así que ningún carácter con significado en
/// <c>tsquery</c> —<c>&amp;</c>, <c>|</c>, <c>!</c>, paréntesis, comillas— sobrevive a la
/// limpieza. Aun así la consulta viaja como parámetro y no concatenada en el SQL.
/// </remarks>
public sealed class SearchTerm
{
    /// <summary>
    /// Debajo de dos caracteres no se consulta: una sola letra coincide con casi todo y el
    /// resultado no le sirve a nadie.
    /// </summary>
    public const int MinimumLength = 2;

    /// <summary>
    /// Tope de palabras. Un buscador de una línea no necesita más, y evita que un pegado
    /// accidental de un texto largo arme una consulta enorme.
    /// </summary>
    public const int MaxTokens = 10;

    private const string LikeEscape = "\\";

    private SearchTerm(string text, string tsQuery, string identifierPrefix)
    {
        Text = text;
        TsQuery = tsQuery;
        IdentifierPrefix = identifierPrefix;
    }

    /// <summary>Texto original, ya recortado.</summary>
    public string Text { get; }

    /// <summary>Consulta lista para <c>to_tsquery</c>, por ejemplo <c>auten:* &amp; bug:*</c>.</summary>
    public string TsQuery { get; }

    /// <summary>
    /// Patrón para buscar por identificador con <c>LIKE</c>. Va en mayúsculas porque los
    /// identificadores se guardan así, y con los comodines escapados para que un <c>%</c>
    /// escrito por el usuario sea un carácter común.
    /// </summary>
    public string IdentifierPrefix { get; }

    /// <summary>
    /// Interpreta lo que se escribió. Devuelve <c>null</c> cuando no queda nada que buscar
    /// —vacío, demasiado corto, o solo signos de puntuación—, que es la señal para no
    /// consultar la base.
    /// </summary>
    public static SearchTerm? Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim();

        if (text.Length < MinimumLength)
        {
            return null;
        }

        var tokens = Tokenize(text);

        if (tokens.Count == 0)
        {
            return null;
        }

        // Todas las palabras tienen que aparecer: al escribir se va acotando el resultado,
        // que es lo contrario de lo que haría un OR.
        var tsQuery = string.Join(" & ", tokens.Select(token => $"{token}:*"));

        return new SearchTerm(text, tsQuery, $"{Escape(text.ToUpperInvariant())}%");
    }

    /// <summary>
    /// Parte el texto en palabras, quedándose solo con letras y dígitos.
    /// </summary>
    /// <remarks>
    /// Se conservan los acentos y la eñe: el diccionario 'spanish' los normaliza por su
    /// cuenta, así que quitarlos acá solo perdería información.
    /// </remarks>
    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();

        foreach (var character in text)
        {
            if (char.IsLetterOrDigit(character))
            {
                current.Append(char.ToLower(character, CultureInfo.InvariantCulture));
                continue;
            }

            AddToken(tokens, current);

            if (tokens.Count == MaxTokens)
            {
                return tokens;
            }
        }

        AddToken(tokens, current);

        return tokens;
    }

    private static void AddToken(List<string> tokens, StringBuilder current)
    {
        if (current.Length > 0 && tokens.Count < MaxTokens)
        {
            tokens.Add(current.ToString());
        }

        current.Clear();
    }

    private static string Escape(string value) => value
        .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
        .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
        .Replace("_", LikeEscape + "_", StringComparison.Ordinal);
}
