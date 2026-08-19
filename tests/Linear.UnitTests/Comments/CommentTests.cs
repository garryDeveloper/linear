using Linear.Domain.Comments;

namespace Linear.UnitTests.Comments;

public class CommentTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Now.AddMinutes(5);

    private static readonly Guid IssueId = Guid.CreateVersion7();
    private static readonly Guid AuthorId = Guid.CreateVersion7();

    private static Comment AComment(string content = "Esto ya está andando en staging.") =>
        Comment.Create(IssueId, AuthorId, content, Now).Value;

    [Fact]
    public void ANewCommentBelongsToItsIssueAndAuthor()
    {
        var comment = Comment.Create(IssueId, AuthorId, "Reproduje el bug.", Now);

        Assert.True(comment.IsSuccess);
        Assert.Equal(IssueId, comment.Value.IssueId);
        Assert.Equal(AuthorId, comment.Value.AuthorId);
        Assert.Equal("Reproduje el bug.", comment.Value.Content);
        Assert.Equal(Now, comment.Value.CreatedAt);
        Assert.Equal(Now, comment.Value.UpdatedAt);
        Assert.Null(comment.Value.DeletedAt);
        Assert.False(comment.Value.IsDeleted);
        Assert.False(comment.Value.IsEdited);
    }

    [Fact]
    public void TheContentIsTrimmed()
    {
        var comment = Comment.Create(IssueId, AuthorId, "   Con espacios de más   ", Now);

        Assert.Equal("Con espacios de más", comment.Value.Content);
    }

    /// <summary>
    /// El contenido se guarda en Markdown crudo, sin interpretar ni escapar: es lo que
    /// deja implementar el renderizado de la task 012 sin migrar lo ya escrito.
    /// </summary>
    [Fact]
    public void TheMarkdownIsStoredVerbatim()
    {
        const string markdown = "## Título\n\n- **negrita**\n- `código`\n\n> cita";

        var comment = Comment.Create(IssueId, AuthorId, markdown, Now);

        Assert.Equal(markdown, comment.Value.Content);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t ")]
    public void ACommentWithoutContentIsRejected(string content)
    {
        var comment = Comment.Create(IssueId, AuthorId, content, Now);

        Assert.True(comment.IsFailure);
        Assert.Equal(CommentErrors.ContentRequired, comment.Error);
    }

    [Fact]
    public void ContentLongerThanTheLimitIsRejected()
    {
        var comment = Comment.Create(IssueId, AuthorId, new string('a', Comment.MaxContentLength + 1), Now);

        Assert.True(comment.IsFailure);
        Assert.Equal(CommentErrors.ContentTooLong, comment.Error);
    }

    [Fact]
    public void ContentExactlyAtTheLimitIsAccepted()
    {
        var comment = Comment.Create(IssueId, AuthorId, new string('a', Comment.MaxContentLength), Now);

        Assert.True(comment.IsSuccess);
    }

    [Fact]
    public void EditingChangesTheContentAndMarksItAsEdited()
    {
        var comment = AComment();

        var updated = comment.UpdateContent("Corrijo: falta desplegar.", Later);

        Assert.True(updated.IsSuccess);
        Assert.Equal("Corrijo: falta desplegar.", comment.Content);
        Assert.Equal(Later, comment.UpdatedAt);
        Assert.Equal(Now, comment.CreatedAt);
        Assert.True(comment.IsEdited);
    }

    [Fact]
    public void EditingWithEmptyContentIsRejectedAndLeavesTheCommentIntact()
    {
        var comment = AComment("Contenido original");

        var updated = comment.UpdateContent("   ", Later);

        Assert.True(updated.IsFailure);
        Assert.Equal(CommentErrors.ContentRequired, updated.Error);
        Assert.Equal("Contenido original", comment.Content);
        Assert.False(comment.IsEdited);
    }

    [Fact]
    public void DeletingMarksTheCommentWithoutRemovingItsContent()
    {
        var comment = AComment("Sigue existiendo en la base");

        var deleted = comment.Delete(Later);

        Assert.True(deleted.IsSuccess);
        Assert.True(comment.IsDeleted);
        Assert.Equal(Later, comment.DeletedAt);
        Assert.Equal("Sigue existiendo en la base", comment.Content);
    }

    /// <summary>
    /// Eliminar no es editar: si tocara <c>UpdatedAt</c>, todo comentario eliminado
    /// aparecería además como editado.
    /// </summary>
    [Fact]
    public void DeletingDoesNotCountAsEditing()
    {
        var comment = AComment();

        comment.Delete(Later);

        Assert.Equal(Now, comment.UpdatedAt);
        Assert.False(comment.IsEdited);
    }

    [Fact]
    public void DeletingTwiceFails()
    {
        var comment = AComment();
        comment.Delete(Later);

        var again = comment.Delete(Later.AddMinutes(1));

        Assert.True(again.IsFailure);
        Assert.Equal(CommentErrors.AlreadyDeleted, again.Error);
    }

    [Fact]
    public void ADeletedCommentCannotBeEdited()
    {
        var comment = AComment("Original");
        comment.Delete(Later);

        var updated = comment.UpdateContent("Otra cosa", Later.AddMinutes(1));

        Assert.True(updated.IsFailure);
        Assert.Equal(CommentErrors.Deleted, updated.Error);
        Assert.Equal("Original", comment.Content);
    }

    [Fact]
    public void OnlyTheAuthorIsRecognizedAsSuch()
    {
        var comment = AComment();

        Assert.True(comment.IsAuthoredBy(AuthorId));
        Assert.False(comment.IsAuthoredBy(Guid.CreateVersion7()));
    }
}
