namespace Inventory.Infrastructure.Data.Configurations
{
    // Entity Framework Core configuration class for the Product entity
    //
    // This class is intentionally empty for now.
    // Its role is architectural, not accidental.
    //
    // Purpose of this folder and class:
    // - Centralize EF Core Fluent API configuration
    // - Keep BoilerplateDbContext clean (no long OnModelCreating blocks)
    // - Allow fine-grained control over table mapping, constraints, indexes, etc.
    //
    // What is typically added here (by design, NOT added yet):
    // - Implementation of IEntityTypeConfiguration<Product>
    // - Table name configuration
    // - Property constraints (max length, required, precision)
    // - Enum-to-string or enum-to-int conversions
    // - Indexes (unique name, search optimization)
    // - Default values (CreatedAt, Status, etc.)
    //
    // Example responsibilities (documented, not implemented here):
    // - builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
    // - builder.Property(p => p.Price).HasPrecision(18,2);
    // - builder.HasIndex(p => p.Name).IsUnique();
    //
    // Why keeping it empty is acceptable in a boilerplate:
    // - Shows the intended extension point
    // - Avoids premature constraints
    // - Makes the architecture explicit for buyers/readers
    // - Encourages correct EF Core usage patterns
    //
    // This class exists to signal:
    // “Domain rules and persistence rules belong here, not in DbContext.”
    public class ProductConfiguration
    {
    }
}
