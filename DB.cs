
using Microsoft.VisualBasic.ApplicationServices;
using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace BubbyPlanetShowroom
{
    public static class DB
    {
        // Local:
        private const string ConnectionString = "server=localhost;user=root;password=;database=showroom_db;";

        //// Hosted server:
        //private const string ConnectionString = "server=srv529.hstgr.io;user=u608223022_bubbyplanet;password=m9YG=S13Tb>J;database=u608223022_showroom;";

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }


        public static DataTable GetData(string query)
        {
            using var con = GetConnection();
            using var da = new MySqlDataAdapter(query, con);
            DataTable dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public static void Execute(string query)
        {
            using var con = GetConnection();
            con.Open();
            using var cmd = new MySqlCommand(query, con);
            cmd.ExecuteNonQuery();

            //MainForm.OnStockUpdated();
        }

        public static void EnsureColumnExists(MySqlConnection conn, string tableName, string columnName, string definition)
        {
            using (MySqlCommand checkCmd = new MySqlCommand($"SHOW COLUMNS FROM `{tableName}` LIKE @col", conn))
            {
                checkCmd.Parameters.AddWithValue("@col", columnName);
                object result = checkCmd.ExecuteScalar();
                if (result != null)
                    return;
            }

            using (MySqlCommand alterCmd = new MySqlCommand($"ALTER TABLE `{tableName}` ADD COLUMN `{columnName}` {definition}", conn))
            {
                alterCmd.ExecuteNonQuery();
            }
        }

        public static void EnsureColumnDefinition(MySqlConnection conn, string tableName, string columnName, string definition)
        {
            // Safe, best-effort: keeps existing values but ensures future updates behave correctly.
            // If the column doesn't exist, caller should have created it via EnsureColumnExists.
            try
            {
                using (MySqlCommand alterCmd = new MySqlCommand(
                    $"ALTER TABLE `{tableName}` MODIFY COLUMN `{columnName}` {definition}", conn))
                {
                    alterCmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Ignore (older MySQL versions / permissions). App will still run with explicit updates.
            }
        }

        public static void EnsureAgeDiscountSchema(MySqlConnection conn)
        {
            // Track when stock first came in (used for age-based discounts)
            EnsureColumnExists(conn, "inv_stock", "date_added", "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP");
            EnsureColumnExists(conn, "inv_stock", "last_updated", "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP");

            // Critical: date_added must NOT auto-update on quantity changes.
            EnsureColumnDefinition(conn, "inv_stock", "date_added", "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP");

            // Keep last_updated as the changing timestamp (either app-driven or DB-driven).
            EnsureColumnDefinition(conn, "inv_stock", "last_updated", "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

            string createRules = @"
CREATE TABLE IF NOT EXISTS inv_age_discount_rules
(
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    rule_name VARCHAR(120) NOT NULL,
    main_category VARCHAR(80) NULL,
    item_code VARCHAR(80) NULL,
    min_age_months INT NOT NULL DEFAULT 0,
    discount_percent DECIMAL(6,2) NOT NULL DEFAULT 0,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    date_added DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);";

            using (var cmd = new MySqlCommand(createRules, conn))
            {
                cmd.ExecuteNonQuery();
            }

            // Additional filters for specific segments (e.g. Clothes->Party->Girls)
            EnsureColumnExists(conn, "inv_age_discount_rules", "sub_category", "VARCHAR(80) NULL");
            EnsureColumnExists(conn, "inv_age_discount_rules", "gender", "VARCHAR(40) NULL");
            EnsureColumnExists(conn, "inv_age_discount_rules", "staff_only", "TINYINT(1) NOT NULL DEFAULT 0");
        }

        public static void EnsureClosingBalanceSchema(MySqlConnection conn)
        {
            string createClosing = @"
CREATE TABLE IF NOT EXISTS daily_cash_closing
(
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    closing_date DATE NOT NULL,
    opening_balance DECIMAL(12,2) NOT NULL DEFAULT 0,
    cash_sales DECIMAL(12,2) NOT NULL DEFAULT 0,
    other_cash_in DECIMAL(12,2) NOT NULL DEFAULT 0,
    cash_in_reason VARCHAR(300) NULL,
    shop_expense DECIMAL(12,2) NOT NULL DEFAULT 0,
    staff_advance DECIMAL(12,2) NOT NULL DEFAULT 0,
    vendor_payment DECIMAL(12,2) NOT NULL DEFAULT 0,
    bank_deposit DECIMAL(12,2) NOT NULL DEFAULT 0,
    refund_amount DECIMAL(12,2) NOT NULL DEFAULT 0,
    other_cash_out DECIMAL(12,2) NOT NULL DEFAULT 0,
    cash_out_reason VARCHAR(300) NULL,
    counter_left_for_tomorrow DECIMAL(12,2) NOT NULL DEFAULT 0,
    cash_given_to_owner DECIMAL(12,2) NOT NULL DEFAULT 0,
    total_cash_in_hand DECIMAL(12,2) NOT NULL DEFAULT 0,
    total_cash_out DECIMAL(12,2) NOT NULL DEFAULT 0,
    available_before_closing DECIMAL(12,2) NOT NULL DEFAULT 0,
    expected_owner_cash DECIMAL(12,2) NOT NULL DEFAULT 0,
    difference_amount DECIMAL(12,2) NOT NULL DEFAULT 0,
    note VARCHAR(500) NULL,
    created_by_user VARCHAR(100) NOT NULL,
    created_by_role VARCHAR(50) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_daily_cash_closing_date (closing_date),
    INDEX ix_daily_cash_closing_created_at (created_at)
);";

            using (MySqlCommand cmd = new MySqlCommand(createClosing, conn))
            {
                cmd.ExecuteNonQuery();
            }

            EnsureColumnExists(conn, "daily_cash_closing", "cash_in_reason", "VARCHAR(300) NULL");
            EnsureColumnExists(conn, "daily_cash_closing", "cash_out_reason", "VARCHAR(300) NULL");

            string createMovements = @"
CREATE TABLE IF NOT EXISTS daily_cash_movements
(
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    closing_id INT NOT NULL,
    movement_date DATE NOT NULL,
    movement_type VARCHAR(10) NOT NULL,
    amount DECIMAL(12,2) NOT NULL DEFAULT 0,
    reason VARCHAR(300) NOT NULL,
    created_by_user VARCHAR(100) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX ix_daily_cash_movements_closing_id (closing_id),
    INDEX ix_daily_cash_movements_date_type (movement_date, movement_type),
    CONSTRAINT fk_daily_cash_movements_closing
        FOREIGN KEY (closing_id) REFERENCES daily_cash_closing(id)
        ON DELETE CASCADE
);";

            using (MySqlCommand movementCmd = new MySqlCommand(createMovements, conn))
            {
                movementCmd.ExecuteNonQuery();
            }
        }

    }
}
