using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Barcodes
{
    public static class BarcodeDetector
    {
        public static BarcodeType Detect(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                throw new ArgumentException("Barcode empty");

            barcode = barcode.Trim();

            // caractères non numériques
            if (!barcode.All(char.IsDigit))
                return BarcodeType.Unknown;

            return barcode.Length switch
            {
                13 => BarcodeType.EAN13,
                12 => BarcodeType.UPC,
                8 => BarcodeType.EAN8,
                4 or 5 => BarcodeType.PLU,
                _ => BarcodeType.Unknown
            };
        }
    }
}
