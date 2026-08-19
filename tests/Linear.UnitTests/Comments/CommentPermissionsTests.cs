using Linear.Domain.Comments;
using Linear.Domain.Teams;
using Linear.Web.Features.Comments.Contracts;

namespace Linear.UnitTests.Comments;

/// <summary>
/// La matriz de permisos de la task 006: cada uno edita lo suyo, y solo un Admin puede
/// eliminar lo ajeno.
/// </summary>
public class CommentPermissionsTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid AuthorId = Guid.CreateVersion7();
    private static readonly Guid SomeoneElseId = Guid.CreateVersion7();

    private static Comment AComment() =>
        Comment.Create(Guid.CreateVersion7(), AuthorId, "Un comentario", Now).Value;

    [Fact]
    public void TheAuthorCanEditTheirOwnComment()
    {
        Assert.True(CommentPermissions.CanEdit(AComment(), AuthorId));
    }

    /// <summary>
    /// Editar no depende del rol: <see cref="CommentPermissions.CanEdit"/> ni lo recibe.
    /// Moderar es eliminar, no reescribir, así que ni un Owner puede cambiar las palabras
    /// de otra persona.
    /// </summary>
    [Fact]
    public void NobodyElseCanEditIt()
    {
        Assert.False(CommentPermissions.CanEdit(AComment(), SomeoneElseId));
    }

    [Theory]
    [InlineData(TeamRole.Member)]
    [InlineData(TeamRole.Admin)]
    [InlineData(TeamRole.Owner)]
    public void TheAuthorCanDeleteTheirOwnCommentWhateverTheirRole(TeamRole role)
    {
        Assert.True(CommentPermissions.CanDelete(AComment(), AuthorId, role));
    }

    [Fact]
    public void AMemberCannotDeleteSomeoneElsesComment()
    {
        Assert.False(CommentPermissions.CanDelete(AComment(), SomeoneElseId, TeamRole.Member));
    }

    [Theory]
    [InlineData(TeamRole.Admin)]
    [InlineData(TeamRole.Owner)]
    public void AnAdminOrOwnerCanModerateSomeoneElsesComment(TeamRole role)
    {
        Assert.True(CommentPermissions.CanDelete(AComment(), SomeoneElseId, role));
    }
}
