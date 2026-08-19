namespace Linear.Web.Features.Issues.Contracts;

/// <summary>
/// Referencia liviana a un usuario, para mostrar en un issue sin traer todo lo que expone
/// <c>TeamMemberResponse</c> (rol, fecha de ingreso) — nada de eso es relevante acá.
/// </summary>
public sealed record IssueUserResponse(Guid Id, string Name, string? AvatarUrl);
