using System.Net;

using Linear.Domain.Common;
using Linear.Web.Shared.Results;

using Microsoft.AspNetCore.Http;

namespace Linear.UnitTests.Shared;

public class ErrorHttpMappingTests
{
    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Failure, StatusCodes.Status500InternalServerError)]
    public void EachErrorType_MapsToItsStatusCode(ErrorType errorType, int expectedStatusCode)
    {
        var error = new Error("Some.Code", "Descripción.", errorType);

        Assert.Equal(expectedStatusCode, ErrorHttpMapping.ToStatusCode(error));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorType.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorType.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, ErrorType.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, ErrorType.NotFound)]
    [InlineData(HttpStatusCode.Conflict, ErrorType.Conflict)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorType.Failure)]
    [InlineData(HttpStatusCode.BadGateway, ErrorType.Failure)]
    public void EachFailedStatusCode_MapsBackToAnErrorType(
        HttpStatusCode statusCode,
        ErrorType expectedErrorType)
    {
        Assert.Equal(expectedErrorType, ErrorHttpMapping.ToErrorType(statusCode));
    }

    [Fact]
    public void TheMappingIsSymmetricForTheTypesThatTravelOverHttp()
    {
        ErrorType[] roundTrippable =
        [
            ErrorType.Validation,
            ErrorType.Unauthorized,
            ErrorType.Forbidden,
            ErrorType.NotFound,
            ErrorType.Conflict,
            ErrorType.Failure
        ];

        foreach (var errorType in roundTrippable)
        {
            var statusCode = (HttpStatusCode)ErrorHttpMapping.ToStatusCode(errorType);

            Assert.Equal(errorType, ErrorHttpMapping.ToErrorType(statusCode));
        }
    }
}
