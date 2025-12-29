using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.Suppliers.Results
{
    public class SupplierResult
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string? ContactPerson { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public string? City { get; set; }

        public string? PostalCode { get; set; }

        public string? Country { get; set; }

        public string? TaxNumber { get; set; }

        public int PaymentTermsDays { get; set; } // Délai de paiement en jours

        public string? BankAccount { get; set; }

        public bool IsActive { get; set; }

        public string? Notes { get; set; }
    }
}
