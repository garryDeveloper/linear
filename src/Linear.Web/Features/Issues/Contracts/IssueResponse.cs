using Linear.Web.Features.Labels.Contracts;

namespace Linear.Web.Features.Issues.Contracts;

/// <summary>
/// Un issue completo, para la vista de detalle.
/// </summary>
public sealed record IssueResponse(
    Guid Id,
    string Identifier,
    string Title,
    string? Description,
    string Status,
    string Priority,
    int? Estimate,
    IssueUserResponse? Assignee,
    IssueUserResponse CreatedBy,
    IReadOnlyList<LabelResponse> Labels,
    IssueSprintResponse? Sprint,
    IssueRoadmapItemResponse? RoadmapItem,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? ArchivedAt);

/// <summary>
/// Referencia liviana al sprint de un issue: alcanza para mostrarlo y para enlazarlo, sin
/// arrastrar sus fechas, su estado ni sus métricas.
/// </summary>
public sealed record IssueSprintResponse(Guid Id, string Name);

/// <summary>
/// Referencia liviana a la iniciativa del roadmap a la que aporta el issue. Lleva también el
/// roadmap que la contiene, porque sin él no se puede armar el enlace a la línea de tiempo.
/// </summary>
public sealed record IssueRoadmapItemResponse(Guid Id, string Name, Guid RoadmapId, string RoadmapName);

/// <summary>
/// Un issue en un listado: liviano a propósito, sin descripción — la lista se lee de un
/// vistazo, la descripción se lee en el detalle.
/// </summary>
public sealed record IssueSummaryResponse(
    Guid Id,
    string Identifier,
    string Title,
    string Status,
    string Priority,
    int? Estimate,
    IssueUserResponse? Assignee,
    IReadOnlyList<LabelResponse> Labels,
    DateTimeOffset CreatedAt);
