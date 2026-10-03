using FluentAssertions;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;
using Xunit;

namespace POS.Domain.Tests.Customers;

public class PointRedemptionRateTests
{
    [Fact]
    public void Vnd_RedemptionRate_Should_CalculateRequiredPoints_WithCeilingRounding()
    {
        // Arrange: VND standard rate = 1000 (1 point = 1,000 VND)
        var tier = new MemberTier(
            name: MemberTierName.Normal,
            minSpending: 0m,
            pointRate: 0.01m,
            discountRate: 0m,
            pointRedemptionRate: 1000m);

        // Act & Assert
        // Exact division
        tier.CalculateRequiredPoints(10_000m).Should().Be(10m);
        tier.CalculateRequiredPoints(1_000m).Should().Be(1m);

        // Fractional division with Ceiling rounding
        // 1,500 / 1,000 = 1.5 -> 2 points
        tier.CalculateRequiredPoints(1_500m).Should().Be(2m);

        // 1,001 / 1,000 = 1.001 -> 2 points
        tier.CalculateRequiredPoints(1_001m).Should().Be(2m);

        // 999 / 1,000 = 0.999 -> 1 point
        tier.CalculateRequiredPoints(999m).Should().Be(1m);

        // 0 VND -> 0 points
        tier.CalculateRequiredPoints(0m).Should().Be(0m);
    }

    [Fact]
    public void Usd_RedemptionRate_Should_CalculateRequiredPoints_WithCeilingRounding()
    {
        // Arrange: USD standard rate = 0.01 (1 point = $0.01 or 100 points = $1.00)
        var tier = new MemberTier(
            name: MemberTierName.Silver,
            minSpending: 100m,
            pointRate: 0.01m,
            discountRate: 0.02m,
            pointRedemptionRate: 0.01m);

        // Act & Assert
        // Exact division: $10.00 / 0.01 = 1,000 points
        tier.CalculateRequiredPoints(10.00m).Should().Be(1000m);

        // Fractional division: $10.005 / 0.01 = 1000.5 -> 1001 points
        tier.CalculateRequiredPoints(10.005m).Should().Be(1001m);

        // $0.001 / 0.01 = 0.1 -> 1 point
        tier.CalculateRequiredPoints(0.001m).Should().Be(1m);

        // $0.00 -> 0 points
        tier.CalculateRequiredPoints(0m).Should().Be(0m);
    }

    [Fact]
    public void Vnd_RedemptionRate_Should_ConvertPointsToCurrency()
    {
        // Arrange
        var tier = new MemberTier(
            name: MemberTierName.Gold,
            minSpending: 5_000_000m,
            pointRate: 0.02m,
            discountRate: 0.05m,
            pointRedemptionRate: 1000m);

        // Act & Assert
        tier.ConvertPointsToCurrency(10m).Should().Be(10_000m);
        tier.ConvertPointsToCurrency(25.5m).Should().Be(25_500m);
        tier.ConvertPointsToCurrency(0m).Should().Be(0m);
    }

    [Fact]
    public void Usd_RedemptionRate_Should_ConvertPointsToCurrency()
    {
        // Arrange
        var tier = new MemberTier(
            name: MemberTierName.VIP,
            minSpending: 1000m,
            pointRate: 0.03m,
            discountRate: 0.10m,
            pointRedemptionRate: 0.01m);

        // Act & Assert
        tier.ConvertPointsToCurrency(100m).Should().Be(1.00m);
        tier.ConvertPointsToCurrency(1250m).Should().Be(12.50m);
        tier.ConvertPointsToCurrency(0m).Should().Be(0m);
    }

    [Fact]
    public void Default_PointRedemptionRate_ShouldBe_1000()
    {
        // Arrange & Act
        var tier = new MemberTier(MemberTierName.Normal, 0m, 0.01m, 0m);

        // Assert
        tier.PointRedemptionRate.Should().Be(1000m);
    }
}
