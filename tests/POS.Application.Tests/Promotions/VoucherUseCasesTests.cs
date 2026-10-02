using FluentAssertions;
using NSubstitute;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Promotions.Commands.CreateVoucher;
using POS.Application.UseCases.Promotions.Commands.DeleteVoucher;
using POS.Application.UseCases.Promotions.Commands.UpdateVoucher;
using POS.Application.UseCases.Promotions.Queries.GetVoucherById;
using POS.Application.UseCases.Promotions.Queries.GetVouchers;
using POS.Application.UseCases.Promotions.Queries.ValidateVoucher;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;
using POS.Domain.Promotions.Errors;
using Xunit;

namespace POS.Application.Tests.Promotions;

public class VoucherUseCasesTests
{
    private readonly IVoucherRepository voucherRepository = Substitute.For<IVoucherRepository>();
    private readonly IPromotionRepository promotionRepository = Substitute.For<IPromotionRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task CreateVoucher_UniqueCode_ShouldCreateAndSave()
    {
        // Arrange
        var promotion = new Promotion(Guid.Empty, "Promo", PromotionType.CartPercent, 10);
        promotionRepository.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);
        voucherRepository.IsCodeUniqueAsync("SUMMER2026", null, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateVoucherCommandHandler(voucherRepository, promotionRepository, unitOfWork);
        var command = new CreateVoucherCommand(promotion.Id, "SUMMER2026", 100, 1);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("SUMMER2026");
        result.Value.MaxUses.Should().Be(100);

        await voucherRepository.Received(1).AddAsync(Arg.Any<Voucher>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateVoucher_DuplicateCode_ShouldReturnDuplicateCodeError()
    {
        // Arrange
        var promotion = new Promotion(Guid.Empty, "Promo", PromotionType.CartPercent, 10);
        promotionRepository.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);
        voucherRepository.IsCodeUniqueAsync("EXISTINGCODE", null, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateVoucherCommandHandler(voucherRepository, promotionRepository, unitOfWork);
        var command = new CreateVoucherCommand(promotion.Id, "EXISTINGCODE", 100, 1);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PromotionErrors.VoucherDuplicateCode);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateVoucher_Valid_ShouldUpdate()
    {
        // Arrange
        var voucher = new Voucher(Guid.NewGuid(), "VOUCHER10", 50, 2);
        voucherRepository.GetByIdWithPromotionAsync(voucher.Id, Arg.Any<CancellationToken>()).Returns(voucher);

        var handler = new UpdateVoucherCommandHandler(voucherRepository, unitOfWork);
        var command = new UpdateVoucherCommand(voucher.Id, 100, 3, null, true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        voucher.MaxUses.Should().Be(100);
        voucher.PerCustomerLimit.Should().Be(3);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteVoucher_Valid_ShouldRemove()
    {
        // Arrange
        var voucher = new Voucher(Guid.NewGuid(), "VOUCHER10", 50, 2);
        voucherRepository.GetByIdAsync(voucher.Id, Arg.Any<CancellationToken>()).Returns(voucher);

        var handler = new DeleteVoucherCommandHandler(voucherRepository, unitOfWork);

        // Act
        var result = await handler.Handle(new DeleteVoucherCommand(voucher.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        voucherRepository.Received(1).Remove(voucher);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateVoucher_ValidCartPercent_ShouldCalculateDiscount()
    {
        // Arrange
        var promotion = new Promotion(Guid.Empty, "Giảm 10% đơn từ 100k", PromotionType.CartPercent, 10, minOrderAmount: 100000, maxDiscountAmount: 50000);
        var voucher = new Voucher(promotion.Id, "GIAM10", 100, 2);
        typeof(Voucher).GetProperty(nameof(Voucher.Promotion))!.SetValue(voucher, promotion);

        voucherRepository.GetByCodeWithPromotionAsync("GIAM10", Arg.Any<CancellationToken>()).Returns(voucher);

        var handler = new ValidateVoucherQueryHandler(voucherRepository);
        var query = new ValidateVoucherQuery("GIAM10", 200000);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsValid.Should().BeTrue();
        result.Value.DiscountAmount.Should().Be(20000); // 10% of 200,000
        result.Value.VoucherCode.Should().Be("GIAM10");
        result.Value.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateVoucher_MaxUsesReached_ShouldReturnInvalid()
    {
        // Arrange
        var promotion = new Promotion(Guid.Empty, "Promo", PromotionType.CartFixed, 20000);
        var voucher = new Voucher(promotion.Id, "MAXOUT", 10, 1);
        for (int i = 0; i < 10; i++) voucher.RecordUse();
        typeof(Voucher).GetProperty(nameof(Voucher.Promotion))!.SetValue(voucher, promotion);

        voucherRepository.GetByCodeWithPromotionAsync("MAXOUT", Arg.Any<CancellationToken>()).Returns(voucher);

        var handler = new ValidateVoucherQueryHandler(voucherRepository);
        var query = new ValidateVoucherQuery("MAXOUT", 100000);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsValid.Should().BeFalse();
        result.Value.ErrorMessage.Should().Be("Mã giảm giá đã hết lượt sử dụng.");
    }

    [Fact]
    public async Task ValidateVoucher_CustomerLimitReached_ShouldReturnInvalid()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var promotion = new Promotion(Guid.Empty, "Promo", PromotionType.CartFixed, 20000);
        var voucher = new Voucher(promotion.Id, "ONCEPERCUST", 100, 1);
        typeof(Voucher).GetProperty(nameof(Voucher.Promotion))!.SetValue(voucher, promotion);

        voucherRepository.GetByCodeWithPromotionAsync("ONCEPERCUST", Arg.Any<CancellationToken>()).Returns(voucher);
        voucherRepository.GetCustomerUsageCountAsync(voucher.Id, customerId, Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ValidateVoucherQueryHandler(voucherRepository);
        var query = new ValidateVoucherQuery("ONCEPERCUST", 100000, customerId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsValid.Should().BeFalse();
        result.Value.ErrorMessage.Should().Be("Khách hàng đã dùng hết số lượt cho phép với mã giảm giá này.");
    }

    [Fact]
    public async Task ValidateVoucher_MinOrderAmountNotMet_ShouldReturnInvalid()
    {
        // Arrange
        var promotion = new Promotion(Guid.Empty, "Promo", PromotionType.CartFixed, 20000, minOrderAmount: 150000);
        var voucher = new Voucher(promotion.Id, "MIN150", 100, 1);
        typeof(Voucher).GetProperty(nameof(Voucher.Promotion))!.SetValue(voucher, promotion);

        voucherRepository.GetByCodeWithPromotionAsync("MIN150", Arg.Any<CancellationToken>()).Returns(voucher);

        var handler = new ValidateVoucherQueryHandler(voucherRepository);
        var query = new ValidateVoucherQuery("MIN150", 100000); // Only 100k, min is 150k

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsValid.Should().BeFalse();
        result.Value.ErrorMessage.Should().Contain("Đơn hàng chưa đạt giá trị tối thiểu");
    }

    [Fact]
    public async Task ValidateVoucher_NotFound_ShouldReturnInvalid()
    {
        // Arrange
        voucherRepository.GetByCodeWithPromotionAsync("UNKNOWN", Arg.Any<CancellationToken>()).Returns((Voucher?)null);

        var handler = new ValidateVoucherQueryHandler(voucherRepository);
        var query = new ValidateVoucherQuery("UNKNOWN", 100000);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsValid.Should().BeFalse();
        result.Value.ErrorMessage.Should().Be("Mã giảm giá không tồn tại.");
    }
}
