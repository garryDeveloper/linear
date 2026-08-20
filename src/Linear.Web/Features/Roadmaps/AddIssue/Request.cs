namespace Linear.Web.Features.Roadmaps.AddIssue;

public sealed class AddRoadmapItemIssueRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid RoadmapId { get; set; }

    public Guid ItemId { get; set; }

    /// <summary>Identificador legible del issue, por ejemplo <c>WEB-42</c>.</summary>
    public string Identifier { get; set; } = string.Empty;
}
