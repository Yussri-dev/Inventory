namespace Inventory.LocalDB.Models
{
    public sealed class ReceiptPrinterOptions
    {
        public bool Enabled { get; set; }

        /// <summary>
        /// Nom exact de l'imprimante dans Windows.
        /// Exemple : EPSON TM-T20III Receipt
        /// </summary>
        public string PrinterName { get; set; } =
            string.Empty;

        /// <summary>
        /// 32 caractères pour une imprimante 58 mm.
        /// 42 ou 48 caractères pour une imprimante 80 mm.
        /// </summary>
        public int CharactersPerLine { get; set; } =
            48;

        /// <summary>
        /// Code page ESC/POS.
        /// CP858 gère correctement le symbole €.
        /// </summary>
        public int CodePage { get; set; } =
            858;

        public bool CutPaper { get; set; } =
            true;

        public int FeedLinesAfterReceipt { get; set; } =
            4;

        public string ReceiptTitle { get; set; } =
            "TICKET DE CAISSE";
    }
}
