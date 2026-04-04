namespace Inventory.Dto.Enums
{
    public enum UserRole
    {
        SuperAdmin = 0,   // Accès complet multi-tenant
        Admin = 1,        // Admin du tenant
        Manager = 2,      // Gestionnaire
        Cashier = 3,      // Caissier
        StockManager = 4,  // Gestionnaire de stock
        Viewer = 5
    }
}
