using System.Drawing.Printing;
using System.Linq;

namespace BubbyPlanetShowroom
{
    internal static class PrinterRouting
    {
        // Set exact Windows printer names here.
        // Keep blank to use system default printer.
        internal const string LabelPrinterName = "BubbyPlanet";
        internal const string ReceiptReturnPrinterName = "Receipt_BubbyPlanet";

        internal static void ApplyLabelPrinter(PrintDocument doc)
        {
            ApplyPrinter(doc, LabelPrinterName);
        }

        internal static void ApplyReceiptReturnPrinter(PrintDocument doc)
        {
            ApplyPrinter(doc, ReceiptReturnPrinterName);
        }

        private static void ApplyPrinter(PrintDocument doc, string configuredName)
        {
            if (doc == null)
                return;

            if (!string.IsNullOrWhiteSpace(configuredName))
            {
                bool exists = PrinterSettings.InstalledPrinters
                    .Cast<string>()
                    .Any(p => string.Equals(p, configuredName, System.StringComparison.OrdinalIgnoreCase));

                if (exists)
                    doc.PrinterSettings.PrinterName = configuredName;
            }

            if (doc.PrinterSettings.IsValid)
                return;

            string fallback = PrinterSettings.InstalledPrinters
                .Cast<string>()
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(fallback))
                doc.PrinterSettings.PrinterName = fallback;
        }
    }
}
