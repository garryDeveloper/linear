namespace Linear.Web.Features.Issues.AssignUser;

public sealed class AssignIssueUserRequest
{
    public string Key { get; set; } = string.Empty;

    public string Identifier { get; set; } = string.Empty;

    /// <summary>Usuario responsable, o <c>null</c> para dejar el issue sin asignar.</summary>
    public Guid? AssigneeId { get; set; }
}
