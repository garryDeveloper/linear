using System.Text.RegularExpressions;

using Linear.Web.Shared.Markdown;

namespace Linear.UnitTests.Markdown;

/// <summary>
/// Que no se pueda inyectar HTML ni JavaScript a través del Markdown.
/// </summary>
/// <remarks>
/// Los tests no buscan subcadenas sueltas en la salida, sino que miran **las etiquetas que
/// realmente se emitieron**. La diferencia importa: <c>&amp;lt;img onerror=x&amp;gt;</c>
/// contiene la palabra "onerror" y es texto inerte, mientras que <c>&lt;img onerror=x&gt;</c>
/// es un ataque. Buscar la palabra confunde las dos cosas; mirar las etiquetas distingue lo
/// que el navegador va a ejecutar de lo que va a mostrar.
///
/// El renderizador no sanea después de armar el HTML: codifica todo el texto al salir y solo
/// emite las etiquetas que él mismo decide. Estos tests fijan esa garantía desde afuera.
/// </remarks>
public class MarkdownSecurityTests
{
    /// <summary>Etiquetas que el renderizador puede emitir. Cualquier otra es una inyección.</summary>
    private static readonly string[] AllowedTags =
    [
        "p", "br", "h1", "h2", "h3", "h4", "h5", "h6",
        "strong", "em", "code", "pre", "a", "ul", "ol", "li",
        "blockquote", "table", "thead", "tbody", "tr", "th", "td"
    ];

    /// <summary>Las etiquetas realmente emitidas: todo lo demás de la salida es texto.</summary>
    private static IReadOnlyList<string> Tags(string html) =>
        [.. Regex.Matches(html, "<[^>]*>", RegexOptions.None, TimeSpan.FromSeconds(1))
            .Select(match => match.Value)];

    /// <summary>Comprueba la garantía central sobre cualquier entrada.</summary>
    private static void AssertInert(string markdown)
    {
        var html = MarkdownRenderer.Render(markdown);

        foreach (var tag in Tags(html))
        {
            var name = Regex.Match(tag, "^</?([a-zA-Z0-9]+)", RegexOptions.None, TimeSpan.FromSeconds(1))
                .Groups[1].Value.ToLowerInvariant();

            Assert.Contains(name, AllowedTags);

            // Se vacían los valores de los atributos antes de buscar manejadores de eventos.
            // Lo que queda adentro de un valor es inerte —las comillas del usuario salen como
            // &quot;, así que nunca cierran el atributo—; lo que importa es que no aparezca un
            // atributo nuevo en el esqueleto de la etiqueta.
            var skeleton = Regex.Replace(tag, "\"[^\"]*\"", "\"\"",
                RegexOptions.None, TimeSpan.FromSeconds(1));

            Assert.DoesNotMatch(@"\son\w+\s*=", skeleton);
            Assert.DoesNotContain("javascript:", skeleton, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("vbscript:", skeleton, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("data:", skeleton, StringComparison.OrdinalIgnoreCase);

            // Y el valor de href, que sí sale de lo que escribió el usuario, se comprueba
            // aparte: ahí el esquema es lo que ejecuta el navegador.
            var href = Regex.Match(tag, "href=\"([^\"]*)\"", RegexOptions.None, TimeSpan.FromSeconds(1));

            if (href.Success)
            {
                Assert.DoesNotContain("javascript:", href.Groups[1].Value, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("vbscript:", href.Groups[1].Value, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("data:", href.Groups[1].Value, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    // ---- HTML crudo ----------------------------------------------------------------------

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("<iframe src=\"https://evil.test\"></iframe>")]
    [InlineData("<svg/onload=alert(1)>")]
    [InlineData("<body onload=alert(1)>")]
    [InlineData("<a href=\"javascript:alert(1)\">click</a>")]
    [InlineData("<style>body{display:none}</style>")]
    [InlineData("<object data=\"x\"></object>")]
    [InlineData("<embed src=\"x\">")]
    [InlineData("<form action=\"x\"><input name=\"p\"></form>")]
    [InlineData("<meta http-equiv=\"refresh\" content=\"0;url=https://evil.test\">")]
    [InlineData("<a href=\"#\" onmouseover=\"alert(1)\">x</a>")]
    public void RawHtmlNeverBecomesALiveTag(string markdown) => AssertInert(markdown);

    [Fact]
    public void RawHtmlIsShownAsLiteralText()
    {
        var html = MarkdownRenderer.Render("<script>alert(1)</script>");

        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Dentro de código, el HTML tampoco se interpreta.</summary>
    [Theory]
    [InlineData("`<script>alert(1)</script>`")]
    [InlineData("```\n<script>alert(1)</script>\n```")]
    public void HtmlInsideCodeIsEncoded(string markdown)
    {
        AssertInert(markdown);

        Assert.Contains("&lt;script&gt;", MarkdownRenderer.Render(markdown), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("**<img src=x onerror=alert(1)>**")]
    [InlineData("# <script>alert(1)</script>")]
    [InlineData("> <script>alert(1)</script>")]
    [InlineData("- <img src=x onerror=alert(1)>")]
    [InlineData("1. <script>alert(1)</script>")]
    [InlineData("| a |\n|---|\n| <script>alert(1)</script> |")]
    public void HtmlIsEncodedInsideEveryBlock(string markdown) => AssertInert(markdown);

    // ---- enlaces --------------------------------------------------------------------------

    /// <summary>
    /// El caso clásico: el texto del enlace está bien escapado, pero el navegador ejecuta el
    /// esquema igual. Por eso la dirección se valida aparte.
    /// </summary>
    [Theory]
    [InlineData("[click](javascript:alert(1))")]
    [InlineData("[click](JavaScript:alert(1))")]
    [InlineData("[click](JAVASCRIPT:alert(1))")]
    [InlineData("[click](vbscript:msgbox(1))")]
    [InlineData("[click](data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==)")]
    [InlineData("[click](file:///etc/passwd)")]
    public void DangerousLinkSchemesAreBlocked(string markdown)
    {
        AssertInert(markdown);

        Assert.Contains($"href=\"{MarkdownUrl.Blocked}\"", MarkdownRenderer.Render(markdown),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// El navegador ignora espacios y caracteres de control dentro del esquema, así que
    /// compararlo sin normalizar dejaría pasar "java\tscript:".
    /// </summary>
    [Theory]
    [InlineData("[click](  javascript:alert(1))")]
    [InlineData("[click](java\tscript:alert(1))")]
    [InlineData("[click](\0javascript:alert(1))")]
    [InlineData("[click](JaVaScRiPt:alert(1))")]
    public void ObfuscatedSchemesAreBlocked(string markdown)
    {
        AssertInert(markdown);

        Assert.Contains($"href=\"{MarkdownUrl.Blocked}\"", MarkdownRenderer.Render(markdown),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Un salto de línea corta la sintaxis del enlace: no se emite ningún <c>a</c>, que es
    /// aún más seguro que emitirlo bloqueado.
    /// </summary>
    [Fact]
    public void ANewlineInsideALinkProducesNoLinkAtAll()
    {
        var html = MarkdownRenderer.Render("[click](java\nscript:alert(1))");

        AssertInert("[click](java\nscript:alert(1))");
        Assert.DoesNotContain("<a ", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("https://linear.app")]
    [InlineData("http://ejemplo.test/ruta?x=1")]
    [InlineData("mailto:alguien@ejemplo.test")]
    [InlineData("/teams/WEB/issues/WEB-1")]
    [InlineData("informe.pdf")]
    public void SafeLinksSurvive(string url)
    {
        var html = MarkdownRenderer.Render($"[texto]({url})");

        Assert.Contains($"href=\"{url}\"", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// Sin <c>noopener</c>, la página que se abre puede redirigir a la que la abrió usando
    /// <c>window.opener</c>.
    /// </summary>
    [Fact]
    public void ExternalLinksCannotReachBackThroughTheOpener()
    {
        var html = MarkdownRenderer.Render("[texto](https://ejemplo.test)");

        Assert.Contains("rel=\"noopener noreferrer\"", html, StringComparison.Ordinal);
    }

    /// <summary>Las comillas de la dirección no pueden cerrar el atributo y abrir otro.</summary>
    [Fact]
    public void QuotesInAUrlCannotEscapeTheAttribute()
    {
        const string markdown = "[texto](https://ejemplo.test/\" onmouseover=\"alert(1))";

        AssertInert(markdown);

        Assert.Contains("&quot;", MarkdownRenderer.Render(markdown), StringComparison.Ordinal);
    }

    // ---- otros vectores ---------------------------------------------------------------------

    /// <summary>El nombre del lenguaje va a un atributo: no puede cerrarlo.</summary>
    [Fact]
    public void TheCodeBlockLanguageCannotEscapeTheAttribute()
    {
        const string markdown = "```js\" onload=\"alert(1)\ncódigo\n```";

        AssertInert(markdown);

        // Queda un único atributo, con un valor reducido a caracteres inofensivos.
        var html = MarkdownRenderer.Render(markdown);

        Assert.Matches("<code class=\"language-[a-zA-Z0-9+#-]*\">", html);
    }

    [Fact]
    public void AmpersandsAreEncodedSoEntitiesCannotBeSmuggled()
    {
        var html = MarkdownRenderer.Render("&lt;script&gt;alert(1)&lt;/script&gt;");

        // El '&' que escribió el usuario sale como &amp;: el navegador muestra "&lt;" y no lo
        // vuelve a decodificar en '<'.
        Assert.Contains("&amp;lt;", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Una entrada cualquiera nunca produce una etiqueta fuera de la lista.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("texto común")]
    [InlineData("*sin cerrar")]
    [InlineData("[roto](")
    ]
    [InlineData("```")]
    [InlineData("|||")]
    [InlineData("######## demasiados")]
    [InlineData("\\<script\\>")]
    public void OddInputStillProducesOnlyAllowedTags(string markdown) => AssertInert(markdown);
}
