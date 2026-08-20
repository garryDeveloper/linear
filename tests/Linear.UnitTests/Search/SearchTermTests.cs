using Linear.Web.Infrastructure.Search;

namespace Linear.UnitTests.Search;

/// <summary>
/// Cómo se convierte lo que se escribe en el buscador en algo que PostgreSQL puede ejecutar.
/// </summary>
public class SearchTermTests
{
    [Fact]
    public void AWordBecomesAPrefixQuery()
    {
        var term = SearchTerm.Create("auten");

        Assert.NotNull(term);
        Assert.Equal("auten:*", term.TsQuery);
    }

    /// <summary>
    /// Todas las palabras tienen que aparecer: al escribir se va acotando el resultado, que
    /// es lo contrario de lo que haría un OR.
    /// </summary>
    [Fact]
    public void SeveralWordsAreCombinedWithAnd()
    {
        var term = SearchTerm.Create("arreglar login");

        Assert.Equal("arreglar:* & login:*", term!.TsQuery);
    }

    [Fact]
    public void TheQueryIsLowercased()
    {
        Assert.Equal("login:*", SearchTerm.Create("LOGIN")!.TsQuery);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NothingToSearchGivesNull(string? raw)
    {
        Assert.Null(SearchTerm.Create(raw));
    }

    /// <summary>Una sola letra coincide con casi todo: no se consulta.</summary>
    [Fact]
    public void ATermShorterThanTheMinimumGivesNull()
    {
        Assert.Null(SearchTerm.Create("a"));
        Assert.NotNull(SearchTerm.Create("ab"));
    }

    [Fact]
    public void PunctuationOnlyGivesNull()
    {
        Assert.Null(SearchTerm.Create("!!!"));
        Assert.Null(SearchTerm.Create("--"));
    }

    /// <summary>
    /// Los caracteres con significado en tsquery no sobreviven a la limpieza, así que no
    /// pueden alterar la consulta.
    /// </summary>
    [Theory]
    [InlineData("login & bug", "login:* & bug:*")]
    [InlineData("login | bug", "login:* & bug:*")]
    [InlineData("!login", "login:*")]
    [InlineData("(login)", "login:*")]
    [InlineData("login:*", "login:*")]
    [InlineData("'login'", "login:*")]
    [InlineData("login <-> bug", "login:* & bug:*")]
    public void OperatorCharactersAreStripped(string raw, string expected)
    {
        Assert.Equal(expected, SearchTerm.Create(raw)!.TsQuery);
    }

    /// <summary>
    /// Los acentos y la eñe se conservan: el diccionario 'spanish' los normaliza por su
    /// cuenta, y quitarlos acá solo perdería información.
    /// </summary>
    [Fact]
    public void AccentsAndSpanishLettersSurvive()
    {
        Assert.Equal("autenticación:*", SearchTerm.Create("autenticación")!.TsQuery);
        Assert.Equal("año:*", SearchTerm.Create("año")!.TsQuery);
    }

    [Fact]
    public void DigitsAreKept()
    {
        Assert.Equal("web:* & 123:*", SearchTerm.Create("WEB-123")!.TsQuery);
    }

    [Fact]
    public void ExtraWhitespaceDoesNotProduceEmptyTokens()
    {
        Assert.Equal("uno:* & dos:*", SearchTerm.Create("  uno    dos  ")!.TsQuery);
    }

    [Fact]
    public void TheNumberOfWordsIsCapped()
    {
        var many = string.Join(' ', Enumerable.Range(1, SearchTerm.MaxTokens + 5).Select(n => $"palabra{n}"));

        var term = SearchTerm.Create(many);

        Assert.Equal(SearchTerm.MaxTokens, term!.TsQuery.Split('&').Length);
    }

    // ---- identificador -----------------------------------------------------------------

    [Fact]
    public void TheIdentifierPatternIsAnUppercasePrefix()
    {
        Assert.Equal("WEB-12%", SearchTerm.Create("web-12")!.IdentifierPrefix);
    }

    /// <summary>
    /// Un comodín escrito por quien busca es un carácter común: sin escapar, "%" traería
    /// todos los issues por identificador.
    /// </summary>
    [Theory]
    [InlineData("a%", "A\\%%")]
    [InlineData("a_b", "A\\_B%")]
    [InlineData("50%", "50\\%%")]
    public void TheWildcardsOfLikeAreEscaped(string raw, string expected)
    {
        Assert.Equal(expected, SearchTerm.Create(raw)!.IdentifierPrefix);
    }

    /// <summary>Solo comodines no deja ninguna palabra, así que tampoco se consulta.</summary>
    [Fact]
    public void WildcardsAloneGiveNull()
    {
        Assert.Null(SearchTerm.Create("%%"));
        Assert.Null(SearchTerm.Create("___"));
    }

    [Fact]
    public void TheOriginalTextIsKeptTrimmed()
    {
        Assert.Equal("arreglar login", SearchTerm.Create("  arreglar login  ")!.Text);
    }
}
