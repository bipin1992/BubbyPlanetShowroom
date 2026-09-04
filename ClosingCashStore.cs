using System;
using System.Collections.Generic;
using System.Globalization;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    /// <summary>
    /// After today's closing is saved, a late cash bill/return must update
    /// leftover so next-day opening is not short.
    /// </summary>
    public static class ClosingCashStore
    {
        public static decimal GetCashSalesFromDb(DateTime date)
        {
            using MySqlConnection conn = DB.GetConnection();
            conn.Open();
            return GetCashSalesFromDb(conn, date);
        }

        public static decimal GetCashSalesFromDb(MySqlConnection conn, DateTime date)
        {
            decimal total = 0;
            foreach (ClosingCashBillLine line in GetCashBillLines(conn, date))
                total += line.Amount;
            return total;
        }

        public static List<ClosingCashBillLine> GetCashBillLines(DateTime date)
        {
            using MySqlConnection conn = DB.GetConnection();
            conn.Open();
            return GetCashBillLines(conn, date);
        }

        public static List<ClosingCashBillLine> GetCashBillLines(MySqlConnection conn, DateTime date)
        {
            var lines = new List<ClosingCashBillLine>();
            DB.EnsureColumnExists(conn, "inv_orders", "payment_method", "VARCHAR(40) NOT NULL DEFAULT 'Cash'");
            DB.EnsureReturnSettlementSchema(conn);

            DateTime fromDate = date.Date;
            DateTime toDate = fromDate.AddDays(1);

            const string extraQuery = @"
SELECT
    s.order_id,
    s.created_at,
    s.amount,
    o.date_added,
    o.payment_method AS order_payment_method,
    s.payment_method,
    s.settlement_type
FROM inv_return_settlements s
LEFT JOIN inv_orders o ON o.id = s.order_id
WHERE s.created_at >= @fromDate AND s.created_at < @toDate
ORDER BY s.created_at ASC, s.id ASC;";

            var extraLines = new List<ClosingCashBillLine>();
            var alreadyCountedByOrder = new Dictionary<int, decimal>();

            using (MySqlCommand cmd = new MySqlCommand(extraQuery, conn))
            {
                cmd.Parameters.AddWithValue("@fromDate", fromDate);
                cmd.Parameters.AddWithValue("@toDate", toDate);
                using MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    DateTime? originalBillDate = reader["date_added"] == DBNull.Value
                        ? (DateTime?)null
                        : Convert.ToDateTime(reader["date_added"]);

                    decimal amount = Convert.ToDecimal(reader["amount"], CultureInfo.InvariantCulture);
                    string settlementType = reader["settlement_type"]?.ToString() ?? "";
                    string settlementMethod = reader["payment_method"]?.ToString() ?? "";
                    string orderMethod = reader["order_payment_method"]?.ToString() ?? "";

                    decimal signed = ClosingCashCalculations.SignedCashAmount(
                        settlementType, settlementMethod, amount);
                    decimal cashEffect = ClosingCashCalculations.SettlementEffectOnDrawer(
                        settlementType, settlementMethod, amount, fromDate, originalBillDate, orderMethod);

                    int orderId = Convert.ToInt32(reader["order_id"]);
                    bool alreadyInTodayCash = ClosingCashCalculations.ExtraAlreadyInTodayCashBills(
                        fromDate, originalBillDate, orderMethod);

                    if (alreadyInTodayCash && signed != 0)
                    {
                        extraLines.Add(new ClosingCashBillLine
                        {
                            OrderId = orderId,
                            Kind = signed > 0 ? "Return extra" : "Return refund",
                            At = Convert.ToDateTime(reader["created_at"]),
                            Amount = signed
                        });

                        if (alreadyCountedByOrder.ContainsKey(orderId))
                            alreadyCountedByOrder[orderId] += signed;
                        else
                            alreadyCountedByOrder[orderId] = signed;
                    }
                    else if (cashEffect != 0)
                    {
                        bool online = !ClosingCashCalculations.IsCash(settlementMethod);
                        extraLines.Add(new ClosingCashBillLine
                        {
                            OrderId = orderId,
                            Kind = online
                                ? (cashEffect < 0
                                    ? ClosingCashCalculations.OnlineExtraKind
                                    : ClosingCashCalculations.OnlineRefundKind)
                                : (cashEffect > 0 ? "Return extra" : "Return refund"),
                            At = Convert.ToDateTime(reader["created_at"]),
                            Amount = cashEffect
                        });
                    }
                }
            }

            const string orderQuery = @"
SELECT id, date_added, grand_total
FROM inv_orders
WHERE DATE(date_added) = @sale_date
  AND LOWER(TRIM(IFNULL(payment_method, 'Cash'))) = 'cash'
ORDER BY id ASC;";

            using (MySqlCommand cmd = new MySqlCommand(orderQuery, conn))
            {
                cmd.Parameters.AddWithValue("@sale_date", fromDate);
                using MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int orderId = Convert.ToInt32(reader["id"]);
                    decimal alreadyCounted = alreadyCountedByOrder.TryGetValue(orderId, out decimal counted)
                        ? counted
                        : 0;

                    if (ClosingCashCalculations.HideSaleLineWhenExtraCollected(alreadyCounted))
                        continue;

                    lines.Add(new ClosingCashBillLine
                    {
                        OrderId = orderId,
                        Kind = "Sale",
                        At = Convert.ToDateTime(reader["date_added"]),
                        Amount = Convert.ToDecimal(reader["grand_total"], CultureInfo.InvariantCulture)
                    });
                }
            }

            lines.AddRange(extraLines);
            return lines;
        }

        public static void SyncTodaysSavedClosing()
        {
            try
            {
                DateTime today = DateTime.Today;
                using MySqlConnection conn = DB.GetConnection();
                conn.Open();
                DB.EnsureClosingBalanceSchema(conn);

                decimal liveCashSales = GetCashSalesFromDb(conn, today);

                const string selectQuery = @"
SELECT cash_sales
FROM daily_cash_closing
WHERE closing_date = @closing_date
LIMIT 1;";

                decimal savedCashSales;
                using (MySqlCommand selectCmd = new MySqlCommand(selectQuery, conn))
                {
                    selectCmd.Parameters.AddWithValue("@closing_date", today);
                    using MySqlDataReader reader = selectCmd.ExecuteReader();
                    if (!reader.Read())
                        return;

                    savedCashSales = Convert.ToDecimal(reader["cash_sales"], CultureInfo.InvariantCulture);
                }

                decimal delta = ClosingCashCalculations.LateCashDelta(savedCashSales, liveCashSales);
                if (delta == 0)
                    return;

                const string updateQuery = @"
UPDATE daily_cash_closing
SET
    cash_sales = @live_cash,
    total_cash_in_hand = total_cash_in_hand + @delta,
    available_before_closing = available_before_closing + @delta,
    expected_owner_cash = expected_owner_cash + @delta,
    difference_amount = difference_amount + @delta,
    is_shop_closed = CASE WHEN @live_cash <> 0 THEN 0 ELSE is_shop_closed END
WHERE closing_date = @closing_date;";

                using MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@live_cash", liveCashSales);
                updateCmd.Parameters.AddWithValue("@delta", delta);
                updateCmd.Parameters.AddWithValue("@closing_date", today);
                updateCmd.ExecuteNonQuery();
            }
            catch
            {
                // Billing/return must not fail if closing sync has a problem.
            }
        }
    }

    public sealed class ClosingCashBillLine
    {
        public int OrderId { get; set; }
        public string Kind { get; set; } = "";
        public DateTime At { get; set; }
        public decimal Amount { get; set; }
    }
}
