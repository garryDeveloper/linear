namespace Linear.Web.Features.Comments.Delete;

public sealed class DeleteCommentRequest
{
    public string Key { get; set; } = string.Empty;

    public string Identifier { get; set; } = string.Empty;

    public Guid CommentId { get; set; }
}
