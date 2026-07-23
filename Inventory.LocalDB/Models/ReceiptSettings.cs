using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.LocalDB.Models
{
    public sealed class ReceiptSettings
    {
        public string CompanyName { get; set; } =
            "My Store";

        public string? CompanyAddress { get; set; }

        public string? CompanyPhone { get; set; }

        public string? CompanyEmail { get; set; }

        public string? CompanyTaxNumber { get; set; }

        public string? DefaultCashierName { get; set; }

        public string? FooterText { get; set; } =
            "Thank you for your purchase.";
    }
}
