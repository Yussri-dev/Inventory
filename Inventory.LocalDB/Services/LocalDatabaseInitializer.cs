using Inventory.LocalDB.Context;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace Inventory.LocalDB.Services
{
    public class LocalDatabaseInitializer : ILocalDatabaseInitializer
    {
        private readonly PosLocalDbContext _db;

        public LocalDatabaseInitializer(PosLocalDbContext db)
        {
            _db = db;
        }

        public async Task InitializeAsync()
        {
            await _db.Database.MigrateAsync();
        }
    }
}
