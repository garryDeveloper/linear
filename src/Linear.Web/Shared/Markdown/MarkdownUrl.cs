using System.Globalization;
using System.Text;

namespace Linear.Web.Shared.Markdown;

/// <summary>
/// Decide si la dirección de un enlace se puede emitir.
/// </summary>
/// <remarks>
/// Es el único lugar del renderizador donde un dato del usuario termina en un atributo y no
/// en texto, así que es también el único donde codificar no alcanza: el navegador ejecuta
/// <c>javascript:</c> aunque el texto esté bien escapado. Por eso el esquema se valida
/// contra una lista blanca — se acepta lo conocido, no se rechaza lo sospechoso, que es la
/// forma que no se queda corta cuando aparece un esquema nuevo.
/// </remarks>
public static class MarkdownUrl
{
    /// <summary>Adónde se permite enlazar.</summary>
    private static readonly string[] AllowedSchemes = ["http", "https", "mailto"];

    /// <summary>Reemplazo de un enlace rechazado: no navega a ningún lado.</summary>
    public const string Blocked = "#";

    /// <summary>
    /// Devuelve la dirección si es segura, o <see cref="Blocked"/> si no lo es.
    /// </summary>
    public static string Sanitize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Blocked;
        }

        var candidate = Normalize(url);

        if (candidate.Length == 0)
        {
            return Blocked;
        }

        var scheme = SchemeOf(candidate);

        // Sin esquema es una dirección relativa —"/teams/WEB", "informe.pdf"— y se permite:
        // se resuelve contra el propio sitio y no puede ejecutar nada.
        if (scheme is null)
        {
            return url.Trim();
        }

        return AllowedSchemes.Contains(scheme, StringComparer.Ordinal) ? url.Trim() : Blocked;
    }

    /// <summary>
    /// Deja la dirección lista para inspeccionar el esquema.
    /// </summary>
    /// <remarks>
    /// Se quitan los espacios y los caracteres de control de cualquier parte, no solo de las
    /// puntas: el navegador ignora un tabulador o un salto de línea metidos en el medio, así
    /// que <c>"java\tscript:"</c> se ejecuta igual. Comparar sin normalizar dejaría pasar
    /// justamente eso.
    /// </remarks>
    private static string Normalize(string url)
    {
        var builder = new StringBuilder(url.Length);

        foreach (var character in url)
        {
            if (!char.IsWhiteSpace(character) && !char.IsControl(character))
            {
                builder.Append(char.ToLower(character, CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Esquema de la dirección, o <c>null</c> si es relativa.
    /// </summary>
    /// <remarks>
    /// Un <c>:</c> que aparece después de <c>/</c>, <c>?</c> o <c>#</c> ya es parte de la
    /// ruta o de la consulta, no un esquema: en <c>"/a/b:c"</c> no hay esquema ninguno.
    /// </remarks>
    private static string? SchemeOf(string candidate)
    {
        for (var index = 0; index < candidate.Length; index++)
        {
            var character = candidate[index];

            if (character is '/' or '?' or '#')
            {
                return null;
            }

            if (character == ':')
            {
                return index == 0 ? string.Empty : candidate[..index];
            }
        }

        return null;
    }
}
