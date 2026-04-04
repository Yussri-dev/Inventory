namespace Inventory.Dto.Sales.Results
{
    public class SaleTicketResult
    {
        public string InvoiceNumber { get; set; } = "";
        public DateTime SaleDate { get; set; }

        // ── Informations du magasin (depuis Tenant) ──────────────
        public string StoreName { get; set; } = "";
        public string? StoreAddress { get; set; }
        public string? StoreCity { get; set; }
        public string? StorePostalCode { get; set; }
        public string? StorePhone { get; set; }
        public string? StoreTaxNumber { get; set; }    // Numéro TVA
        public string? ReceiptHeader { get; set; }     // Texte libre en-tête (Tenant.ReceiptHeader)
        public string? ReceiptFooter { get; set; }     // Texte libre pied de page (Tenant.ReceiptFooter)

        // ── Client ───────────────────────────────────────────────
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = "Walk-in customer";

        // ── Lignes de vente ──────────────────────────────────────
        public List<SaleTicketLineResult> Lines { get; set; } = new();

        // ── Paiements ────────────────────────────────────────────
        public List<TicketPaymentLine> Payments { get; set; } = new();

        // ── Totaux ───────────────────────────────────────────────
        public decimal Subtotal { get; set; }
        public decimal VatAmount { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Change { get; set; }
    }
}
