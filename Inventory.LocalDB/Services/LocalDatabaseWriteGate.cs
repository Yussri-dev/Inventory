
namespace Inventory.LocalDB.Services
{
    public static class LocalDatabaseWriteGate
    {
        public static SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
    }
}
