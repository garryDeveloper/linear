using Linear.Web.Features.Issues.Contracts;

namespace Linear.Web.Features.Comments.Contracts;

/// <summary>
/// Un comentario tal como lo consume la interfaz.
/// </summary>
/// <param name="Content">
/// Markdown crudo, sin interpretar. Renderizarlo —y sanitizarlo— es de la task 012.
/// </param>
/// <param name="IsEdited">Si se editó después de publicado, para poder marcarlo.</param>
/// <param name="CanEdit">Si quien pide el comentario puede editarlo.</param>
/// <param name="CanDelete">Si quien pide el comentario puede eliminarlo.</param>
public sealed record CommentResponse(
    Guid Id,
    string Content,
    IssueUserResponse Author,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsEdited,
    bool CanEdit,
    bool CanDelete);
