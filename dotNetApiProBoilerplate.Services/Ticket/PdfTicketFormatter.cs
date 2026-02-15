using Inventory.Dto.Sales.Results;
using Inventory.Services.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Inventory.Services.Ticket
{
    public sealed class PdfTicketFormatter : ITicketFormatter
    {
        public byte[] Format(SaleTicketResult ticket)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A7);
                    page.Margin(6);

                    page.Content().Column(col =>
                    {
                        col.Spacing(3);

                        col.Item().DefaultTextStyle(t =>
                            t.FontFamily("Courier New")
                             .FontSize(9)
                        );

                        // ================= HEADER =================
                        col.Item().Text($"TICKET : {ticket.InvoiceNumber}");
                        col.Item().Text($"DATE   : {ticket.SaleDate:dd/MM/yyyy HH:mm}");
                        col.Item().Text($"CUSTOMER: {(string.IsNullOrWhiteSpace(ticket.CustomerName) ? "Walk-in customer" : ticket.CustomerName)}");

                        col.Item().LineHorizontal(1);

                        // ================= TABLE HEADER =================
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(30).Text("QTY");
                            r.RelativeItem().Text("PRODUCT");
                            r.ConstantItem(45).AlignRight().Text("TOTAL");
                        });

                        col.Item().LineHorizontal(0.5f);

                        // ================= LINES =================
                        foreach (var l in ticket.Lines)
                        {
                            col.Item().Row(r =>
                            {
                                r.ConstantItem(30)
                                    .Text(l.Quantity.ToString());

                                r.RelativeItem()
                                    .Text(l.ProductName);

                                r.ConstantItem(45)
                                    .AlignRight()
                                    .Text(l.Total.ToString("0.00"));
                            });
                        }

                        col.Item().LineHorizontal(1);

                        // ================= TOTALS =================
                        col.Item().Text($"TOTAL  : {ticket.Total:0.00} €");
                        col.Item().Text($"PAID   : {ticket.Paid:0.00} €");
                        col.Item().Text($"CHANGE : {ticket.Change:0.00} €");

                        col.Item().PaddingTop(4);

                        // ================= FOOTER =================
                        col.Item().Text("LOW PRICE YOU CAN TRUST")
                            .AlignCenter()
                            .FontSize(8);

                        col.Item().Text(ticket.SaleDate.ToString("dd/MM/yyyy HH:mm"))
                            .AlignCenter()
                            .FontSize(8);
                    });
                });
            }).GeneratePdf();
        }
    }
}
