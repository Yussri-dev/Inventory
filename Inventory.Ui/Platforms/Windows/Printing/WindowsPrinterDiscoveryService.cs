#if WINDOWS

using Inventory.Ui.Services.Printing;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Inventory.Ui.Platforms.Windows.Printing
{
    public sealed class WindowsPrinterDiscoveryService
        : IPrinterDiscoveryService
    {
        private const uint PrinterEnumLocal =
            0x00000002;

        private const uint PrinterEnumConnections =
            0x00000004;

        private const uint PrinterInfoLevel =
            4;

        private const int ErrorInsufficientBuffer =
            122;

        public Task<IReadOnlyList<PrinterInfo>>
            GetInstalledPrintersAsync(
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var defaultPrinterName =
                GetDefaultPrinterName();

            var printers =
                GetInstalledPrinterNames()
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .OrderBy(
                        printerName => printerName,
                        StringComparer.OrdinalIgnoreCase)
                    .Select(printerName =>
                        new PrinterInfo
                        {
                            Name = printerName,

                            IsDefault =
                                string.Equals(
                                    printerName,
                                    defaultPrinterName,
                                    StringComparison.OrdinalIgnoreCase)
                        })
                    .ToList();

            return Task.FromResult<
                IReadOnlyList<PrinterInfo>>(
                    printers);
        }

        private static IReadOnlyList<string>
            GetInstalledPrinterNames()
        {
            var flags =
                PrinterEnumLocal |
                PrinterEnumConnections;

            var firstCallSucceeded =
                EnumPrinters(
                    flags,
                    null,
                    PrinterInfoLevel,
                    IntPtr.Zero,
                    0,
                    out var requiredBytes,
                    out _);

            var firstCallError =
                Marshal.GetLastWin32Error();

            if (requiredBytes == 0)
            {
                if (!firstCallSucceeded &&
                    firstCallError !=
                        ErrorInsufficientBuffer)
                {
                    throw new Win32Exception(
                        firstCallError,
                        "Windows could not enumerate installed printers.");
                }

                return Array.Empty<string>();
            }

            var buffer =
                Marshal.AllocHGlobal(
                    checked((int)requiredBytes));

            try
            {
                var succeeded =
                    EnumPrinters(
                        flags,
                        null,
                        PrinterInfoLevel,
                        buffer,
                        requiredBytes,
                        out _,
                        out var returnedPrinters);

                if (!succeeded)
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error(),
                        "Windows could not enumerate installed printers.");
                }

                var structureSize =
                    Marshal.SizeOf<PrinterInfo4>();

                var printerNames =
                    new List<string>(
                        checked((int)returnedPrinters));

                var currentPointer =
                    buffer;

                for (var index = 0;
                     index < returnedPrinters;
                     index++)
                {
                    var printer =
                        Marshal.PtrToStructure<PrinterInfo4>(
                            currentPointer);

                    if (!string.IsNullOrWhiteSpace(
                            printer.PrinterName))
                    {
                        printerNames.Add(
                            printer.PrinterName);
                    }

                    currentPointer =
                        IntPtr.Add(
                            currentPointer,
                            structureSize);
                }

                return printerNames;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static string?
            GetDefaultPrinterName()
        {
            uint requiredCharacters = 0;

            var firstCallSucceeded =
                GetDefaultPrinter(
                    null,
                    ref requiredCharacters);

            if (firstCallSucceeded &&
                requiredCharacters == 0)
            {
                return null;
            }

            if (requiredCharacters == 0)
            {
                return null;
            }

            var buffer =
                new StringBuilder(
                    checked((int)requiredCharacters));

            var succeeded =
                GetDefaultPrinter(
                    buffer,
                    ref requiredCharacters);

            if (!succeeded)
            {
                return null;
            }

            var printerName =
                buffer.ToString();

            return string.IsNullOrWhiteSpace(printerName)
                ? null
                : printerName.Trim();
        }

        [StructLayout(
            LayoutKind.Sequential,
            CharSet = CharSet.Unicode)]
        private struct PrinterInfo4
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? PrinterName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string? ServerName;

            public uint Attributes;
        }

        [DllImport(
            "winspool.drv",
            EntryPoint = "EnumPrintersW",
            SetLastError = true,
            CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumPrinters(
            uint flags,
            string? name,
            uint level,
            IntPtr printerInfo,
            uint bufferSize,
            out uint requiredBytes,
            out uint returnedPrinters);

        [DllImport(
            "winspool.drv",
            EntryPoint = "GetDefaultPrinterW",
            SetLastError = true,
            CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDefaultPrinter(
            StringBuilder? printerName,
            ref uint bufferSize);
    }
}

#endif