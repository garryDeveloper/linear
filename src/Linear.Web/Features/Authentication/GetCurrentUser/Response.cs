namespace Linear.Web.Features.Authentication.GetCurrentUser;

/// <summary>
/// Datos del usuario con la sesión iniciada.
/// </summary>
public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string Name,
    string? AvatarUrl,
    string Role,
    DateTimeOffset CreatedAt);
