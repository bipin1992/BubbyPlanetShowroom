using System;
using System.IO;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    internal class DBBackup
    {
        private static readonly string BackupFolder =
            @"G:\My Drive\ProductionBubbyPlanet\Inv_DB";

        public static void CreateBackup()
        {
            string connStr = "server=localhost;user=root;password=;database=showroom_db;";

            Directory.CreateDirectory(BackupFolder);

            string backupFile = Path.Combine(
                BackupFolder,
                "showroom_db" + DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss") + ".sql");

            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                conn.Open();

                using (MySqlCommand cmd = new MySqlCommand())
                using (MySqlBackup mb = new MySqlBackup(cmd))
                {
                    cmd.Connection = conn;
                    mb.ExportToFile(backupFile);
                }
            }
        }
    }
}
