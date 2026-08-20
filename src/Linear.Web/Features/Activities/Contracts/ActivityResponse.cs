using Linear.Web.Features.Issues.Contracts;

namespace Linear.Web.Features.Activities.Contracts;

/// <summary>
/// Una entrada del historial, tal como la consume la interfaz.
/// </summary>
/// <param name="Payload">
/// Detalle de la acción, ya interpretado. Las claves dependen de la acción: un cambio de
/// estado trae <c>oldValue</c> y <c>newValue</c>, una label trae <c>labelId</c>.
/// </param>
public sealed record ActivityResponse(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    IssueUserResponse Actor,
    IReadOnlyDictionary<string, string?> Payload,
    DateTimeOffset CreatedAt);
