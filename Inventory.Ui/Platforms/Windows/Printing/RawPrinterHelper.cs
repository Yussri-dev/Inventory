#if WINDOWS
using System;
using System.Runtime.InteropServices;

namespace Inventory.Ui.Platforms.Windows.Printing
{
    public static class RawPrinterHelper
    {
        [DllImport("winspool.Drv", SetLastError = true)]
        static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.Drv", SetLastError = true)]
        static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        static extern bool StartDocPrinter(IntPtr hPrinter, int level, IntPtr pDocInfo);

        [DllImport("winspool.Drv", SetLastError = true)]
        static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        static extern bool WritePrinter(IntPtr hPrinter, byte[] pBytes, int dwCount, out int dwWritten);

        public static void SendBytes(string printerName, byte[] bytes)
        {
            if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
                throw new Exception("Printer not found");

            try
            {
                StartDocPrinter(hPrinter, 1, IntPtr.Zero);
                StartPagePrinter(hPrinter);

                WritePrinter(hPrinter, bytes, bytes.Length, out _);

                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }
    }
}
#endif