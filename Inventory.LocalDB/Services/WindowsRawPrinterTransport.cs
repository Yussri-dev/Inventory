

using Inventory.LocalDB.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Inventory.LocalDB.Services
{
    public sealed class WindowsRawPrinterTransport
    : IReceiptPrinterTransport
    {
        private readonly ILogger<WindowsRawPrinterTransport> _logger;

        public WindowsRawPrinterTransport(
            ILogger<WindowsRawPrinterTransport> logger)
        {
            _logger =
                logger;
        }

        public Task SendAsync(
            string printerName,
            byte[] data,
            CancellationToken cancellationToken = default)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException(
                    "Raw Windows receipt printing is only available on Windows.");
            }

            if (string.IsNullOrWhiteSpace(
                    printerName))
            {
                throw new ArgumentException(
                    "The printer name is required.",
                    nameof(printerName));
            }

            if (data == null ||
                data.Length == 0)
            {
                throw new ArgumentException(
                    "The print data is empty.",
                    nameof(data));
            }

            cancellationToken.ThrowIfCancellationRequested();

            return Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    SendToWindowsPrinter(
                        printerName,
                        data);
                },
                cancellationToken);
        }

        private void SendToWindowsPrinter(
            string printerName,
            byte[] data)
        {
            IntPtr printerHandle =
                IntPtr.Zero;

            IntPtr unmanagedData =
                IntPtr.Zero;

            var documentStarted =
                false;

            var pageStarted =
                false;

            try
            {
                if (!OpenPrinter(
                        printerName,
                        out printerHandle,
                        IntPtr.Zero))
                {
                    ThrowLastWin32Error(
                        $"Unable to open printer '{printerName}'.");
                }

                var documentInfo =
                    new DocInfo
                    {
                        DocumentName =
                            "Inventory POS Receipt",

                        OutputFile =
                            null,

                        DataType =
                            "RAW"
                    };

                var documentId =
                    StartDocPrinter(
                        printerHandle,
                        1,
                        documentInfo);

                if (documentId == 0)
                {
                    ThrowLastWin32Error(
                        "Unable to start the print document.");
                }

                documentStarted =
                    true;

                if (!StartPagePrinter(
                        printerHandle))
                {
                    ThrowLastWin32Error(
                        "Unable to start the printer page.");
                }

                pageStarted =
                    true;

                unmanagedData =
                    Marshal.AllocCoTaskMem(
                        data.Length);

                Marshal.Copy(
                    data,
                    0,
                    unmanagedData,
                    data.Length);

                if (!WritePrinter(
                        printerHandle,
                        unmanagedData,
                        data.Length,
                        out var bytesWritten))
                {
                    ThrowLastWin32Error(
                        "Unable to write receipt data to the printer.");
                }

                if (bytesWritten !=
                    data.Length)
                {
                    throw new IOException(
                        $"Only {bytesWritten} of {data.Length} bytes were " +
                        "sent to the printer.");
                }

                _logger.LogInformation(
                    "Sent {ByteCount} raw bytes to printer {PrinterName}.",
                    bytesWritten,
                    printerName);
            }
            finally
            {
                if (unmanagedData !=
                    IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(
                        unmanagedData);
                }

                if (pageStarted &&
                    printerHandle !=
                        IntPtr.Zero)
                {
                    EndPagePrinter(
                        printerHandle);
                }

                if (documentStarted &&
                    printerHandle !=
                        IntPtr.Zero)
                {
                    EndDocPrinter(
                        printerHandle);
                }

                if (printerHandle !=
                    IntPtr.Zero)
                {
                    ClosePrinter(
                        printerHandle);
                }
            }
        }

        private static void ThrowLastWin32Error(
            string message)
        {
            var errorCode =
                Marshal.GetLastWin32Error();

            throw new Win32Exception(
                errorCode,
                $"{message} Windows error: {errorCode}.");
        }

        [StructLayout(
            LayoutKind.Sequential,
            CharSet = CharSet.Unicode)]
        private sealed class DocInfo
        {
            [MarshalAs(
                UnmanagedType.LPWStr)]
            public string? DocumentName;

            [MarshalAs(
                UnmanagedType.LPWStr)]
            public string? OutputFile;

            [MarshalAs(
                UnmanagedType.LPWStr)]
            public string? DataType;
        }

        [DllImport(
            "winspool.drv",
            EntryPoint = "OpenPrinterW",
            SetLastError = true,
            CharSet = CharSet.Unicode)]
        [return: MarshalAs(
            UnmanagedType.Bool)]
        private static extern bool OpenPrinter(
            string printerName,
            out IntPtr printerHandle,
            IntPtr printerDefaults);

        [DllImport(
            "winspool.drv",
            EntryPoint = "ClosePrinter",
            SetLastError = true)]
        [return: MarshalAs(
            UnmanagedType.Bool)]
        private static extern bool ClosePrinter(
            IntPtr printerHandle);

        [DllImport(
            "winspool.drv",
            EntryPoint = "StartDocPrinterW",
            SetLastError = true,
            CharSet = CharSet.Unicode)]
        private static extern int StartDocPrinter(
            IntPtr printerHandle,
            int level,
            [In] DocInfo documentInfo);

        [DllImport(
            "winspool.drv",
            EntryPoint = "EndDocPrinter",
            SetLastError = true)]
        [return: MarshalAs(
            UnmanagedType.Bool)]
        private static extern bool EndDocPrinter(
            IntPtr printerHandle);

        [DllImport(
            "winspool.drv",
            EntryPoint = "StartPagePrinter",
            SetLastError = true)]
        [return: MarshalAs(
            UnmanagedType.Bool)]
        private static extern bool StartPagePrinter(
            IntPtr printerHandle);

        [DllImport(
            "winspool.drv",
            EntryPoint = "EndPagePrinter",
            SetLastError = true)]
        [return: MarshalAs(
            UnmanagedType.Bool)]
        private static extern bool EndPagePrinter(
            IntPtr printerHandle);

        [DllImport(
            "winspool.drv",
            EntryPoint = "WritePrinter",
            SetLastError = true)]
        [return: MarshalAs(
            UnmanagedType.Bool)]
        private static extern bool WritePrinter(
            IntPtr printerHandle,
            IntPtr bytes,
            int byteCount,
            out int bytesWritten);
    }
}
