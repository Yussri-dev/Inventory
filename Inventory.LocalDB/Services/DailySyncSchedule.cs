namespace Inventory.LocalDB.Services;

public static class DailySyncSchedule
{
    // localNow is the store computer's clock, not the API server's timezone.
    public static DateOnly? GetDueDate(DateTime localNow, DateOnly enabledOn, DateOnly? lastAttempt)
    {
        var due = DateOnly.FromDateTime(localNow);
        if (localNow.TimeOfDay < TimeSpan.FromHours(21)) due = due.AddDays(-1);
        return due >= enabledOn && (!lastAttempt.HasValue || due > lastAttempt.Value) ? due : null;
    }
}
