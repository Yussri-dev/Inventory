using Inventory.LocalDB.Services;

public class DailySyncScheduleTests
{
    [Fact]
    public void Failure_waits_five_minutes_but_does_not_consume_daily_success()
    {
        var local = new DateTime(2026, 10, 5, 21, 0, 0);
        var utc = new DateTimeOffset(2026, 10, 5, 19, 0, 0, TimeSpan.Zero);
        var enabled = DateOnly.FromDateTime(local);
        var retry = utc.AddMinutes(5);
        Assert.Equal(enabled, DailySyncSchedule.GetDueDate(local, enabled, null));
        Assert.False(DailySyncSchedule.CanRetry(utc.AddMinutes(4), retry));
        Assert.True(DailySyncSchedule.CanRetry(utc.AddMinutes(5), retry));
        Assert.Equal(enabled, DailySyncSchedule.GetDueDate(local.AddMinutes(5), enabled, null));
        Assert.Null(DailySyncSchedule.GetDueDate(local.AddMinutes(6), enabled, enabled));
    }
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
