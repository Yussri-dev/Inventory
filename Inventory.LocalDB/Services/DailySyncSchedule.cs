namespace Inventory.LocalDB.Services;

public static class DailySyncSchedule
{
    // localNow is the store computer's clock, not the API server's timezone.
    public static DateOnly? GetDueDate(DateTime localNow, DateOnly enabledOn, DateOnly? lastSuccess)
    {
        var due = DateOnly.FromDateTime(localNow);
        if (localNow.TimeOfDay < TimeSpan.FromHours(21)) due = due.AddDays(-1);
        return due >= enabledOn && (!lastSuccess.HasValue || due > lastSuccess.Value) ? due : null;
    }

    public static bool CanRetry(DateTimeOffset utcNow, DateTimeOffset? retryAt) => !retryAt.HasValue || utcNow >= retryAt.Value;
}
