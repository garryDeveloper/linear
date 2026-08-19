using Linear.Domain.Sprints;

namespace Linear.UnitTests.Sprints;

public class SprintTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Now.AddDays(14);

    private static readonly Guid TeamId = Guid.CreateVersion7();
    private static readonly DateOnly Start = new(2026, 8, 19);
    private static readonly DateOnly End = new(2026, 9, 2);

    private static Sprint ASprint() =>
        Sprint.Create(TeamId, "Sprint 12", "Cerrar el checkout", Start, End, Now).Value;

    private static Sprint AnActiveSprint()
    {
        var sprint = ASprint();
        sprint.Start(Now);
        return sprint;
    }

    [Fact]
    public void ANewSprintStartsPlanned()
    {
        var sprint = Sprint.Create(TeamId, "Sprint 12", "Cerrar el checkout", Start, End, Now);

        Assert.True(sprint.IsSuccess);
        Assert.Equal(SprintStatus.Planned, sprint.Value.Status);
        Assert.Equal(TeamId, sprint.Value.TeamId);
        Assert.Equal(Start, sprint.Value.StartDate);
        Assert.Equal(End, sprint.Value.EndDate);
        Assert.Null(sprint.Value.CompletedAt);
        Assert.False(sprint.Value.IsActive);
        Assert.False(sprint.Value.IsClosed);
    }

    [Fact]
    public void TheNameAndGoalAreTrimmed()
    {
        var sprint = Sprint.Create(TeamId, "  Sprint 12  ", "  Cerrar el checkout  ", Start, End, Now);

        Assert.Equal("Sprint 12", sprint.Value.Name);
        Assert.Equal("Cerrar el checkout", sprint.Value.Goal);
    }

    [Fact]
    public void AnEmptyGoalIsStoredAsNull()
    {
        var sprint = Sprint.Create(TeamId, "Sprint 12", "   ", Start, End, Now);

        Assert.Null(sprint.Value.Goal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ASprintWithoutANameIsRejected(string name)
    {
        var sprint = Sprint.Create(TeamId, name, null, Start, End, Now);

        Assert.True(sprint.IsFailure);
        Assert.Equal(SprintErrors.NameRequired, sprint.Error);
    }

    [Fact]
    public void ANameLongerThanTheLimitIsRejected()
    {
        var sprint = Sprint.Create(TeamId, new string('a', Sprint.MaxNameLength + 1), null, Start, End, Now);

        Assert.True(sprint.IsFailure);
        Assert.Equal(SprintErrors.NameTooLong, sprint.Error);
    }

    [Fact]
    public void AGoalLongerThanTheLimitIsRejected()
    {
        var sprint = Sprint.Create(
            TeamId, "Sprint 12", new string('a', Sprint.MaxGoalLength + 1), Start, End, Now);

        Assert.True(sprint.IsFailure);
        Assert.Equal(SprintErrors.GoalTooLong, sprint.Error);
    }

    /// <summary>"EndDate debe ser posterior a StartDate" (task 007). Iguales no alcanza.</summary>
    [Fact]
    public void AnEndDateThatIsNotAfterTheStartDateIsRejected()
    {
        var sameDay = Sprint.Create(TeamId, "Sprint 12", null, Start, Start, Now);
        var backwards = Sprint.Create(TeamId, "Sprint 12", null, End, Start, Now);

        Assert.Equal(SprintErrors.EndDateNotAfterStartDate, sameDay.Error);
        Assert.Equal(SprintErrors.EndDateNotAfterStartDate, backwards.Error);
    }

    [Fact]
    public void StartingPutsTheSprintInProgress()
    {
        var sprint = ASprint();

        var started = sprint.Start(Later);

        Assert.True(started.IsSuccess);
        Assert.Equal(SprintStatus.Active, sprint.Status);
        Assert.True(sprint.IsActive);
        Assert.Equal(Later, sprint.UpdatedAt);
        Assert.Null(sprint.CompletedAt);
    }

    [Fact]
    public void OnlyAPlannedSprintCanBeStarted()
    {
        var active = AnActiveSprint();

        var again = active.Start(Later);

        Assert.True(again.IsFailure);
        Assert.Equal(SprintErrors.NotPlanned, again.Error);
    }

    [Fact]
    public void CompletingClosesTheSprintAndStampsTheDate()
    {
        var sprint = AnActiveSprint();

        var completed = sprint.Complete(Later);

        Assert.True(completed.IsSuccess);
        Assert.Equal(SprintStatus.Completed, sprint.Status);
        Assert.Equal(Later, sprint.CompletedAt);
        Assert.True(sprint.IsClosed);
    }

    [Fact]
    public void OnlyAnActiveSprintCanBeCompleted()
    {
        var planned = ASprint();

        var completed = planned.Complete(Later);

        Assert.True(completed.IsFailure);
        Assert.Equal(SprintErrors.NotActive, completed.Error);
    }

    [Fact]
    public void APlannedSprintCanBeCanceled()
    {
        var sprint = ASprint();

        var canceled = sprint.Cancel(Later);

        Assert.True(canceled.IsSuccess);
        Assert.Equal(SprintStatus.Canceled, sprint.Status);
        Assert.True(sprint.IsClosed);
    }

    [Fact]
    public void AnActiveSprintCanBeCanceled()
    {
        var sprint = AnActiveSprint();

        var canceled = sprint.Cancel(Later);

        Assert.True(canceled.IsSuccess);
        Assert.Equal(SprintStatus.Canceled, sprint.Status);
    }

    /// <summary>
    /// Cancelar no es completar: confundirlos ensuciaría cualquier métrica que cuente
    /// sprints terminados.
    /// </summary>
    [Fact]
    public void CancelingDoesNotStampTheCompletionDate()
    {
        var sprint = AnActiveSprint();

        sprint.Cancel(Later);

        Assert.Null(sprint.CompletedAt);
    }

    [Fact]
    public void AClosedSprintCannotBeCanceledAgain()
    {
        var completed = AnActiveSprint();
        completed.Complete(Later);

        var canceled = completed.Cancel(Later.AddDays(1));

        Assert.True(canceled.IsFailure);
        Assert.Equal(SprintErrors.Closed, canceled.Error);
    }

    [Fact]
    public void AClosedSprintCannotBeStarted()
    {
        var canceled = ASprint();
        canceled.Cancel(Later);

        var started = canceled.Start(Later.AddDays(1));

        Assert.True(started.IsFailure);
        Assert.Equal(SprintErrors.NotPlanned, started.Error);
    }

    [Fact]
    public void UpdatingChangesTheNameGoalAndDates()
    {
        var sprint = ASprint();
        var newEnd = End.AddDays(7);

        var updated = sprint.Update("Sprint 13", "Otro objetivo", Start, newEnd, Later);

        Assert.True(updated.IsSuccess);
        Assert.Equal("Sprint 13", sprint.Name);
        Assert.Equal("Otro objetivo", sprint.Goal);
        Assert.Equal(newEnd, sprint.EndDate);
        Assert.Equal(Later, sprint.UpdatedAt);
    }

    [Fact]
    public void AnActiveSprintCanStillBeUpdated()
    {
        var sprint = AnActiveSprint();

        var updated = sprint.Update("Sprint 12 (extendido)", null, Start, End.AddDays(7), Later);

        Assert.True(updated.IsSuccess);
        Assert.Equal(SprintStatus.Active, sprint.Status);
    }

    /// <summary>Un sprint cerrado es el registro de lo que pasó: no se reescribe.</summary>
    [Fact]
    public void AClosedSprintCannotBeUpdated()
    {
        var sprint = AnActiveSprint();
        sprint.Complete(Later);

        var updated = sprint.Update("Otro nombre", null, Start, End, Later.AddDays(1));

        Assert.True(updated.IsFailure);
        Assert.Equal(SprintErrors.Closed, updated.Error);
        Assert.Equal("Sprint 12", sprint.Name);
    }

    [Fact]
    public void UpdatingWithInvalidDatesIsRejectedAndLeavesTheSprintIntact()
    {
        var sprint = ASprint();

        var updated = sprint.Update("Sprint 12", null, End, Start, Later);

        Assert.True(updated.IsFailure);
        Assert.Equal(SprintErrors.EndDateNotAfterStartDate, updated.Error);
        Assert.Equal(Start, sprint.StartDate);
        Assert.Equal(End, sprint.EndDate);
    }
}
