namespace Linear.Domain.Issues;

/// <summary>
/// Asociación entre un issue y una label.
/// </summary>
/// <remarks>
/// Vive dentro del agregado <see cref="Issue"/>, igual que <c>TeamMember</c> vive dentro de
/// <c>Team</c>: se crea y se quita a través de él. Referencia a la label solo por
/// identificador —nunca la carga como objeto— porque <c>Label</c> es la raíz de su propio
/// agregado.
/// </remarks>
public sealed class IssueLabel
{
    /// <summary>Requerido por EF Core para materializar la entidad.</summary>
    private IssueLabel()
    {
    }

    internal IssueLabel(Guid issueId, Guid labelId)
    {
        IssueId = issueId;
        LabelId = labelId;
    }

    public Guid IssueId { get; private set; }

    public Guid LabelId { get; private set; }
}
