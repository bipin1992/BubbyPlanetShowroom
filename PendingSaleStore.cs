using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BubbyPlanetShowroom
{
    /// <summary>
    /// Durable checkpoint for Receipt Save+Print so a crash after Print click
    /// can resume on next app start (DB commit and/or reprint).
    /// </summary>
    public enum PendingSaleStage
    {
        /// <summary>Print clicked; cart snapshotted; DB not committed yet.</summary>
        PrintClicked = 1,

        /// <summary>Order inserted and committed (or id written pre-commit); may still need print.</summary>
        DbCommitted = 2,

        /// <summary>Print was started after commit.</summary>
        PrintStarted = 3
    }

    public sealed class PendingSaleLine
    {
        public string ItemName { get; set; } = "";
        public decimal Discount { get; set; }
        public string Size { get; set; } = "";
        public decimal Price { get; set; }
        public int Qty { get; set; }
        public decimal Gross { get; set; }
        public decimal Taxable { get; set; }
        public decimal Gst { get; set; }
        public decimal Net { get; set; }
        public decimal GstPercent { get; set; }
        public string ItemCode { get; set; } = "";
        public int ItemId { get; set; }
        public string Color { get; set; } = "";
        public bool DiscountManual { get; set; }
        public decimal AutoDiscount { get; set; }
        public decimal ManualDiscount { get; set; }
        public decimal RewardDiscount { get; set; }
    }

    public sealed class PendingSaleCheckpoint
    {
        public PendingSaleStage Stage { get; set; } = PendingSaleStage.PrintClicked;
        public int OrderId { get; set; }
        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
        public string Mobile { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string Surname { get; set; } = "";
        public string PaymentMethod { get; set; } = "Cash";
        public bool RewardApplied { get; set; }
        public decimal RewardDiscountPercent { get; set; }
        public string MembershipName { get; set; } = "";
        public int CurrentCustomerId { get; set; }
        public bool CurrentCustomerIsStaff { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal TotalTaxable { get; set; }
        public decimal TotalGst { get; set; }
        public List<PendingSaleLine> Items { get; set; } = new();
    }

    public static class PendingSaleStore
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
                return Path.Combine(dir, "pending_sale.json");
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

        public static PendingSaleCheckpoint? Load()
        {
            try
            {
                if (!Exists())
                    return null;

                string json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                return JsonSerializer.Deserialize<PendingSaleCheckpoint>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public static void Save(PendingSaleCheckpoint checkpoint)
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
