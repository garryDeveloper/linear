using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Infrastructure.Issues;

/// <summary>
/// Reserva el próximo número de issue de un equipo.
/// </summary>
/// <remarks>
/// Recibe el <see cref="AppDbContext"/> de quien la llama en lugar de crear el suyo propio
/// —mismo patrón que <c>TeamResponseMapper</c>— porque la sentencia no necesita su propia
/// conexión: reutiliza la del handler.
/// <para>
/// No pasa por el <c>Team</c> rastreado por ese contexto: cargarlo, incrementar el número en
/// memoria y guardar dejaría una ventana donde dos requests concurrentes leen el mismo valor
/// y generan el mismo identificador. En cambio, ejecuta un único <c>UPDATE ... RETURNING</c>
/// —PostgreSQL serializa las escrituras concurrentes sobre la misma fila— así que el número
/// que vuelve ya es exclusivo de quien lo pidió.
/// </para>
/// </remarks>
public static class IssueNumberSequence
{
    public static async Task<int> NextAsync(
        AppDbContext dbContext,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var results = await dbContext.Database
            .SqlQuery<int>(
                $"""
                UPDATE "Teams"
                SET "LastIssueNumber" = "LastIssueNumber" + 1
                WHERE "Id" = {teamId}
                RETURNING "LastIssueNumber"
                """)
            .ToListAsync(cancellationToken);

        return results.Count == 1
            ? results[0]
            : throw new InvalidOperationException(
                $"No se pudo reservar un número de issue: el equipo '{teamId}' no existe.");
    }
}
