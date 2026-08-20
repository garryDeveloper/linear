using Linear.Domain.Activities;
using Linear.Domain.Comments;
using Linear.Domain.Issues;
using Linear.Domain.Roadmaps;
using Linear.Domain.Sprints;
using Linear.Domain.Teams;

namespace Linear.UnitTests.Activities;

/// <summary>
/// Qué actividad levanta cada agregado.
/// </summary>
/// <remarks>
/// Es la parte que no se puede reconstruir mirando la base: acá se decide que cambiar el
/// estado a Done fue "completar" y no "editar", y que asignar es una acción propia.
/// </remarks>
public class ActivityEventTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Now.AddHours(1);

    private static readonly Guid TeamId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    private static Issue AnIssue()
    {
        var issue = Issue.Create(
            IssueIdentifier.Create(TeamKey.Create("WEB").Value, 1),
            TeamId,
            "Arreglar el login",
            null,
            UserId,
            Now).Value;

        issue.ClearActivity();

        return issue;
    }

    private static ActivityEvent Single(IHasActivity source) => Assert.Single(source.PendingActivity);

    // ---- issue --------------------------------------------------------------------------

    [Fact]
    public void CreatingAnIssueRecordsIt()
    {
        var issue = Issue.Create(
            IssueIdentifier.Create(TeamKey.Create("WEB").Value, 1),
            TeamId,
            "Arreglar el login",
            null,
            UserId,
            Now).Value;

        var activity = Single(issue);

        Assert.Equal(ActivityAction.IssueCreated, activity.Action);
        Assert.Equal(ActivityEntityType.Issue, activity.EntityType);
        Assert.Equal(issue.Id, activity.EntityId);
        Assert.Equal(TeamId, activity.TeamId);
        Assert.Equal(issue.Id, activity.IssueId);
        Assert.Equal("WEB-1", activity.Payload["identifier"]);
    }

    [Fact]
    public void EditingAnIssueRecordsTheOldAndNewTitle()
    {
        var issue = AnIssue();

        issue.UpdateContent("Otro título", "Detalle", Later);

        var activity = Single(issue);

        Assert.Equal(ActivityAction.IssueUpdated, activity.Action);
        Assert.Equal("Arreglar el login", activity.Payload["oldValue"]);
        Assert.Equal("Otro título", activity.Payload["newValue"]);
    }

    /// <summary>
    /// Completar y cancelar son acciones propias. Es la distinción que justifica levantar el
    /// evento en el dominio: la fila resultante no dice cuál de las tres cosas pasó.
    /// </summary>
    [Theory]
    [InlineData(IssueStatus.Done, ActivityAction.IssueCompleted)]
    [InlineData(IssueStatus.Canceled, ActivityAction.IssueCanceled)]
    [InlineData(IssueStatus.InProgress, ActivityAction.IssueUpdated)]
    [InlineData(IssueStatus.InReview, ActivityAction.IssueUpdated)]
    [InlineData(IssueStatus.Todo, ActivityAction.IssueUpdated)]
    public void ChangingStatusRecordsTheRightAction(IssueStatus status, ActivityAction expected)
    {
        var issue = AnIssue();

        issue.ChangeStatus(status, Later);

        var activity = Single(issue);

        Assert.Equal(expected, activity.Action);
        Assert.Equal(nameof(Issue.Status), activity.Payload["field"]);
        Assert.Equal(nameof(IssueStatus.Backlog), activity.Payload["oldValue"]);
        Assert.Equal(status.ToString(), activity.Payload["newValue"]);
    }

    /// <summary>Un cambio que no cambia nada no ensucia el historial.</summary>
    [Fact]
    public void SettingTheSameStatusRecordsNothing()
    {
        var issue = AnIssue();

        issue.ChangeStatus(IssueStatus.Backlog, Later);

        Assert.Empty(issue.PendingActivity);
    }

    [Fact]
    public void AssigningRecordsTheOldAndNewAssignee()
    {
        var issue = AnIssue();
        var assignee = Guid.CreateVersion7();

        issue.AssignTo(assignee, Later);

        var activity = Single(issue);

        Assert.Equal(ActivityAction.IssueAssigned, activity.Action);
        Assert.Null(activity.Payload["oldValue"]);
        Assert.Equal(assignee.ToString(), activity.Payload["newValue"]);
    }

    [Fact]
    public void UnassigningRecordsTheEmptyNewValue()
    {
        var issue = AnIssue();
        issue.AssignTo(Guid.CreateVersion7(), Later);
        issue.ClearActivity();

        issue.AssignTo(null, Later);

        Assert.Null(Single(issue).Payload["newValue"]);
    }

    [Fact]
    public void AssigningToTheSamePersonRecordsNothing()
    {
        var issue = AnIssue();
        var assignee = Guid.CreateVersion7();
        issue.AssignTo(assignee, Later);
        issue.ClearActivity();

        issue.AssignTo(assignee, Later);

        Assert.Empty(issue.PendingActivity);
    }

    [Fact]
    public void AddingAndRemovingALabelAreRecorded()
    {
        var issue = AnIssue();
        var labelId = Guid.CreateVersion7();

        issue.AddLabel(labelId, Later);
        Assert.Equal(ActivityAction.LabelAdded, Single(issue).Action);

        issue.ClearActivity();

        issue.RemoveLabel(labelId, Later);
        var removed = Single(issue);

        Assert.Equal(ActivityAction.LabelRemoved, removed.Action);
        Assert.Equal(labelId.ToString(), removed.Payload["labelId"]);
    }

    [Fact]
    public void AFailedLabelOperationRecordsNothing()
    {
        var issue = AnIssue();

        issue.RemoveLabel(Guid.CreateVersion7(), Later);

        Assert.Empty(issue.PendingActivity);
    }

    [Fact]
    public void ChangesThatHaveNoActionRecordNothing()
    {
        var issue = AnIssue();

        // La task 011 no define acciones para prioridad, estimate, sprint ni archivar.
        issue.ChangePriority(IssuePriority.High, Later);
        issue.ChangeEstimate(5, Later);
        issue.AssignToSprint(Guid.CreateVersion7(), Later);
        issue.Archive(Later);

        Assert.Empty(issue.PendingActivity);
    }

    [Fact]
    public void ClearingLeavesNothingPending()
    {
        var issue = AnIssue();
        issue.ChangeStatus(IssueStatus.Done, Later);

        issue.ClearActivity();

        Assert.Empty(issue.PendingActivity);
    }

    // ---- comentarios ---------------------------------------------------------------------

    /// <summary>
    /// Un comentario solo conoce su issue, así que su actividad va sin equipo: lo completa la
    /// infraestructura al guardar.
    /// </summary>
    [Fact]
    public void CreatingACommentRecordsItAgainstTheIssue()
    {
        var issueId = Guid.CreateVersion7();

        var comment = Comment.Create(issueId, UserId, "Reproduje el bug.", Now).Value;

        var activity = Single(comment);

        Assert.Equal(ActivityAction.CommentCreated, activity.Action);
        Assert.Equal(ActivityEntityType.Comment, activity.EntityType);
        Assert.Equal(comment.Id, activity.EntityId);
        Assert.Equal(issueId, activity.IssueId);
        Assert.Null(activity.TeamId);
    }

    [Fact]
    public void EditingACommentIsRecorded()
    {
        var comment = Comment.Create(Guid.CreateVersion7(), UserId, "Original", Now).Value;
        comment.ClearActivity();

        comment.UpdateContent("Corregido", Later);

        Assert.Equal(ActivityAction.CommentUpdated, Single(comment).Action);
    }

    /// <summary>
    /// Eliminar un comentario no tiene acción en el vocabulario de la task 011, y el historial
    /// es append-only: inventarla sería agregar un término que después no se puede corregir.
    /// </summary>
    [Fact]
    public void DeletingACommentRecordsNothing()
    {
        var comment = Comment.Create(Guid.CreateVersion7(), UserId, "Se borra", Now).Value;
        comment.ClearActivity();

        comment.Delete(Later);

        Assert.Empty(comment.PendingActivity);
    }

    // ---- sprints -------------------------------------------------------------------------

    [Fact]
    public void StartingAndCompletingASprintAreRecorded()
    {
        var sprint = Sprint.Create(
            TeamId, "Sprint 12", null, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 15), Now).Value;

        // Crear un sprint no tiene acción propia en la task 011.
        Assert.Empty(sprint.PendingActivity);

        sprint.Start(Later);
        var started = Single(sprint);

        Assert.Equal(ActivityAction.SprintStarted, started.Action);
        Assert.Equal(ActivityEntityType.Sprint, started.EntityType);
        Assert.Equal(TeamId, started.TeamId);
        Assert.Equal("Sprint 12", started.Payload["name"]);

        sprint.ClearActivity();

        sprint.Complete(Later);
        Assert.Equal(ActivityAction.SprintCompleted, Single(sprint).Action);
    }

    [Fact]
    public void CancelingASprintRecordsNothing()
    {
        var sprint = Sprint.Create(
            TeamId, "Sprint 12", null, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 15), Now).Value;

        sprint.Cancel(Later);

        Assert.Empty(sprint.PendingActivity);
    }

    // ---- roadmap -------------------------------------------------------------------------

    [Fact]
    public void CreatingAndUpdatingARoadmapItemAreRecorded()
    {
        var roadmap = Roadmap.Create(TeamId, "Segundo semestre", null, Now).Value;
        var start = new DateOnly(2026, 9, 1);
        var target = new DateOnly(2026, 11, 30);

        var item = roadmap.AddItem("Autenticación", null, start, target, Now).Value;
        var created = Single(roadmap);

        Assert.Equal(ActivityAction.RoadmapItemCreated, created.Action);
        // La entidad afectada es la iniciativa, no el roadmap: es lo que se nombra en el feed.
        Assert.Equal(ActivityEntityType.RoadmapItem, created.EntityType);
        Assert.Equal(item.Id, created.EntityId);
        Assert.Equal(TeamId, created.TeamId);
        Assert.Equal("Autenticación", created.Payload["name"]);
        Assert.Equal("Segundo semestre", created.Payload["roadmapName"]);

        roadmap.ClearActivity();

        roadmap.UpdateItem(item.Id, "SSO", null, start, target, Later);
        var updated = Single(roadmap);

        Assert.Equal(ActivityAction.RoadmapItemUpdated, updated.Action);
        Assert.Equal("SSO", updated.Payload["name"]);
    }

    [Fact]
    public void AFailedRoadmapItemOperationRecordsNothing()
    {
        var roadmap = Roadmap.Create(TeamId, "Segundo semestre", null, Now).Value;

        roadmap.AddItem("", null, new DateOnly(2026, 9, 1), new DateOnly(2026, 11, 30), Now);

        Assert.Empty(roadmap.PendingActivity);
    }
}
