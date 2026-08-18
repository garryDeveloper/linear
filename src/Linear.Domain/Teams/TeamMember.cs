namespace Linear.Domain.Teams;

/// <summary>
/// Pertenencia de un usuario a un equipo.
/// </summary>
/// <remarks>
/// Vive dentro del agregado <see cref="Team"/>: se crea y se modifica a través de él, que
/// es quien puede hacer valer reglas que involucran a todos los miembros a la vez —como
/// que siempre quede un Owner—.
/// </remarks>
public sealed class TeamMember
{
    /// <summary>Requerido por EF Core para materializar la entidad.</summary>
    private TeamMember()
    {
    }

    internal TeamMember(Guid teamId, Guid userId, TeamRole role, DateTimeOffset joinedAt)
    {
        Id = Guid.CreateVersion7();
        TeamId = teamId;
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
    }

    public Guid Id { get; private set; }

    public Guid TeamId { get; private set; }

    public Guid UserId { get; private set; }

    public TeamRole Role { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }

    internal void ChangeRole(TeamRole role) => Role = role;
}
