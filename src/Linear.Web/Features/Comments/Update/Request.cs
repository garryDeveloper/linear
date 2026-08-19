namespace Linear.Web.Features.Comments.Update;

public sealed class UpdateCommentRequest
{
    public string Key { get; set; } = string.Empty;

    public string Identifier { get; set; } = string.Empty;

    public Guid CommentId { get; set; }

    /// <summary>Contenido en Markdown.</summary>
    public string Content { get; set; } = string.Empty;
}
