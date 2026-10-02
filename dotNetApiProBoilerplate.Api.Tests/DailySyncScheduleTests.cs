using Inventory.LocalDB.Services;

public class DailySyncScheduleTests
{
    [Theory]
    [InlineData("2026-09-17T20:59:59", "2026-09-17", null, null)]
    [InlineData("2026-09-17T21:00:00", "2026-09-17", null, "2026-09-17")]
    [InlineData("2026-09-17T23:00:00", "2026-09-17", "2026-09-17", null)]
    [InlineData("2026-09-18T08:00:00", "2026-09-17", null, "2026-09-17")]
    [InlineData("2026-09-18T08:00:00", "2026-09-17", "2026-09-17", null)]
    [InlineData("2026-09-20T08:00:00", "2026-09-17", "2026-09-17", "2026-09-19")]
    [InlineData("2026-09-18T21:00:00", "2026-09-17", "2026-09-17", "2026-09-18")]
    [InlineData("2026-10-25T02:30:00", "2026-10-24", "2026-10-24", null)]
    public void RunsOnceAtLocal21AndCatchesUpMissedDays(string now, string enabled, string? last, string? expected)
    {
        Assert.Equal(expected == null ? (DateOnly?)null : DateOnly.Parse(expected),
            DailySyncSchedule.GetDueDate(DateTime.Parse(now), DateOnly.Parse(enabled),
                last == null ? null : DateOnly.Parse(last)));
    }
}
