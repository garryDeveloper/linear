namespace Linear.Web.Features.Issues.Update;

public sealed class UpdateIssueRequest
{
    public string Key { get; set; } = string.Empty;

    public string Identifier { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Versión del issue que tenía a la vista quien edita.
    /// </summary>
    /// <remarks>
    /// Es la estrategia de conflictos de la task 014, y usa <c>UpdatedAt</c> como versión en
    /// lugar de agregar una columna: el valor ya viaja en la respuesta del issue, así que
    /// quien lo mostró ya lo tiene. Si no coincide con el guardado, alguien más escribió en
    /// el medio y el cambio se rechaza en vez de pisarlo.
    /// <para>
    /// La comparación no puede hacerse solo dentro del handler: entre que este lee el issue
    /// y lo guarda pasan microsegundos, y el conflicto real ocurre en los minutos que alguien
    /// pasa escribiendo. Por eso la versión la aporta el cliente.
    /// </para>
    /// <para>
    /// Es opcional. Omitirla equivale a "guardá igual", que es lo razonable para un cliente
    /// de API que no mostró nada antes de escribir; la interfaz siempre la manda.
    /// </para>
    /// </remarks>
    public DateTimeOffset? ExpectedUpdatedAt { get; set; }
}
