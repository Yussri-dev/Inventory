
using Inventory.LocalDB.Context;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Ui.Services.Labels
{
    public sealed record ProductLabelData(
    string Name,
    string Brand,
    string Barcode,
    decimal SalePrice);
}
