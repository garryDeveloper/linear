using Linear.Domain.Labels;

namespace Linear.UnitTests.Labels;

public class LabelTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid TeamId = Guid.CreateVersion7();

    private static LabelColor AColor() => LabelColor.Create("#E5484D").Value;

    private static Label ALabel() =>
        Label.Create(TeamId, "bug", "Algo no funciona", AColor(), Now).Value;

    [Fact]
    public void ANewLabelKeepsItsData()
    {
        var label = Label.Create(TeamId, "bug", "Algo no funciona", AColor(), Now);

        Assert.True(label.IsSuccess);
        Assert.Equal(TeamId, label.Value.TeamId);
        Assert.Equal("bug", label.Value.Name);
        Assert.Equal("Algo no funciona", label.Value.Description);
        Assert.Equal("#E5484D", label.Value.Color.Value);
        Assert.Equal(Now, label.Value.CreatedAt);
    }

    [Fact]
    public void TheNameIsTrimmed()
    {
        var label = Label.Create(TeamId, "  bug  ", null, AColor(), Now);

        Assert.Equal("bug", label.Value.Name);
    }

    [Theory]
    [InlineData("bug", "BUG")]
    [InlineData("Bug", "BUG")]
    [InlineData("  BuG ", "BUG")]
    public void TheNormalizedNameIgnoresCase(string name, string expected)
    {
        // Es la columna sobre la que se apoya el índice único: si no normalizara, el mismo
        // equipo podría terminar con "bug" y "Bug" como labels distintas.
        var label = Label.Create(TeamId, name, null, AColor(), Now);

        Assert.Equal(expected, label.Value.NormalizedName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ALabelWithoutANameIsRejected(string name)
    {
        var label = Label.Create(TeamId, name, null, AColor(), Now);

        Assert.True(label.IsFailure);
        Assert.Equal(LabelErrors.NameRequired, label.Error);
    }

    [Fact]
    public void ANameLongerThanTheLimitIsRejected()
    {
        var label = Label.Create(TeamId, new string('a', Label.MaxNameLength + 1), null, AColor(), Now);

        Assert.Equal(LabelErrors.NameTooLong, label.Error);
    }

    [Fact]
    public void ADescriptionLongerThanTheLimitIsRejected()
    {
        var description = new string('a', Label.MaxDescriptionLength + 1);

        var label = Label.Create(TeamId, "bug", description, AColor(), Now);

        Assert.Equal(LabelErrors.DescriptionTooLong, label.Error);
    }

    [Fact]
    public void AnEmptyDescriptionIsStoredAsNull()
    {
        var label = Label.Create(TeamId, "bug", "   ", AColor(), Now);

        Assert.Null(label.Value.Description);
    }

    [Fact]
    public void UpdatingChangesEverythingAndTheTimestamp()
    {
        var label = ALabel();
        var later = Now.AddHours(1);
        var newColor = LabelColor.Create("#4CB782").Value;

        var result = label.Update("mejora", "Una mejora", newColor, later);

        Assert.True(result.IsSuccess);
        Assert.Equal("mejora", label.Name);
        Assert.Equal("MEJORA", label.NormalizedName);
        Assert.Equal("Una mejora", label.Description);
        Assert.Equal(newColor, label.Color);
        Assert.Equal(later, label.UpdatedAt);
    }

    [Fact]
    public void UpdatingWithAnInvalidNameLeavesTheLabelUntouched()
    {
        var label = ALabel();

        var result = label.Update("", null, AColor(), Now.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal("bug", label.Name);
        Assert.Equal(Now, label.UpdatedAt);
    }

    [Fact]
    public void TheTeamOfALabelNeverChanges()
    {
        // Una label pertenece a un único equipo: el agregado no ofrece forma de moverla.
        var label = ALabel();

        label.Update("otra", null, AColor(), Now.AddHours(1));

        Assert.Equal(TeamId, label.TeamId);
    }
}
