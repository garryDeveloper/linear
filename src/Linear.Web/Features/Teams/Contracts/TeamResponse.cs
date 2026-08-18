namespace Linear.Web.Features.Teams.Contracts;

/// <summary>
/// Un equipo con su plantel de miembros.
/// </summary>
public sealed record TeamResponse(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    string Role,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TeamMemberResponse> Members);

/// <summary>
/// Un equipo sin sus miembros, para listados.
/// </summary>
/// <param name="Role">Rol del usuario que consulta dentro de ese equipo.</param>
/// <param name="MemberCount">Cantidad de miembros del equipo.</param>
public sealed record TeamSummaryResponse(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    string Role,
    int MemberCount);

public sealed record TeamMemberResponse(
    Guid UserId,
    string Name,
    string Email,
    string? AvatarUrl,
    string Role,
    DateTimeOffset JoinedAt);
