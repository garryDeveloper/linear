using Linear.Web.Components.Common;
using Linear.Web.Features.Roadmaps.Contracts;

namespace Linear.UnitTests.Roadmaps;

/// <summary>
/// La escala de la línea de tiempo: convertir fechas en porcentajes es justo el tipo de
/// cuenta que se rompe en silencio, porque una barra mal ubicada sigue dibujándose.
/// </summary>
public class RoadmapTimelineTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    private static RoadmapItemResponse AnItem(DateOnly start, DateOnly target, string name = "Iniciativa") =>
        new(
            Guid.CreateVersion7(),
            name,
            null,
            nameof(Linear.Domain.Roadmaps.RoadmapItemStatus.Planned),
            start,
            target,
            RoadmapItemProgress.Empty,
            Now,
            Now);

    [Fact]
    public void WithoutItemsThereIsNoTimeline()
    {
        Assert.Null(RoadmapTimeline.Create([]));
    }

    /// <summary>
    /// El rango se redondea a meses completos: si arrancara un día 17, la primera columna
    /// sería un mes cortado y se leería mal.
    /// </summary>
    [Fact]
    public void TheRangeSnapsToWholeMonths()
    {
        var timeline = RoadmapTimeline.Create(
            [AnItem(new DateOnly(2026, 8, 17), new DateOnly(2026, 10, 5))]);

        Assert.Equal(new DateOnly(2026, 8, 1), timeline!.From);
        Assert.Equal(new DateOnly(2026, 10, 31), timeline.To);
    }

    [Fact]
    public void TheRangeCoversEveryItem()
    {
        var timeline = RoadmapTimeline.Create(
        [
            AnItem(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)),
            AnItem(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 20)),
            AnItem(new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 15))
        ]);

        Assert.Equal(new DateOnly(2026, 8, 1), timeline!.From);
        Assert.Equal(new DateOnly(2026, 12, 31), timeline.To);
        Assert.Equal(5, timeline.Months.Count);
    }

    [Fact]
    public void ThereIsOneColumnPerMonth()
    {
        var timeline = RoadmapTimeline.Create(
            [AnItem(new DateOnly(2026, 8, 1), new DateOnly(2026, 10, 31))]);

        Assert.Equal(3, timeline!.Months.Count);
        Assert.Equal(0, timeline.Months[0].LeftPercent);
    }

    /// <summary>Las columnas cubren el ancho entero, sin huecos ni superposición.</summary>
    [Fact]
    public void TheMonthColumnsFillTheWholeWidth()
    {
        var timeline = RoadmapTimeline.Create(
            [AnItem(new DateOnly(2026, 8, 1), new DateOnly(2026, 12, 31))]);

        var last = timeline!.Months[^1];

        Assert.Equal(100, last.LeftPercent + last.WidthPercent, 3);

        for (var index = 1; index < timeline.Months.Count; index++)
        {
            var previous = timeline.Months[index - 1];

            Assert.Equal(
                timeline.Months[index].LeftPercent,
                previous.LeftPercent + previous.WidthPercent,
                3);
        }
    }

    /// <summary>Una iniciativa que ocupa todo el rango se dibuja de punta a punta.</summary>
    [Fact]
    public void AnItemSpanningTheWholeRangeFillsTheBar()
    {
        var item = AnItem(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        var bar = RoadmapTimeline.Create([item])!.PositionOf(item);

        Assert.Equal(0, bar.LeftPercent);
        Assert.Equal(100, bar.WidthPercent, 3);
    }

    [Fact]
    public void AnItemInTheSecondHalfStartsHalfway()
    {
        // Rango: agosto y septiembre (31 + 30 = 61 días). Septiembre arranca el día 32.
        var first = AnItem(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));
        var second = AnItem(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        var timeline = RoadmapTimeline.Create([first, second])!;
        var bar = timeline.PositionOf(second);

        Assert.Equal(31 * 100d / 61, bar.LeftPercent, 3);
        Assert.Equal(30 * 100d / 61, bar.WidthPercent, 3);
    }

    /// <summary>
    /// El largo cuenta las dos puntas: del 1 al 31 hay 31 días, no 30. Un error de un día
    /// acá dejaría todas las barras cortas.
    /// </summary>
    [Fact]
    public void TheBarLengthIncludesBothEnds()
    {
        var item = AnItem(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 10));
        var filler = AnItem(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        var bar = RoadmapTimeline.Create([item, filler])!.PositionOf(item);

        Assert.Equal(10 * 100d / 31, bar.WidthPercent, 3);
    }

    [Fact]
    public void NoBarEscapesTheTimeline()
    {
        var items = new[]
        {
            AnItem(new DateOnly(2026, 8, 3), new DateOnly(2026, 9, 15)),
            AnItem(new DateOnly(2026, 10, 20), new DateOnly(2026, 12, 28))
        };

        var timeline = RoadmapTimeline.Create(items)!;

        foreach (var item in items)
        {
            var bar = timeline.PositionOf(item);

            Assert.True(bar.LeftPercent >= 0);
            Assert.True(bar.WidthPercent > 0);
            Assert.True(bar.LeftPercent + bar.WidthPercent <= 100.001);
        }
    }

    [Fact]
    public void TheTimelineCanCrossYears()
    {
        var timeline = RoadmapTimeline.Create(
            [AnItem(new DateOnly(2026, 11, 1), new DateOnly(2027, 2, 28))]);

        Assert.Equal(new DateOnly(2026, 11, 1), timeline!.From);
        Assert.Equal(new DateOnly(2027, 2, 28), timeline.To);
        Assert.Equal(4, timeline.Months.Count);

        // El año aparece solo en enero, para no repetirlo en cada columna.
        Assert.Contains("2027", timeline.Months[2].Label);
        Assert.DoesNotContain("2026", timeline.Months[0].Label);
    }

    [Fact]
    public void LeapYearsAreCountedCorrectly()
    {
        var timeline = RoadmapTimeline.Create(
            [AnItem(new DateOnly(2028, 2, 1), new DateOnly(2028, 2, 20))]);

        // 2028 es bisiesto: febrero termina el 29.
        Assert.Equal(new DateOnly(2028, 2, 29), timeline!.To);
    }
}
