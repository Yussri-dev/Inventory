using Inventory.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Models
{
    public class Tenant
    {
        [Key]
        public Guid Id { get; set; }

        // ===============================
        // INFORMATIONS DE BASE
        // ===============================

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(200)]
        public string? LegalName { get; set; }

        [MaxLength(100)]
        public string? TradeName { get; set; }

        [MaxLength(50)]
        public string? TaxNumber { get; set; } // TVA, SIRET, etc.

        [MaxLength(50)]
        public string? RegistrationNumber { get; set; } // Numéro de registre de commerce

        // ===============================
        // ADRESSE & CONTACT
        // ===============================

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; } // Province/Région

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string? Mobile { get; set; }

        [MaxLength(50)]
        public string? Fax { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(200)]
        public string? Website { get; set; }

        // ===============================
        // PARAMÈTRES RÉGIONAUX
        // ===============================

        [Required, MaxLength(3)]
        public string Currency { get; set; } = "EUR";

        [MaxLength(10)]
        public string? CurrencySymbol { get; set; } = "€";

        [MaxLength(10)]
        public string? CurrencyPosition { get; set; } = "after"; // before, after

        [MaxLength(10)]
        public string Locale { get; set; } = "fr-BE"; // fr-BE, nl-BE, en-US, etc.

        [MaxLength(50)]
        public string TimeZone { get; set; } = "Europe/Brussels";

        [MaxLength(20)]
        public string DateFormat { get; set; } = "dd/MM/yyyy";

        [MaxLength(20)]
        public string TimeFormat { get; set; } = "HH:mm";

        public int DecimalPlaces { get; set; } = 2;

        // ===============================
        // CONFIGURATION MÉTIER
        // ===============================

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal DefaultVatRate { get; set; } = 21.00m;

        public bool AutoGenerateInvoiceNumbers { get; set; } = true;

        [MaxLength(20)]
        public string? InvoicePrefix { get; set; } = "INV";

        public bool RequireCustomerForSales { get; set; } = false;

        public bool AllowNegativeStock { get; set; } = false;

        public bool EnableLoyaltyProgram { get; set; } = true;

        public bool EnableMultiCurrency { get; set; } = false;

        public bool EnableBarcodePrinting { get; set; } = true;

        public bool EnableReceiptEmail { get; set; } = true;

        // ===============================
        // PLAN & LIMITES
        // ===============================

        [MaxLength(50)]
        public string SubscriptionPlan { get; set; } = "Free"; // Free, Starter, Professional, Enterprise

        public DateTime? SubscriptionStartDate { get; set; }

        public DateTime? SubscriptionEndDate { get; set; }

        public bool IsTrialActive { get; set; }

        public DateTime? TrialEndDate { get; set; }

        // Limites du plan
        public int MaxUsers { get; set; } = 20;
        public int MaxProducts { get; set; } = 1000;
        public int MaxLocations { get; set; } = 1;
        public int MaxMonthlyTransactions { get; set; } = 10000;

        // Compteurs actuels
        public int CurrentUsers { get; set; }
        public int CurrentProducts { get; set; }
        public int CurrentLocations { get; set; }
        public int CurrentMonthTransactions { get; set; }

        public DateTime LastTransactionCountReset { get; set; }

        // ===============================
        // BRANDING
        // ===============================

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(500)]
        public string? FaviconUrl { get; set; }

        [MaxLength(7)]
        public string? PrimaryColor { get; set; } = "#1976d2";

        [MaxLength(7)]
        public string? SecondaryColor { get; set; } = "#424242";

        [MaxLength(2000)]
        public string? ReceiptHeader { get; set; } // Texte en-tête des tickets

        [MaxLength(2000)]
        public string? ReceiptFooter { get; set; } // Texte pied de page des tickets

        // ===============================
        // INFORMATIONS BANCAIRES
        // ===============================

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(50)]
        public string? BankAccountNumber { get; set; }

        [MaxLength(50)]
        public string? IBAN { get; set; }

        [MaxLength(50)]
        public string? BIC { get; set; }

        [MaxLength(50)]
        public string? SwiftCode { get; set; }

        // ===============================
        // SÉCURITÉ & CONFORMITÉ
        // ===============================

        public bool RequireTwoFactorAuth { get; set; } = false;

        public bool EnableDataEncryption { get; set; } = true;

        public bool EnableAuditLog { get; set; } = true;

        public int PasswordExpiryDays { get; set; } = 90;

        public int SessionTimeoutMinutes { get; set; } = 60;

        public bool EnableGDPRCompliance { get; set; } = true;

        public int DataRetentionYears { get; set; } = 7;

        [MaxLength(2000)]
        public string? PrivacyPolicyUrl { get; set; }

        [MaxLength(2000)]
        public string? TermsOfServiceUrl { get; set; }

        // ===============================
        // INTÉGRATIONS
        // ===============================

        public bool EnableEmailNotifications { get; set; } = true;

        [MaxLength(200)]
        public string? SmtpHost { get; set; }

        public int? SmtpPort { get; set; }

        [MaxLength(100)]
        public string? SmtpUsername { get; set; }

        [MaxLength(500)]
        public string? SmtpPassword { get; set; } // À chiffrer

        public bool SmtpUseSsl { get; set; } = true;

        [MaxLength(100)]
        public string? FromEmail { get; set; }

        [MaxLength(200)]
        public string? FromName { get; set; }

        // ===============================
        // ÉTAT & DATES
        // ===============================

        public bool IsActive { get; set; } = true;

        public bool IsSuspended { get; set; }

        [MaxLength(500)]
        public string? SuspensionReason { get; set; }

        public DateTime? SuspendedAt { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public Guid CreatedByUserId { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public Guid? UpdatedByUserId { get; set; }

        public DateTime? LastActivityAt { get; set; }

        // ===============================
        // MÉTADONNÉES
        // ===============================

        [MaxLength(10)]
        public string? IndustryType { get; set; } // Retail, Restaurant, Service, etc.

        [MaxLength(10)]
        public string? BusinessSize { get; set; } // Small, Medium, Large

        [MaxLength(2000)]
        public string? Notes { get; set; }

        [MaxLength(4000)]
        public string? CustomSettings { get; set; } // JSON pour paramètres personnalisés

        // ===============================
        // NAVIGATION PROPERTIES
        // ===============================

        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

        public ICollection<Product> Products { get; set; } = new List<Product>();

        public ICollection<Customer> Customers { get; set; } = new List<Customer>();

        public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();

        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();

        public ICollection<Stock> Stocks { get; set; } = new List<Stock>();

        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

        public ICollection<CashSession> CashSessions { get; set; } = new List<CashSession>();

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        public ICollection<Return> Returns { get; set; } = new List<Return>();

        public ICollection<SupplierReturn> SupplierReturns { get; set; } = new List<SupplierReturn>();

        public ICollection<Damage> Damages { get; set; } = new List<Damage>();

        public ICollection<InventorySession> InventorySessions { get; set; } = new List<InventorySession>();

        public ICollection<LoyaltyCard> LoyaltyCards { get; set; } = new List<LoyaltyCard>();

        public ICollection<DocumentNumber> DocumentNumbers { get; set; } = new List<DocumentNumber>();

        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        public ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

        public ICollection<SystemConfiguration> Configurations { get; set; } = new List<SystemConfiguration>();

        // ===============================
        // MÉTHODES UTILITAIRES
        // ===============================

        public bool CanAddUser()
        {
            return CurrentUsers < MaxUsers;
        }

        public bool CanAddProduct()
        {
            return CurrentProducts < MaxProducts;
        }

        public bool HasFeature(string featureName)
        {
            return SubscriptionPlan switch
            {
                "Free" => featureName switch
                {
                    "BasicPOS" => true,
                    "Inventory" => true,
                    "Reports" => false,
                    "MultiLocation" => false,
                    "API" => false,
                    _ => false
                },
                "Starter" => featureName switch
                {
                    "BasicPOS" => true,
                    "Inventory" => true,
                    "Reports" => true,
                    "MultiLocation" => false,
                    "API" => false,
                    _ => false
                },
                "Professional" => featureName switch
                {
                    "BasicPOS" => true,
                    "Inventory" => true,
                    "Reports" => true,
                    "MultiLocation" => true,
                    "API" => true,
                    "AdvancedReports" => true,
                    _ => false
                },
                "Enterprise" => true,
                _ => false
            };
        }

        public bool IsSubscriptionActive()
        {
            if (IsTrialActive && TrialEndDate.HasValue && TrialEndDate.Value > DateTime.UtcNow)
                return true;

            if (SubscriptionEndDate.HasValue && SubscriptionEndDate.Value > DateTime.UtcNow)
                return true;

            return false;
        }

        public int GetDaysUntilExpiry()
        {
            DateTime? expiryDate = IsTrialActive ? TrialEndDate : SubscriptionEndDate;

            if (!expiryDate.HasValue)
                return 0;

            return Math.Max(0, (expiryDate.Value - DateTime.UtcNow).Days);
        }
    }
}
