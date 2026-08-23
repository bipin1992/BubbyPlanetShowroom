using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BubbyPlanetShowroom
{
    public enum PendingReturnStage
    {
        /// <summary>Process clicked; UI snapshotted; DB not committed.</summary>
        ProcessClicked = 1,

        /// <summary>Return/exchange committed in DB; print may be pending.</summary>
        DbCommitted = 2,

        /// <summary>Print started after commit.</summary>
        PrintStarted = 3
    }

    public sealed class PendingReturnLine
    {
        public int DetailId { get; set; }
        public string ItemName { get; set; } = "";
        public string ItemCode { get; set; } = "";
        public int ReturnQty { get; set; }
        /// <summary>return_qty expected on detail after successful process (for resume verify).</summary>
        public int ExpectedReturnQtyAfter { get; set; }
    }

    public sealed class PendingExchangeLine
    {
        public int ItemId { get; set; }
        public string ItemCode { get; set; } = "";
        public string ItemName { get; set; } = "";
        public int Qty { get; set; }
        public decimal Price { get; set; }
        public decimal GstPercent { get; set; }
        public decimal Gross { get; set; }
        public decimal Taxable { get; set; }
        public decimal GstAmt { get; set; }
        public decimal Net { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal AutoDiscount { get; set; }
        public decimal ManualDiscount { get; set; }
        public bool DiscountManual { get; set; }
    }

    public sealed class PendingReturnPrintLine
    {
        public string ItemName { get; set; } = "";
        public string ItemCode { get; set; } = "";
        public string Size { get; set; } = "";
        public int Qty { get; set; }
        public decimal Price { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal Gross { get; set; }
        public decimal Taxable { get; set; }
        public decimal Gst { get; set; }
        public decimal Net { get; set; }
        /// <summary>Back-compat with older pending files.</summary>
        public decimal Amount
        {
            get => Net;
            set => Net = value;
        }
    }

    public sealed class PendingReturnCheckpoint
    {
        public PendingReturnStage Stage { get; set; } = PendingReturnStage.ProcessClicked;
        public int OrderId { get; set; }
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public bool IsExchange { get; set; }
        public decimal TotalRefund { get; set; }
        public decimal BalanceDue { get; set; }
        public decimal ReturnValue { get; set; }
        public decimal ExchangeValue { get; set; }
        /// <summary>Cash or Online — only when collect/refund amount &gt; 0.</summary>
        public string PaymentMethod { get; set; } = "Cash";
        /// <summary>collect | refund | empty</summary>
        public string SettlementType { get; set; } = "";
        public decimal SettlementAmount { get; set; }
        public List<PendingReturnLine> ReturnLines { get; set; } = new();
        public List<PendingExchangeLine> ExchangeLines { get; set; } = new();
        public List<PendingReturnPrintLine> PrintReturnLines { get; set; } = new();
        public List<PendingReturnPrintLine> PrintExchangeLines { get; set; } = new();
    }

    public static class PendingReturnStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static string FilePath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BubbyPlanetShowroom");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "pending_return.json");
            }
        }

        public static bool Exists()
        {
            try
            {
                return File.Exists(FilePath) && new FileInfo(FilePath).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public static PendingReturnCheckpoint? Load()
        {
            try
            {
                if (!Exists())
                    return null;

                string json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                return JsonSerializer.Deserialize<PendingReturnCheckpoint>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public static void Save(PendingReturnCheckpoint checkpoint)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));

            string json = JsonSerializer.Serialize(checkpoint, JsonOptions);
            string path = FilePath;
            string temp = path + ".tmp";

            File.WriteAllText(temp, json);
            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);

                string tmp = FilePath + ".tmp";
                if (File.Exists(tmp))
                    File.Delete(tmp);
            }
            catch
            {
            }
        }
    }
}
