using Inventory.Dto.Sales.Results;
using System.Text;


namespace Inventory.Ui.Platforms.Windows.Printing
{
    public static class EscPosTicketBuilder
    {
        public static byte[] Build(SaleTicketResult t)
        {
            var b = new List<byte>();

            void txt(string s) => b.AddRange(Encoding.ASCII.GetBytes(s));

            txt("\x1B\x40");          // init
            txt("\x1B\x61\x01");      // center
            txt($"{t.CustomerName}\n");
            txt($"{t.StoreAddress}\n");
            txt("\n");

            txt("\x1B\x61\x00");      // left
            txt("--------------------------------\n");

            foreach (var l in t.Lines)
            {
                txt($"{l.ProductName}\n");
                txt($" {l.Quantity} x {l.UnitPrice:0.00}  {l.Total:0.00}\n");
            }

            txt("--------------------------------\n");
            txt($"TOTAL   {t.Total:0.00} EUR\n");
            txt($"PAID    {t.Paid:0.00} EUR\n");
            txt($"CHANGE  {t.Change:0.00} EUR\n");

            txt("\n\n");
            txt("\x1D\x56\x00");      // cut

            return b.ToArray();
        }
    }
}
