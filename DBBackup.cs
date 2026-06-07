using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    internal class DBBackup
    {
        public static void CreateBackup()
        {
            string connStr = "server=localhost;user=root;password=;database=showroom_db;";

            string backupFile = Path.Combine(
                @"G:\My Drive\ProductionBubbyPlanet\Inv_DB",
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