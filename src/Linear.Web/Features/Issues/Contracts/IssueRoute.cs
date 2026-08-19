using Linear.Domain.Common;
using Linear.Domain.Issues;

namespace Linear.Web.Features.Issues.Contracts;

/// <summary>
/// Interpreta el identificador de issue que llega en la ruta.
/// </summary>
public static class IssueRoute
{
    public static Result<string> NormalizeIdentifier(string? identifier) =>
        string.IsNullOrWhiteSpace(identifier)
            ? Result.Failure<string>(IssueErrors.NotFound(identifier ?? string.Empty))
            : Result.Success(identifier.Trim().ToUpperInvariant());
}
