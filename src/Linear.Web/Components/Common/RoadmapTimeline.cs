using System.Globalization;

using Linear.Web.Features.Roadmaps.Contracts;

namespace Linear.Web.Components.Common;

/// <summary>Una columna de mes en la cabecera de la línea de tiempo.</summary>
public sealed record RoadmapTimelineMonth(string Label, double LeftPercent, double WidthPercent);

/// <summary>Posición y largo de la barra de una iniciativa, en porcentaje del ancho total.</summary>
public sealed record RoadmapTimelineBar(double LeftPercent, double WidthPercent);

/// <summary>
/// Calcula la escala de la línea de tiempo del roadmap.
/// </summary>
/// <remarks>
/// Todo se expresa en porcentajes y no en píxeles: el ancho real lo pone el CSS, así que la
/// línea de tiempo se adapta sola a la pantalla sin recalcular nada.
///
/// La escala arranca el primer día del mes de la iniciativa más temprana y termina el último
/// día del mes de la más tardía. Redondear a meses completos es lo que permite dibujar la
/// cabecera con columnas parejas: si el rango empezara un día 17, la primera columna sería
/// un mes cortado y se leería mal.
/// </remarks>
public sealed class RoadmapTimeline
{
    private readonly int _totalDays;

    private RoadmapTimeline(DateOnly from, DateOnly to, IReadOnlyList<RoadmapTimelineMonth> months)
    {
        From = from;
        To = to;
        Months = months;

        // Inclusivo en las dos puntas: del 1 al 31 hay 31 días, no 30.
        _totalDays = to.DayNumber - from.DayNumber + 1;
    }

    public DateOnly From { get; }

    public DateOnly To { get; }

    public IReadOnlyList<RoadmapTimelineMonth> Months { get; }

    /// <summary>
    /// Arma la escala a partir de las iniciativas. Devuelve <c>null</c> si no hay ninguna:
    /// una línea de tiempo sin nada que ubicar no tiene rango que representar.
    /// </summary>
    public static RoadmapTimeline? Create(IReadOnlyList<RoadmapItemResponse> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            return null;
        }

        var earliest = items.Min(item => item.StartDate);
        var latest = items.Max(item => item.TargetDate);

        var from = new DateOnly(earliest.Year, earliest.Month, 1);
        var to = new DateOnly(latest.Year, latest.Month, 1).AddMonths(1).AddDays(-1);

        var totalDays = to.DayNumber - from.DayNumber + 1;
        var months = new List<RoadmapTimelineMonth>();

        // El ancho de cada mes sale de restar dos bordes ya redondeados, no de redondear el
        // ancho por su cuenta: así el borde derecho de una columna cae exactamente sobre el
        // izquierdo de la siguiente, y las columnas embaldosan sin huecos ni solapes.
        for (var month = from; month <= to; month = month.AddMonths(1))
        {
            var left = Percent(month.DayNumber - from.DayNumber, totalDays);
            var right = Percent(month.AddMonths(1).DayNumber - from.DayNumber, totalDays);

            months.Add(new RoadmapTimelineMonth(LabelFor(month), left, right - left));
        }

        return new RoadmapTimeline(from, to, months);
    }

    /// <summary>
    /// Ubica una iniciativa dentro de la escala.
    /// </summary>
    /// <remarks>
    /// La barra se recorta al rango de la línea de tiempo. En la práctica nunca se sale
    /// —el rango se calculó a partir de estas mismas fechas—, pero recortar evita que un
    /// redondeo deje una barra asomando fuera del área dibujada.
    /// </remarks>
    public RoadmapTimelineBar PositionOf(RoadmapItemResponse item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var start = Math.Max(item.StartDate.DayNumber, From.DayNumber);
        var end = Math.Min(item.TargetDate.DayNumber, To.DayNumber);

        // Mismo criterio que con los meses: el ancho es la diferencia entre dos bordes. El
        // borde derecho es el día siguiente al último, porque la barra incluye las dos
        // puntas — del 1 al 31 hay 31 días, no 30.
        var left = Percent(start - From.DayNumber, _totalDays);
        var right = Percent(Math.Max(end + 1, start + 1) - From.DayNumber, _totalDays);

        return new RoadmapTimelineBar(left, right - left);
    }

    private static double Percent(int days, int totalDays) =>
        totalDays <= 0 ? 0 : Math.Round(days * 100d / totalDays, 4);

    /// <summary>
    /// Etiqueta del mes. Incluye el año solo en enero, para no repetirlo en cada columna
    /// cuando la línea de tiempo cruza varios años.
    /// </summary>
    private static string LabelFor(DateOnly month)
    {
        var culture = CultureInfo.GetCultureInfo("es-ES");
        var name = culture.DateTimeFormat.GetAbbreviatedMonthName(month.Month).TrimEnd('.');

        name = culture.TextInfo.ToTitleCase(name);

        return month.Month == 1 ? $"{name} {month.Year}" : name;
    }
}
