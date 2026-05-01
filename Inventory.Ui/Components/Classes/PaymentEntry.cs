using Inventory.Dto.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Ui.Components.Classes
{
    public class PaymentEntry
    {
        public PaymentType Type { get; set; }
        public decimal Amount { get; set; }            
        public decimal AmountReceived { get; set; }  
        public DateTime Timestamp { get; set; }
    }
}
