using System;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    /// <summary>
    /// Age / category / staff auto-discount used by Receipt scan and Return exchange add.
    /// </summary>
    public static class AutoDiscountHelper
    {
        public static decimal ClampDiscount(decimal value)
        {
            if (value < 0) return 0;
            if (value > 100) return 100;
            return value;
        }

        public static decimal GetAutoDiscountPercent(MySqlConnection conn, string itemCode, bool isStaffCustomer)
        {
            try
            {
                using var cmd = new MySqlCommand(@"
                SELECT IFNULL(r.discount_percent,0)
                FROM inv_items_master i
                JOIN inv_stock s ON LOWER(TRIM(s.item_code)) = LOWER(TRIM(i.item_code))
                JOIN inv_age_discount_rules r ON r.is_active = 1
                WHERE LOWER(TRIM(i.item_code)) = LOWER(TRIM(@code))
                  AND r.min_age_months <= TIMESTAMPDIFF(MONTH, DATE(s.date_added), CURDATE())
                  AND (IFNULL(r.staff_only,0) = 0 OR @isStaff = 1)
                  AND (
                    (
                      r.item_code IS NOT NULL
                      AND TRIM(r.item_code) <> ''
                      AND LOWER(TRIM(r.item_code)) = LOWER(TRIM(i.item_code))
                    )
                    OR
                    (
                      (r.item_code IS NULL OR TRIM(r.item_code) = '')
                      AND (r.main_category IS NULL OR TRIM(r.main_category) = '' OR LOWER(TRIM(r.main_category)) = LOWER(TRIM(IFNULL(i.main_category,''))))
                      AND (r.sub_category IS NULL OR TRIM(r.sub_category) = '' OR LOWER(TRIM(r.sub_category)) = LOWER(TRIM(IFNULL(i.sub_category,''))))
                      AND (r.gender IS NULL OR TRIM(r.gender) = '' OR LOWER(TRIM(r.gender)) = LOWER(TRIM(IFNULL(i.gender,''))))
                    )
                  )
                ORDER BY
                  (
                    CASE WHEN IFNULL(r.staff_only,0) = 1 THEN 16 ELSE 0 END +
                    CASE WHEN r.item_code IS NOT NULL AND TRIM(r.item_code) <> '' THEN 8 ELSE 0 END +
                    CASE WHEN r.main_category IS NOT NULL AND TRIM(r.main_category) <> '' THEN 4 ELSE 0 END +
                    CASE WHEN r.sub_category IS NOT NULL AND TRIM(r.sub_category) <> '' THEN 2 ELSE 0 END +
                    CASE WHEN r.gender IS NOT NULL AND TRIM(r.gender) <> '' THEN 1 ELSE 0 END
                  ) DESC,
                  r.min_age_months DESC,
                  r.discount_percent DESC
                LIMIT 1;", conn);

                cmd.Parameters.AddWithValue("@code", (itemCode ?? "").Trim());
                cmd.Parameters.AddWithValue("@isStaff", isStaffCustomer ? 1 : 0);
                object val = cmd.ExecuteScalar();
                if (val == null)
                    return 0;

                decimal d = Convert.ToDecimal(val);
                return ClampDiscount(d);
            }
            catch
            {
                return 0;
            }
        }

        public static bool IsStaffMobile(MySqlConnection conn, string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return false;

            try
            {
                using var cmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM inv_users WHERE status=1 AND role='Staff' AND phone=@phone",
                    conn);
                cmd.Parameters.AddWithValue("@phone", mobile.Trim());
                object result = cmd.ExecuteScalar();
                return result != null && Convert.ToInt32(result) > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
