using System;
using Xunit;

namespace BubbyPlanetShowroom.Tests
{
    public class CouponCustomerAccessTests
    {
        [Theory]
        [InlineData("9876543210", "9876543210")]
        [InlineData(" 98765 43210 ", "9876543210")]
        [InlineData("+919876543210", "9876543210")]
        [InlineData("98765", "98765")]
        [InlineData("", "")]
        public void NormalizePhone_KeepsLastTenDigits(string raw, string expected)
        {
            Assert.Equal(expected, CouponCustomerAccess.NormalizePhone(raw));
        }

        [Fact]
        public void CommonCoupon_NeedsMobileAndCountsUses()
        {
            CouponPhoneGateResult nobodyTyped = CouponCustomerAccess.CheckAssignedUse(
                "BUBBY10",
                "",
                hasAssignments: false,
                phoneIsAssigned: false,
                usesPerPhone: 1,
                redeemedCount: 0);
            Assert.False(nobodyTyped.Allowed);
            Assert.Equal("Enter mobile", nobodyTyped.Status);

            CouponPhoneGateResult firstUse = CouponCustomerAccess.CheckAssignedUse(
                "BUBBY10",
                "9000000001",
                hasAssignments: false,
                phoneIsAssigned: false,
                usesPerPhone: 1,
                redeemedCount: 0);
            Assert.True(firstUse.Allowed);

            CouponPhoneGateResult usedUp = CouponCustomerAccess.CheckAssignedUse(
                "BUBBY10",
                "9876543210",
                hasAssignments: false,
                phoneIsAssigned: false,
                usesPerPhone: 1,
                redeemedCount: 1);
            Assert.False(usedUp.Allowed);
            Assert.Equal("Already used", usedUp.Status);
        }

        [Fact]
        public void SpecialCoupon_WithoutMobiles_IsNotOpenToEveryone()
        {
            CouponPhoneGateResult waiting = CouponCustomerAccess.CheckAssignedUse(
                "VIP50",
                "9876543210",
                hasAssignments: false,
                phoneIsAssigned: false,
                usesPerPhone: 1,
                redeemedCount: 0,
                specialOnly: true);

            Assert.False(waiting.Allowed);
            Assert.Equal("Assign mobiles", waiting.Status);
        }

        [Fact]
        public void AssignedCoupon_RequiresTheMobile()
        {
            CouponPhoneGateResult missing = CouponCustomerAccess.CheckAssignedUse(
                "VIP50",
                "",
                hasAssignments: true,
                phoneIsAssigned: false,
                usesPerPhone: 1,
                redeemedCount: 0);
            Assert.False(missing.Allowed);
            Assert.Equal("Enter mobile", missing.Status);

            CouponPhoneGateResult other = CouponCustomerAccess.CheckAssignedUse(
                "VIP50",
                "9999999999",
                hasAssignments: true,
                phoneIsAssigned: false,
                usesPerPhone: 1,
                redeemedCount: 0);
            Assert.False(other.Allowed);
            Assert.Equal("Not for this number", other.Status);
        }

        [Fact]
        public void AssignedCoupon_BlocksWhenUsesAreFinished()
        {
            CouponPhoneGateResult once = CouponCustomerAccess.CheckAssignedUse(
                "VIP50",
                "9876543210",
                hasAssignments: true,
                phoneIsAssigned: true,
                usesPerPhone: 1,
                redeemedCount: 1);
            Assert.False(once.Allowed);
            Assert.Equal("Already used", once.Status);

            CouponPhoneGateResult third = CouponCustomerAccess.CheckAssignedUse(
                "VIP50",
                "9876543210",
                hasAssignments: true,
                phoneIsAssigned: true,
                usesPerPhone: 3,
                redeemedCount: 2);
            Assert.True(third.Allowed);

            CouponPhoneGateResult done = CouponCustomerAccess.CheckAssignedUse(
                "VIP50",
                "9876543210",
                hasAssignments: true,
                phoneIsAssigned: true,
                usesPerPhone: 3,
                redeemedCount: 3);
            Assert.False(done.Allowed);
        }

        [Fact]
        public void UsesLeft_TreatsZeroLimitAsOne()
        {
            Assert.Equal(1, CouponCustomerAccess.AllowedUses(0));
            Assert.Equal(0, CouponCustomerAccess.UsesLeft(1, 1));
            Assert.Equal(2, CouponCustomerAccess.UsesLeft(3, 1));
        }

        [Fact]
        public void BandsFor_SplitsTop50IntoGroupsOfTen()
        {
            CouponCustomerAccess.RankBand[] top10 = CouponCustomerAccess.BandsFor(10);
            Assert.Single(top10);
            Assert.Equal(1, top10[0].From);
            Assert.Equal(10, top10[0].To);

            CouponCustomerAccess.RankBand[] top50 = CouponCustomerAccess.BandsFor(50);
            Assert.Equal(5, top50.Length);
            Assert.Equal(1, top50[0].From);
            Assert.Equal(11, top50[1].From);
            Assert.Equal(41, top50[4].From);
            Assert.Equal(50, top50[4].To);
            Assert.True(CouponCustomerAccess.RankInBand(11, 11, 20));
            Assert.False(CouponCustomerAccess.RankInBand(10, 11, 20));

            CouponCustomerAccess.RankBand[] top15 = CouponCustomerAccess.BandsFor(15);
            Assert.Equal(2, top15.Length);
            Assert.Equal(11, top15[1].From);
            Assert.Equal(15, top15[1].To);

            CouponCustomerAccess.RankBand[] top100 = CouponCustomerAccess.BandsFor(100);
            Assert.Equal(10, top100.Length);
            Assert.Equal(91, top100[9].From);
            Assert.Equal(100, top100[9].To);

            CouponCustomerAccess.RankBand[] top1000 = CouponCustomerAccess.BandsFor(1000);
            Assert.Equal(100, top1000.Length);
            Assert.Equal(991, top1000[99].From);
            Assert.Equal(1000, top1000[99].To);
        }

        [Fact]
        public void NormalizeTopCount_KeepsTypedSizeInsideTheLimit()
        {
            Assert.Equal(10, CouponCustomerAccess.NormalizeTopCount(0));
            Assert.Equal(10, CouponCustomerAccess.NormalizeTopCount(10));
            Assert.Equal(100, CouponCustomerAccess.NormalizeTopCount(100));
            Assert.Equal(1000, CouponCustomerAccess.NormalizeTopCount(1000));
            Assert.Equal(CouponCustomerAccess.MaxTopList, CouponCustomerAccess.NormalizeTopCount(9000));
        }

        [Fact]
        public void ThisMonth_IsTheCalendarMonth()
        {
            (DateTime from, DateTime toExclusive) = CouponCustomerAccess.ThisMonth(new DateTime(2026, 10, 7));
            Assert.Equal(new DateTime(2026, 10, 1), from);
            Assert.Equal(new DateTime(2026, 11, 1), toExclusive);
        }
    }
}
