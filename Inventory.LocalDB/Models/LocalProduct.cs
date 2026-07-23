using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public sealed class LocalProduct : ILocalTenantEntity
    {
        /// <summary>
        /// Identifiant permanent dans SQLite.
        /// Les ventes, stocks et mouvements locaux utilisent cet ID.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Identifiant du Product dans la base serveur.
        /// Null tant que la création offline n'est pas synchronisée.
        /// </summary>
        public Guid? ServerId { get; set; }

        /// <summary>
        /// Magasin propriétaire du produit.
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Référence vers LocalProductCatalog.
        /// </summary>
        public Guid? CatalogProductId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Sku { get; set; }

        [MaxLength(100)]
        public string? Barcode { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(100)]
        public string? Brand { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice2 { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice3 { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MinStockLevel { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MaxStockLevel { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        /*
         * IsActive est conservé temporairement parce que plusieurs services
         * existants l'utilisent encore. Il doit toujours être cohérent
         * avec Status.
         */
        public bool IsActive { get; set; } = true;

        public ProductStatus Status { get; set; } =
            ProductStatus.Active;

        public bool IsTracked { get; set; } = true;

        [Column(TypeName = "decimal(18,3)")]
        public decimal LocalStockQuantity { get; set; }

        public bool IsPack { get; set; }

        /// <summary>
        /// Identifiant local du Product unitaire associé au pack.
        /// </summary>
        public Guid? UnitProductLocalId { get; set; }

        /// <summary>
        /// Identifiant serveur du Product unitaire associé au pack.
        /// </summary>
        public Guid? UnitProductServerId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal UnitsPerPack { get; set; } = 1;

        public bool IsDeletedLocally { get; set; }

        [Required]
        [MaxLength(50)]
        public string SyncStatus { get; set; } =
            SyncQueueStatus.Pending;

        public DateTime CreatedAtUtc { get; set; } =
            DateTime.UtcNow;

        public DateTime? ModifiedAtUtc { get; set; }

        public DateTime? DeletedAtUtc { get; set; }

        public DateTime? LastSyncedAtUtc { get; set; }

        /// <summary>
        /// Dernière date de modification reçue du serveur.
        /// Utile pour les conflits.
        /// </summary>
        public DateTime? ServerModifiedAtUtc { get; set; }
    }
}
