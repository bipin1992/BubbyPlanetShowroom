using System;
using MySql.Data.MySqlClient;

namespace BubbyPlanetShowroom
{
    public readonly struct CouponPhoneUse
    {
        public bool HasAssignments { get; init; }
        public bool PhoneIsAssigned { get; init; }
        public int RedeemedCount { get; init; }
        public bool SpecialOnly { get; init; }
    }

    public sealed class CouponPhoneGateResult
    {
        public bool Allowed { get; init; }
        public string Status { get; init; } = "";
        public string Message { get; init; } = "";
    }

    /// <summary>
    /// Every coupon needs a 10-digit mobile so Uses / phone can be checked.
    /// All customers: any new or old number, until that number hits the limit.
    /// Special: only assigned mobiles. A full return gives the use back.
    /// </summary>
    public static class CouponCustomerAccess
    {
        public const string SqlOrderStillHasItems =
            "EXISTS (SELECT 1 FROM inv_order_details d WHERE d.order_id = o.id AND (IFNULL(d.qty,0) - IFNULL(d.return_qty,0)) > 0)";

        public static string NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return "";

            var digits = new System.Text.StringBuilder(phone.Length);
            foreach (char ch in phone)
            {
                if (char.IsDigit(ch))
                    digits.Append(ch);
            }

            string text = digits.ToString();
            if (text.Length > 10)
                text = text.Substring(text.Length - 10);
            return text;
        }

        public static bool IsMobile(string? phone)
        {
            return NormalizePhone(phone).Length == 10;
        }

        public static int AllowedUses(int usesPerPhone)
        {
            return usesPerPhone < 1 ? 1 : usesPerPhone;
        }

        public static int UsesLeft(int usesPerPhone, int redeemedCount)
        {
            int left = AllowedUses(usesPerPhone) - Math.Max(0, redeemedCount);
            return left < 0 ? 0 : left;
        }

        public sealed class RankBand
        {
            public int From { get; init; }
            public int To { get; init; }

            public override string ToString()
            {
                return "Ranks " + From + "–" + To;
            }
        }

        public const int MaxTopList = 5000;

        public static int NormalizeTopCount(int typed)
        {
            if (typed < 1)
                return 10;
            if (typed > MaxTopList)
                return MaxTopList;
            return typed;
        }

        public static RankBand[] BandsFor(int listSize)
        {
            int size = listSize < 1 ? 1 : listSize;
            if (size > MaxTopList)
                size = MaxTopList;
            var bands = new System.Collections.Generic.List<RankBand>();
            for (int start = 1; start <= size; start += 10)
            {
                int end = start + 9;
                if (end > size)
                    end = size;
                bands.Add(new RankBand { From = start, To = end });
            }

            return bands.ToArray();
        }

        public static bool RankInBand(int rank, int fromRank, int toRank)
        {
            return rank >= fromRank && rank <= toRank;
        }

        public static (DateTime From, DateTime ToExclusive) ThisMonth(DateTime today)
        {
            DateTime from = new DateTime(today.Year, today.Month, 1);
            return (from, from.AddMonths(1));
        }

        public static CouponPhoneGateResult CheckAssignedUse(
            string? code,
            string? phone,
            bool hasAssignments,
            bool phoneIsAssigned,
            int usesPerPhone,
            int redeemedCount,
            bool specialOnly = false)
        {
            string normalizedCode = CouponCalculations.NormalizeCode(code);
            string normalizedPhone = NormalizePhone(phone);
            if (specialOnly && !hasAssignments)
            {
                return new CouponPhoneGateResult
                {
                    Allowed = false,
                    Status = "Assign mobiles",
                    Message = $"Coupon {normalizedCode} is for special customers. Assign their mobiles on Top Customers."
                };
            }

            if (normalizedPhone.Length != 10)
            {
                string why = hasAssignments
                    ? $"Coupon {normalizedCode} is only for assigned numbers."
                    : $"Coupon {normalizedCode} needs the mobile so each number can be limited.";
                return new CouponPhoneGateResult
                {
                    Allowed = false,
                    Status = "Enter mobile",
                    Message = "Enter the customer's 10-digit mobile. " + why
                };
            }

            if (hasAssignments && !phoneIsAssigned)
            {
                return new CouponPhoneGateResult
                {
                    Allowed = false,
                    Status = "Not for this number",
                    Message = $"Coupon {normalizedCode} is not assigned to {normalizedPhone}."
                };
            }

            int allowed = AllowedUses(usesPerPhone);
            int used = Math.Max(0, redeemedCount);
            if (used >= allowed)
            {
                return new CouponPhoneGateResult
                {
                    Allowed = false,
                    Status = "Already used",
                    Message = $"Mobile {normalizedPhone} has already used coupon {normalizedCode} {used} of {allowed} time(s)."
                };
            }

            return new CouponPhoneGateResult { Allowed = true };
        }

        public static CouponPhoneUse ReadUse(MySqlConnection conn, string code, string? phone)
        {
            string normalizedCode = CouponCalculations.NormalizeCode(code);
            string normalizedPhone = NormalizePhone(phone);

            int assignedCount = Scalar(conn, @"
SELECT COUNT(*)
FROM inv_coupon_phones p
INNER JOIN inv_coupons c ON c.id = p.coupon_id
WHERE c.coupon_code = @code", normalizedCode, null);

            bool specialOnly = Scalar(conn, @"
SELECT IFNULL(MAX(for_special),0)
FROM inv_coupons
WHERE coupon_code = @code", normalizedCode, null) > 0;

            bool phoneIsAssigned = false;
            if (normalizedPhone.Length == 10)
            {
                phoneIsAssigned = Scalar(conn, @"
SELECT COUNT(*)
FROM inv_coupon_phones p
INNER JOIN inv_coupons c ON c.id = p.coupon_id
WHERE c.coupon_code = @code
  AND p.phone = @phone", normalizedCode, normalizedPhone) > 0;
            }

            int redeemed = 0;
            if (normalizedPhone.Length == 10)
            {
                redeemed = Scalar(conn, @"
SELECT COUNT(*)
FROM inv_orders o
INNER JOIN inv_customers cu ON cu.id = o.customer_id
WHERE UPPER(TRIM(IFNULL(o.coupon_code,''))) = @code
  AND TRIM(IFNULL(cu.phone,'')) = @phone
  AND " + SqlOrderStillHasItems, normalizedCode, normalizedPhone);
            }

            return new CouponPhoneUse
            {
                HasAssignments = assignedCount > 0,
                PhoneIsAssigned = phoneIsAssigned,
                RedeemedCount = redeemed,
                SpecialOnly = specialOnly || assignedCount > 0
            };
        }

        private static int Scalar(MySqlConnection conn, string sql, string code, string? phone)
        {
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@code", code);
            if (phone != null)
                cmd.Parameters.AddWithValue("@phone", phone);
            object value = cmd.ExecuteScalar();
            if (value == null || value is DBNull)
                return 0;
            return Convert.ToInt32(value);
        }
    }
}
