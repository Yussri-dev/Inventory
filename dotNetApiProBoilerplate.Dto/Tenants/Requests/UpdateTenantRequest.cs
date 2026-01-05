using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.Tenants.Requests
{
    public class UpdateTenantRequest
    {
        public string? Name { get; set; }
        public string? LegalName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? TaxNumber { get; set; }
        public decimal? DefaultVatRate { get; set; }
    }
}
