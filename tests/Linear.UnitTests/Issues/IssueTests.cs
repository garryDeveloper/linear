using Linear.Domain.Issues;
using Linear.Domain.Teams;

namespace Linear.UnitTests.Issues;

public class IssueTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid TeamId = Guid.CreateVersion7();
    private static readonly Guid CreatedById = Guid.CreateVersion7();
    private static readonly Guid AssigneeId = Guid.CreateVersion7();
    private static readonly Guid LabelId = Guid.CreateVersion7();

    private static IssueIdentifier AnIdentifier() =>
        IssueIdentifier.Create(TeamKey.Create("WEB").Value, 1);

    private static Issue AnIssue() =>
        Issue.Create(AnIdentifier(), TeamId, "Fix session timeout", "Detalle", CreatedById, Now).Value;

    [Fact]
    public void ANewIssueStartsInBacklogWithNoPriority()
    {
        var issue = Issue.Create(AnIdentifier(), TeamId, "Fix session timeout", "Detalle", CreatedById, Now);

        Assert.True(issue.IsSuccess);
        Assert.Equal(IssueStatus.Backlog, issue.Value.Status);
        Assert.Equal(IssuePriority.None, issue.Value.Priority);
        Assert.Equal(TeamId, issue.Value.TeamId);
        Assert.Equal(CreatedById, issue.Value.CreatedById);
        Assert.Null(issue.Value.AssigneeId);
        Assert.False(issue.Value.IsArchived);
        Assert.Equal(Now, issue.Value.CreatedAt);
    }

    [Fact]
    public void TheTitleIsTrimmed()
    {
        var issue = Issue.Create(AnIdentifier(), TeamId, "  Fix session timeout  ", null, CreatedById, Now);

        Assert.Equal("Fix session timeout", issue.Value.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AnIssueWithoutATitleIsRejected(string title)
    {
        var issue = Issue.Create(AnIdentifier(), TeamId, title, null, CreatedById, Now);

        Assert.True(issue.IsFailure);
        Assert.Equal(IssueErrors.TitleRequired, issue.Error);
    }

    [Fact]
    public void ATitleLongerThanTheLimitIsRejected()
    {
        var issue = Issue.Create(
            AnIdentifier(), TeamId, new string('a', Issue.MaxTitleLength + 1), null, CreatedById, Now);

        Assert.Equal(IssueErrors.TitleTooLong, issue.Error);
    }

    [Fact]
    public void AnEmptyDescriptionIsStoredAsNull()
    {
        var issue = Issue.Create(AnIdentifier(), TeamId, "Título", "   ", CreatedById, Now);

        Assert.Null(issue.Value.Description);
    }

    [Fact]
    public void UpdatingContentChangesTitleAndDescription()
    {
        var issue = AnIssue();
        var later = Now.AddHours(1);

        var result = issue.UpdateContent("Nuevo título", "Nueva descripción", later);

        Assert.True(result.IsSuccess);
        Assert.Equal("Nuevo título", issue.Title);
        Assert.Equal("Nueva descripción", issue.Description);
        Assert.Equal(later, issue.UpdatedAt);
    }

    [Fact]
    public void UpdatingWithAnEmptyTitleLeavesTheIssueUntouched()
    {
        var issue = AnIssue();

        var result = issue.UpdateContent("", null, Now.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal("Fix session timeout", issue.Title);
        Assert.Equal(Now, issue.UpdatedAt);
    }

    [Fact]
    public void MovingToDoneSetsCompletedAt()
    {
        var issue = AnIssue();
        var later = Now.AddHours(1);

        issue.ChangeStatus(IssueStatus.Done, later);

        Assert.Equal(IssueStatus.Done, issue.Status);
        Assert.Equal(later, issue.CompletedAt);
        Assert.Equal(later, issue.UpdatedAt);
    }

    [Fact]
    public void ReopeningAFinishedIssueClearsCompletedAt()
    {
        var issue = AnIssue();
        issue.ChangeStatus(IssueStatus.Done, Now.AddHours(1));

        issue.ChangeStatus(IssueStatus.InProgress, Now.AddHours(2));

        Assert.Null(issue.CompletedAt);
    }

    [Fact]
    public void CancelingAFinishedIssueClearsCompletedAtToo()
    {
        var issue = AnIssue();
        issue.ChangeStatus(IssueStatus.Done, Now.AddHours(1));

        issue.ChangeStatus(IssueStatus.Canceled, Now.AddHours(2));

        Assert.Null(issue.CompletedAt);
        Assert.Equal(IssueStatus.Canceled, issue.Status);
    }

    [Fact]
    public void SettingTheSameStatusDoesNotTouchTheTimestamp()
    {
        var issue = AnIssue();

        issue.ChangeStatus(IssueStatus.Backlog, Now.AddHours(1));

        Assert.Equal(Now, issue.UpdatedAt);
    }

    [Fact]
    public void ChangingThePriorityUpdatesTheTimestamp()
    {
        var issue = AnIssue();
        var later = Now.AddHours(1);

        issue.ChangePriority(IssuePriority.Urgent, later);

        Assert.Equal(IssuePriority.Urgent, issue.Priority);
        Assert.Equal(later, issue.UpdatedAt);
    }

    [Fact]
    public void AssigningSetsTheAssignee()
    {
        var issue = AnIssue();
        var later = Now.AddHours(1);

        issue.AssignTo(AssigneeId, later);

        Assert.Equal(AssigneeId, issue.AssigneeId);
        Assert.Equal(later, issue.UpdatedAt);
    }

    [Fact]
    public void AssigningNullClearsTheAssignee()
    {
        var issue = AnIssue();
        issue.AssignTo(AssigneeId, Now.AddHours(1));

        issue.AssignTo(null, Now.AddHours(2));

        Assert.Null(issue.AssigneeId);
    }

    [Fact]
    public void ChangingTheEstimateWithinRangeSucceeds()
    {
        var issue = AnIssue();
        var later = Now.AddHours(1);

        var result = issue.ChangeEstimate(5, later);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, issue.Estimate);
        Assert.Equal(later, issue.UpdatedAt);
    }

    [Fact]
    public void ClearingTheEstimateIsAllowed()
    {
        var issue = AnIssue();
        issue.ChangeEstimate(5, Now.AddHours(1));

        var result = issue.ChangeEstimate(null, Now.AddHours(2));

        Assert.True(result.IsSuccess);
        Assert.Null(issue.Estimate);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000)]
    public void AnEstimateOutsideTheRangeIsRejected(int estimate)
    {
        var issue = AnIssue();

        var result = issue.ChangeEstimate(estimate, Now.AddHours(1));

        Assert.True(result.IsFailure);
        Assert.Equal(IssueErrors.EstimateOutOfRange, result.Error);
        Assert.Null(issue.Estimate);
    }

    [Fact]
    public void ArchivingSetsArchivedAt()
    {
        var issue = AnIssue();
        var later = Now.AddHours(1);

        var result = issue.Archive(later);

        Assert.True(result.IsSuccess);
        Assert.True(issue.IsArchived);
        Assert.Equal(later, issue.ArchivedAt);
    }

    [Fact]
    public void ArchivingTwiceFails()
    {
        var issue = AnIssue();
        issue.Archive(Now.AddHours(1));

        var result = issue.Archive(Now.AddHours(2));

        Assert.True(result.IsFailure);
        Assert.Equal(IssueErrors.AlreadyArchived, result.Error);
    }

    [Fact]
    public void ALabelCanBeAdded()
    {
        var issue = AnIssue();

        var result = issue.AddLabel(LabelId, Now.AddHours(1));

        Assert.True(result.IsSuccess);
        Assert.True(issue.HasLabel(LabelId));
        Assert.Single(issue.Labels);
    }

    [Fact]
    public void TheSameLabelCannotBeAddedTwice()
    {
        var issue = AnIssue();
        issue.AddLabel(LabelId, Now.AddHours(1));

        var result = issue.AddLabel(LabelId, Now.AddHours(2));

        Assert.True(result.IsFailure);
        Assert.Equal(IssueErrors.LabelAlreadyAdded, result.Error);
        Assert.Single(issue.Labels);
    }

    [Fact]
    public void RemovingALabelThatIsNotThereFails()
    {
        var issue = AnIssue();

        var result = issue.RemoveLabel(LabelId, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(IssueErrors.LabelNotAdded, result.Error);
    }

    [Fact]
    public void ALabelCanBeRemoved()
    {
        var issue = AnIssue();
        issue.AddLabel(LabelId, Now.AddHours(1));

        var result = issue.RemoveLabel(LabelId, Now.AddHours(2));

        Assert.True(result.IsSuccess);
        Assert.False(issue.HasLabel(LabelId));
        Assert.Empty(issue.Labels);
    }

    [Fact]
    public void TheTeamOfAnIssueNeverChanges()
    {
        var issue = AnIssue();

        issue.UpdateContent("Otro título", null, Now.AddHours(1));
        issue.ChangeStatus(IssueStatus.Done, Now.AddHours(2));

        Assert.Equal(TeamId, issue.TeamId);
    }

    [Fact]
    public void AnIssueStartsWithoutASprint()
    {
        Assert.Null(AnIssue().SprintId);
    }

    [Fact]
    public void AnIssueCanBeAssignedToASprint()
    {
        var issue = AnIssue();
        var sprintId = Guid.CreateVersion7();

        var result = issue.AssignToSprint(sprintId, Now.AddHours(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(sprintId, issue.SprintId);
        Assert.Equal(Now.AddHours(1), issue.UpdatedAt);
    }

    /// <summary>
    /// "Un Issue puede pertenecer a un único Sprint" (task 007): asignarlo a otro lo mueve,
    /// no lo suma a los dos.
    /// </summary>
    [Fact]
    public void AssigningToAnotherSprintMovesTheIssue()
    {
        var issue = AnIssue();
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();

        issue.AssignToSprint(first, Now.AddHours(1));
        var moved = issue.AssignToSprint(second, Now.AddHours(2));

        Assert.True(moved.IsSuccess);
        Assert.Equal(second, issue.SprintId);
    }

    [Fact]
    public void AssigningToTheSameSprintTwiceFails()
    {
        var issue = AnIssue();
        var sprintId = Guid.CreateVersion7();
        issue.AssignToSprint(sprintId, Now.AddHours(1));

        var again = issue.AssignToSprint(sprintId, Now.AddHours(2));

        Assert.True(again.IsFailure);
        Assert.Equal(IssueErrors.AlreadyInSprint, again.Error);
    }

    [Fact]
    public void AnIssueCanBeRemovedFromItsSprint()
    {
        var issue = AnIssue();
        issue.AssignToSprint(Guid.CreateVersion7(), Now.AddHours(1));

        var removed = issue.RemoveFromSprint(Now.AddHours(2));

        Assert.True(removed.IsSuccess);
        Assert.Null(issue.SprintId);
    }

    [Fact]
    public void RemovingAnIssueThatHasNoSprintFails()
    {
        var removed = AnIssue().RemoveFromSprint(Now.AddHours(1));

        Assert.True(removed.IsFailure);
        Assert.Equal(IssueErrors.NotInASprint, removed.Error);
    }
}
