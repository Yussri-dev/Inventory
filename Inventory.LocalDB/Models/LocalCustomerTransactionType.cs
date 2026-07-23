using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.LocalDB.Models
{
    public static class LocalCustomerTransactionType
    {
        public const string Credit = "Credit";
        public const string Debit = "Debit";
        public const string Payment = "Payment";
        public const string Refund = "Refund";
    }

    public static class LocalCustomerTransactionOrigin
    {
        public const string Manual = "Manual";
        public const string Sale = "Sale";
        public const string Return = "Return";
    }

    public static class LocalCustomerCashMovementType
    {
        public const string Payment = "CustomerPayment";
        public const string Refund = "CustomerRefund";
    }
}
