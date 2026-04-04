using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Domain.Barcodes
{
    public enum BarcodeType
    {
        EAN13,
        EAN8,
        UPC,
        PLU,
        Unknown,
        Code39,
        Code128,
        QRCode,
        DataMatrix,
        Internal
    }
}
