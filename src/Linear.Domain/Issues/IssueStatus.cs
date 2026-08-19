namespace Linear.Domain.Issues;

/// <summary>
/// Estado de un issue dentro de su flujo de trabajo.
/// </summary>
public enum IssueStatus
{
    Backlog = 0,
    Todo = 1,
    InProgress = 2,
    InReview = 3,
    Done = 4,
    Canceled = 5
}
