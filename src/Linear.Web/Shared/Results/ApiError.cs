namespace Linear.Web.Shared.Results;

/// <summary>
/// Forma en que un <see cref="Linear.Domain.Common.Error"/> viaja por HTTP.
/// </summary>
/// <param name="Code">Identificador estable del error, por ejemplo <c>Team.KeyAlreadyExists</c>.</param>
/// <param name="Description">Mensaje legible por una persona.</param>
public sealed record ApiError(string Code, string Description);
