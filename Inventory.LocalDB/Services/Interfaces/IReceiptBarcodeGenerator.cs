namespace Inventory.LocalDB.Services.Interfaces
{
    public interface IReceiptBarcodeGenerator
    {
        byte[] GenerateCode128Png(
            string value,
            int width = 320,
            int height = 80);
    }
}

