using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    /// <summary>
    /// Loads and saves selling-price settings (rent, salary, slabs).
    /// </summary>
    public static class PricingSettingsStore
    {
        public static PricingSettings Load()
        {
            using MySqlConnection conn = DB.GetConnection();
            conn.Open();
            DB.EnsurePricingSchema(conn);
            return Load(conn);
        }

        public static PricingSettings Load(MySqlConnection conn)
        {
            PricingSettings settings = PricingSettings.CreateDefaults();

            using (MySqlCommand cmd = new MySqlCommand(
                @"SELECT monthly_rent, monthly_salary, expected_monthly_sales,
                         discount_percent, price_ending_digit,
                         total_transport_cost, total_parcel_quantity
                  FROM pricing_settings
                  WHERE id = 1",
                conn))
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    settings.MonthlyRent = reader.GetDecimal("monthly_rent");
                    settings.MonthlySalary = reader.GetDecimal("monthly_salary");
                    settings.ExpectedMonthlySales = reader.GetDecimal("expected_monthly_sales");
                    settings.DiscountPercent = 0m;
                    settings.PriceEndingDigit = reader.GetInt32("price_ending_digit");
                    settings.TotalTransportCost = 0m;
                    settings.TotalParcelQuantity = 1;
                }
            }

            List<ProfitMarginSlab> slabs = new List<ProfitMarginSlab>();
            using (MySqlCommand cmd = new MySqlCommand(
                @"SELECT min_purchase_cost, max_purchase_cost, margin_percent
                  FROM pricing_profit_slabs
                  ORDER BY sort_order, min_purchase_cost",
                conn))
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    slabs.Add(new ProfitMarginSlab
                    {
                        MinPurchaseCost = reader.GetDecimal("min_purchase_cost"),
                        MaxPurchaseCost = reader.IsDBNull(reader.GetOrdinal("max_purchase_cost"))
                            ? null
                            : reader.GetDecimal("max_purchase_cost"),
                        MarginPercent = reader.GetDecimal("margin_percent")
                    });
                }
            }

            if (slabs.Count > 0)
                settings.Slabs = slabs;

            return settings;
        }

        public static void Save(PricingSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (settings.Slabs == null || settings.Slabs.Count == 0)
                throw new InvalidOperationException("Save at least one profit-margin slab.");
            if (settings.ExpectedMonthlySales <= 0m)
                throw new InvalidOperationException("Expected monthly sales must be greater than 0.");
            if (settings.PriceEndingDigit < 0 || settings.PriceEndingDigit > 9)
                throw new InvalidOperationException("Price ending digit must be 0–9.");

            using MySqlConnection conn = DB.GetConnection();
            conn.Open();
            DB.EnsurePricingSchema(conn);

            using MySqlTransaction tx = conn.BeginTransaction();
            try
            {
                using (MySqlCommand cmd = new MySqlCommand(
                    @"INSERT INTO pricing_settings
                        (id, monthly_rent, monthly_salary, expected_monthly_sales, discount_percent, price_ending_digit,
                         total_transport_cost, total_parcel_quantity)
                      VALUES
                        (1, @rent, @salary, @sales, @discount, @ending, @transport, @parcel)
                      ON DUPLICATE KEY UPDATE
                        monthly_rent = @rent,
                        monthly_salary = @salary,
                        expected_monthly_sales = @sales,
                        discount_percent = @discount,
                        price_ending_digit = @ending,
                        total_transport_cost = @transport,
                        total_parcel_quantity = @parcel",
                    conn, tx))
                {
                    cmd.Parameters.AddWithValue("@rent", settings.MonthlyRent);
                    cmd.Parameters.AddWithValue("@salary", settings.MonthlySalary);
                    cmd.Parameters.AddWithValue("@sales", settings.ExpectedMonthlySales);
                    cmd.Parameters.AddWithValue("@discount", 0m);
                    cmd.Parameters.AddWithValue("@ending", settings.PriceEndingDigit);
                    cmd.Parameters.AddWithValue("@transport", 0m);
                    cmd.Parameters.AddWithValue("@parcel", 1);
                    cmd.ExecuteNonQuery();
                }

                using (MySqlCommand clear = new MySqlCommand("DELETE FROM pricing_profit_slabs", conn, tx))
                    clear.ExecuteNonQuery();

                int order = 0;
                foreach (ProfitMarginSlab slab in settings.Slabs)
                {
                    using MySqlCommand cmd = new MySqlCommand(
                        @"INSERT INTO pricing_profit_slabs
                            (min_purchase_cost, max_purchase_cost, margin_percent, sort_order)
                          VALUES
                            (@min, @max, @margin, @sort)",
                        conn, tx);
                    cmd.Parameters.AddWithValue("@min", slab.MinPurchaseCost);
                    cmd.Parameters.AddWithValue("@max", (object?)slab.MaxPurchaseCost ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@margin", slab.MarginPercent);
                    cmd.Parameters.AddWithValue("@sort", order++);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}
