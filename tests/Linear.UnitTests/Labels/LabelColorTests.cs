using Linear.Domain.Labels;

namespace Linear.UnitTests.Labels;

public class LabelColorTests
{
    [Theory]
    [InlineData("#5B5BD6", "#5B5BD6")]
    [InlineData("5B5BD6", "#5B5BD6")]
    [InlineData("#5b5bd6", "#5B5BD6")]
    [InlineData("  #5b5bd6  ", "#5B5BD6")]
    public void TheColorIsNormalized(string value, string expected)
    {
        var color = LabelColor.Create(value);

        Assert.True(color.IsSuccess);
        Assert.Equal(expected, color.Value.Value);
    }

    [Fact]
    public void TwoColorsWrittenDifferentlyAreTheSame()
    {
        Assert.Equal(LabelColor.Create("#5b5bd6").Value, LabelColor.Create("5B5BD6").Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AnEmptyColorIsRejected(string? value)
    {
        var color = LabelColor.Create(value);

        Assert.True(color.IsFailure);
        Assert.Equal(LabelColorErrors.Empty, color.Error);
    }

    [Theory]
    [InlineData("rojo")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("#GGGGGG")]
    [InlineData("rgb(1,2,3)")]
    public void AMalformedColorIsRejected(string value)
    {
        var color = LabelColor.Create(value);

        Assert.True(color.IsFailure);
        Assert.Equal(LabelColorErrors.InvalidFormat, color.Error);
    }

    [Theory]
    [InlineData("#FFFFFF")]
    [InlineData("#FFFF00")]
    [InlineData("#4CB782")]
    public void OnLightBackgroundsTheTextGoesDark(string value)
    {
        Assert.True(LabelColor.Create(value).Value.PrefersDarkText);
    }

    [Theory]
    [InlineData("#000000")]
    [InlineData("#5B5BD6")]
    [InlineData("#C63D3D")]
    public void OnDarkBackgroundsTheTextGoesLight(string value)
    {
        Assert.False(LabelColor.Create(value).Value.PrefersDarkText);
    }

    [Fact]
    public void TheDefaultColorIsValid()
    {
        Assert.Equal(LabelColor.Default, LabelColor.Create(LabelColor.Default.Value).Value);
    }
}
