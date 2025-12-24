
using Inventory.Domain.Abstraction;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Models
{
    public class SupplierReturn : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string ReturnNumber { get; set; } = null!;

        [Required]
        public Guid SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public Supplier Supplier { get; set; } = null!;

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public SupplierReturnStatus Status { get; set; }

        public DateTime ReturnDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public Guid? PurchaseId { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Navigation properties
        public ICollection<SupplierReturnLine> Lines { get; set; } = new List<SupplierReturnLine>();
    }

    public class SupplierReturnLine
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SupplierReturnId { get; set; }

        [ForeignKey(nameof(SupplierReturnId))]
        public SupplierReturn SupplierReturn { get; set; } = null!;

        [Required]
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [Column(TypeName = "decimal(18,3)")]
        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal UnitPurchasePrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineAmount => Quantity * UnitPurchasePrice;

        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    public class Damage : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string DamageNumber { get; set; } = null!;

        [Required]
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [Column(TypeName = "decimal(18,3)")]
        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedValue { get; set; }

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = null!;

        [MaxLength(100)]
        public string? Category { get; set; } // Breakage, Expiry, Theft, etc.

        public DateTime DamageDate { get; set; }

        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public Guid? ApprovedByUserId { get; set; }

        [MaxLength(500)]
        public string? Photos { get; set; } // JSON array of photo URLs

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    // ===============================
    // 9. INVENTORY COUNT
    // ===============================

    public class InventorySession : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string SessionNumber { get; set; } = null!;

        [MaxLength(200)]
        public string? Name { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? ValidatedAt { get; set; }

        [Required]
        public InventoryStatus Status { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        public Guid? ValidatedByUserId { get; set; }

        [ForeignKey(nameof(ValidatedByUserId))]
        public ApplicationUser? ValidatedByUser { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public int TotalProductsCounted { get; set; }
        public int TotalDiscrepancies { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVarianceValue { get; set; }

        // Navigation properties
        public ICollection<InventoryLine> Lines { get; set; } = new List<InventoryLine>();
    }

    public class InventoryLine : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid InventorySessionId { get; set; }

        [ForeignKey(nameof(InventorySessionId))]
        public InventorySession InventorySession { get; set; } = null!;

        [Required]
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [Column(TypeName = "decimal(18,3)")]
        public decimal SystemQuantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal CountedQuantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Variance => CountedQuantity - SystemQuantity;

        [Column(TypeName = "decimal(18,2)")]
        public decimal VarianceValue { get; set; }

        public DateTime? CountedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public bool IsAdjusted { get; set; }
        public DateTime? AdjustedAt { get; set; }
    }

    // ===============================
    // 10. LOYALTY & RECEIPTS
    // ===============================

    public class LoyaltyCard : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string CardNumber { get; set; } = null!;

        [Required]
        public Guid CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; } = null!;

        public int CurrentPoints { get; set; }
        public int LifetimePoints { get; set; }

        public bool IsActive { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime IssuedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        public ICollection<LoyaltyTransaction> Transactions { get; set; } = new List<LoyaltyTransaction>();
    }

    public class LoyaltyTransaction : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid LoyaltyCardId { get; set; }

        [ForeignKey(nameof(LoyaltyCardId))]
        public LoyaltyCard LoyaltyCard { get; set; } = null!;

        public Guid? SaleId { get; set; }

        [ForeignKey(nameof(SaleId))]
        public Sale? Sale { get; set; }

        public int PointsChange { get; set; }
        public int PointsBefore { get; set; }
        public int PointsAfter { get; set; }

        [Required, MaxLength(200)]
        public string Reason { get; set; } = null!;

        public DateTime TransactionDate { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }

    public class SaleReceipt : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SaleId { get; set; }

        [ForeignKey(nameof(SaleId))]
        public Sale Sale { get; set; } = null!;

        [Required, MaxLength(100)]
        public string ReceiptNumber { get; set; } = null!;

        [Required]
        public string ReceiptData { get; set; } = null!; // JSON ou HTML

        [MaxLength(50)]
        public string Format { get; set; } = "JSON"; // JSON, HTML, XML

        public DateTime GeneratedAt { get; set; }

        public bool IsPrinted { get; set; }
        public DateTime? PrintedAt { get; set; }

        public bool IsEmailed { get; set; }
        public DateTime? EmailedAt { get; set; }

        [MaxLength(100)]
        public string? EmailAddress { get; set; }
    }

    // ===============================
    // 11. DOCUMENT NUMBERING
    // ===============================

    public class DocumentNumber : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(50)]
        public string DocumentType { get; set; } = null!; // Sale, Purchase, Return, etc.

        [Required, MaxLength(20)]
        public string Prefix { get; set; } = null!;

        public int LastNumber { get; set; }

        public int PaddingLength { get; set; } = 6;

        [MaxLength(20)]
        public string? Suffix { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }

        public bool ResetYearly { get; set; }
        public bool ResetMonthly { get; set; }
    }

    // ===============================
    // 12. AUDIT LOG
    // ===============================

    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string EntityType { get; set; } = null!;

        [Required]
        public Guid EntityId { get; set; }

        [Required, MaxLength(50)]
        public string Action { get; set; } = null!; // Create, Update, Delete

        public string? OldValues { get; set; } // JSON

        public string? NewValues { get; set; } // JSON

        [MaxLength(500)]
        public string? ChangeSummary { get; set; }

        public DateTime CreatedAt { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        [Required]
        public Guid TenantId { get; set; }

        [ForeignKey(nameof(TenantId))]
        public Tenant Tenant { get; set; } = null!;

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }
    }

    // ===============================
    // 13. PROMOTIONS & DISCOUNTS
    // ===============================

    public class Promotion : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Code { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required, MaxLength(50)]
        public string Type { get; set; } = null!; // Percentage, FixedAmount, BuyXGetY

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinimumPurchaseAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaximumDiscountAmount { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }

        public int? MaxUsageCount { get; set; }
        public int CurrentUsageCount { get; set; }

        public int? MaxUsagePerCustomer { get; set; }

        [MaxLength(100)]
        public string? ApplicableToCategory { get; set; }

        public Guid? ApplicableToProductId { get; set; }

        public bool CombinableWithOtherPromotions { get; set; }
    }

    // ===============================
    // 14. REPORTING TABLES
    // ===============================

    public class SalesSummaryDaily : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime Date { get; set; }

        public int TotalTransactions { get; set; }
        public int TotalItems { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalRevenue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVat { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDiscount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashSales { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CardSales { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditSales { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AverageTransactionValue { get; set; }

        public DateTime GeneratedAt { get; set; }
    }

    // ===============================
    // 15. CONFIGURATIONS
    // ===============================

    public class SystemConfiguration : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string Key { get; set; } = null!;

        [Required]
        public string Value { get; set; } = null!;

        [MaxLength(50)]
        public string DataType { get; set; } = "String"; // String, Int, Decimal, Boolean, JSON

        [MaxLength(200)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsEditable { get; set; } = true;

        public DateTime LastModified { get; set; }
    }

}
