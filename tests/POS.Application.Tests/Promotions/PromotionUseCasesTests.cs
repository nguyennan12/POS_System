using FluentAssertions;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Promotions.Commands.CreatePromotion;
using POS.Application.UseCases.Promotions.Commands.DeletePromotion;
using POS.Application.UseCases.Promotions.Commands.UpdatePromotion;
using POS.Application.UseCases.Promotions.Queries.GetPromotionById;
using POS.Application.UseCases.Promotions.Queries.GetPromotions;
using POS.Domain.Promotions;
using POS.Domain.Promotions.Enums;
using POS.Domain.Promotions.Errors;
using Xunit;

namespace POS.Application.Tests.Promotions;

public class PromotionUseCasesTests
{
    private readonly IPromotionRepository promotionRepository = Substitute.For<IPromotionRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();

    [Fact]
    public async Task CreatePromotion_Valid_ShouldCreateAndSave()
    {
        // Arrange
        currentUser.IsChainOwner.Returns(true);
        currentUser.EmployeeId.Returns(Guid.NewGuid());

        var handler = new CreatePromotionCommandHandler(promotionRepository, unitOfWork, currentUser);
        var catId = Guid.NewGuid();
        var command = new CreatePromotionCommand(
            StoreId: null,
            Name: "Giảm giá khai trương",
            Type: "CartPercent",
            Value: 10,
            MinOrderAmount: 100000,
            AppliesTo: "Category",
            TargetCategoryIds: [catId]);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Giảm giá khai trương");
        result.Value.Value.Should().Be(10);
        result.Value.TargetCategoryIds.Should().Contain(catId);

        await promotionRepository.Received(1).AddAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPromotionById_WhenFound_ShouldReturnDetail()
    {
        // Arrange
        var promotion = new Promotion(
            storeId: Guid.Empty,
            name: "Flash Sale",
            type: PromotionType.CartFixed,
            value: 50000,
            minOrderAmount: 200000);

        promotionRepository.GetByIdWithTargetsAsync(promotion.Id, Arg.Any<CancellationToken>())
            .Returns(promotion);

        var handler = new GetPromotionByIdQueryHandler(promotionRepository);
        var query = new GetPromotionByIdQuery(promotion.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(promotion.Id);
        result.Value.Name.Should().Be("Flash Sale");
        result.Value.Value.Should().Be(50000);
    }

    [Fact]
    public async Task GetPromotionById_WhenNotFound_ShouldReturnError()
    {
        // Arrange
        var promoId = Guid.NewGuid();
        promotionRepository.GetByIdWithTargetsAsync(promoId, Arg.Any<CancellationToken>())
            .Returns((Promotion?)null);

        var handler = new GetPromotionByIdQueryHandler(promotionRepository);
        var query = new GetPromotionByIdQuery(promoId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PromotionErrors.NotFound);
    }

    [Fact]
    public async Task UpdatePromotion_Valid_ShouldUpdate()
    {
        // Arrange
        var promotion = new Promotion(
            storeId: Guid.Empty,
            name: "Old Name",
            type: PromotionType.CartFixed,
            value: 20000);

        promotionRepository.GetByIdWithTargetsAsync(promotion.Id, Arg.Any<CancellationToken>())
            .Returns(promotion);

        var handler = new UpdatePromotionCommandHandler(promotionRepository, unitOfWork);
        var command = new UpdatePromotionCommand(
            Id: promotion.Id,
            Name: "New Name",
            Type: "CartFixed",
            Value: 30000,
            MinOrderAmount: 100000,
            Status: "Active");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Name");
        result.Value.Value.Should().Be(30000);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletePromotion_Valid_ShouldRemove()
    {
        // Arrange
        var promotion = new Promotion(
            storeId: Guid.Empty,
            name: "To Delete",
            type: PromotionType.CartFixed,
            value: 10000);

        promotionRepository.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>())
            .Returns(promotion);

        var handler = new DeletePromotionCommandHandler(promotionRepository, unitOfWork);

        // Act
        var result = await handler.Handle(new DeletePromotionCommand(promotion.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        promotionRepository.Received(1).Remove(promotion);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPromotions_Paged_ShouldReturnPagedList()
    {
        // Arrange
        var p1 = new Promotion(Guid.Empty, "Promo 1", PromotionType.CartFixed, 10000);
        var p2 = new Promotion(Guid.Empty, "Promo 2", PromotionType.CartPercent, 10);

        promotionRepository.GetPagedAsync(
            Arg.Any<Guid?>(), Arg.Any<PromotionStatus?>(), Arg.Any<PromotionType?>(), Arg.Any<DateTimeOffset?>(), 1, 20, Arg.Any<CancellationToken>())
            .Returns(([p1, p2], 2));

        var handler = new GetPromotionsQueryHandler(promotionRepository);
        var query = new GetPromotionsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
    }
}
