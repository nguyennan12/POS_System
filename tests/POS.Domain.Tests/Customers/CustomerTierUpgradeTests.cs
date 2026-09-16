using FluentAssertions;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;
using Xunit;

namespace POS.Domain.Tests.Customers;

public class CustomerTierUpgradeTests
{
    private readonly MemberTier _normalTier = new(MemberTierName.Normal, 0m, 0.01m, 0m, "#808080");
    private readonly MemberTier _silverTier = new(MemberTierName.Silver, 5_000_000m, 0.015m, 0.02m, "#C0C0C0");
    private readonly MemberTier _goldTier = new(MemberTierName.Gold, 15_000_000m, 0.02m, 0.05m, "#FFD700");
    private readonly MemberTier _vipTier = new(MemberTierName.VIP, 30_000_000m, 0.03m, 0.10m, "#9400D3");

    private List<MemberTier> GetTiers() => [_normalTier, _silverTier, _goldTier, _vipTier];

    [Fact]
    public void New_customer_should_have_default_tier_and_zero_spending()
    {
        // Act
        var customer = new Customer(
            name: "Nguyen Van A",
            phone: "0901234567",
            memberTierId: _normalTier.Id,
            email: "a@example.com");

        // Assert
        customer.Name.Should().Be("Nguyen Van A");
        customer.Phone.Should().Be("0901234567");
        customer.MemberTierId.Should().Be(_normalTier.Id);
        customer.TotalSpending.Should().Be(0);
        customer.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RecordSpending_should_upgrade_tier_from_Normal_to_Silver_when_threshold_reached()
    {
        // Arrange
        var customer = new Customer(
            name: "Nguyen Van A",
            phone: "0901234567",
            memberTierId: _normalTier.Id);

        // Act
        var upgraded = customer.RecordSpending(5_000_000m, GetTiers());

        // Assert
        upgraded.Should().BeTrue();
        customer.TotalSpending.Should().Be(5_000_000m);
        customer.MemberTierId.Should().Be(_silverTier.Id);
    }

    [Fact]
    public void RecordSpending_should_upgrade_progressively_to_Gold_and_VIP()
    {
        // Arrange
        var customer = new Customer(
            name: "Nguyen Van A",
            phone: "0901234567",
            memberTierId: _normalTier.Id);

        // Act & Assert 1: Silver
        customer.RecordSpending(6_000_000m, GetTiers());
        customer.MemberTierId.Should().Be(_silverTier.Id);

        // Act & Assert 2: Gold
        var upgradedToGold = customer.RecordSpending(10_000_000m, GetTiers()); // Total = 16M
        upgradedToGold.Should().BeTrue();
        customer.TotalSpending.Should().Be(16_000_000m);
        customer.MemberTierId.Should().Be(_goldTier.Id);

        // Act & Assert 3: VIP
        var upgradedToVip = customer.RecordSpending(15_000_000m, GetTiers()); // Total = 31M
        upgradedToVip.Should().BeTrue();
        customer.TotalSpending.Should().Be(31_000_000m);
        customer.MemberTierId.Should().Be(_vipTier.Id);
    }

    [Fact]
    public void RecordSpending_under_threshold_should_not_upgrade_tier()
    {
        // Arrange
        var customer = new Customer(
            name: "Nguyen Van A",
            phone: "0901234567",
            memberTierId: _normalTier.Id);

        // Act
        var upgraded = customer.RecordSpending(3_000_000m, GetTiers());

        // Assert
        upgraded.Should().BeFalse();
        customer.TotalSpending.Should().Be(3_000_000m);
        customer.MemberTierId.Should().Be(_normalTier.Id);
    }

    [Fact]
    public void Update_should_correctly_modify_customer_details()
    {
        // Arrange
        var customer = new Customer(
            name: "Nguyen Van A",
            phone: "0901234567",
            memberTierId: _normalTier.Id);

        // Act
        customer.Update(
            name: "Nguyen Van B",
            phone: "0987654321",
            email: "b@example.com",
            dob: new DateOnly(1995, 5, 20),
            barcode: "CUST-999",
            isActive: false);

        // Assert
        customer.Name.Should().Be("Nguyen Van B");
        customer.Phone.Should().Be("0987654321");
        customer.Email.Should().Be("b@example.com");
        customer.Dob.Should().Be(new DateOnly(1995, 5, 20));
        customer.Barcode.Should().Be("CUST-999");
        customer.IsActive.Should().BeFalse();
    }
}
