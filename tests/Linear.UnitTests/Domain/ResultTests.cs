using Linear.Domain.Common;

namespace Linear.UnitTests.Domain;

public class ResultTests
{
    [Fact]
    public void Success_IsSuccessful_AndHasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_IsNotSuccessful_AndKeepsTheError()
    {
        var error = Error.Validation("Team.NameRequired", "El nombre es obligatorio.");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void SuccessWithValue_ExposesTheValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void FailureWithValue_ThrowsWhenTheValueIsRead()
    {
        var result = Result.Failure<int>(Error.NotFound("Team.NotFound", "No existe."));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromValue_ProducesSuccess()
    {
        Result<string> result = "linear";

        Assert.True(result.IsSuccess);
        Assert.Equal("linear", result.Value);
    }

    [Fact]
    public void Failure_WithoutError_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
    }

    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Forbidden)]
    public void Error_KeepsItsClassification(ErrorType errorType)
    {
        var error = new Error("Some.Code", "Descripción.", errorType);

        Assert.Equal(errorType, error.Type);
    }

    [Fact]
    public void Then_RunsTheNextValidationWhenTheCurrentOnePassed()
    {
        var executed = false;

        var result = Result.Success().Then(() =>
        {
            executed = true;
            return Result.Success();
        });

        Assert.True(executed);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Then_SkipsTheNextValidationAndKeepsTheFirstError()
    {
        var first = Error.Validation("First.Error", "Primero.");
        var executed = false;

        var result = Result.Failure(first).Then(() =>
        {
            executed = true;
            return Result.Failure(Error.Validation("Second.Error", "Segundo."));
        });

        Assert.False(executed);
        Assert.Equal(first, result.Error);
    }
}
