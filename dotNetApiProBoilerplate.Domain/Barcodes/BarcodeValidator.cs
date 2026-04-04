using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Barcodes
{
    public static class BarcodeValidator
    {
        public static bool IsValid(string code, BarcodeType type)
        {
            return type switch
            {
                BarcodeType.EAN13 => ValidateEan13(code),
                BarcodeType.EAN8 => ValidateEan8(code),
                BarcodeType.PLU => code.Length is >= 4 and <= 5,
                BarcodeType.UPC => code.Length == 12,
                BarcodeType.Internal => true,
                _ => false
            };
        }

        private static bool ValidateEan13(string ean)
        {
            if (ean.Length != 13) return false;

            var digits = ean.Select(c => c - '0').ToArray();
            int sum = 0;

            for (int i = 0; i < 12; i++)
                sum += (i % 2 == 0) ? digits[i] : digits[i] * 3;

            int checksum = (10 - (sum % 10)) % 10;
            return checksum == digits[12];
        }

        private static bool ValidateEan8(string ean)
        {
            if (ean.Length != 8) return false;

            var digits = ean.Select(c => c - '0').ToArray();
            int sum = 0;

            for (int i = 0; i < 7; i++)
                sum += (i % 2 == 0) ? digits[i] * 3 : digits[i];

            int checksum = (10 - (sum % 10)) % 10;
            return checksum == digits[7];
        }
    }
}
