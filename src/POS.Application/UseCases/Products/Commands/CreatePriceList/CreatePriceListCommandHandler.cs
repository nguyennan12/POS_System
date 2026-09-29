using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Products;
using POS.Domain.Common;

namespace POS.Application.UseCases.Products.Commands.CreatePriceList;

public class CreatePriceListCommandHandler : ICommandHandler<CreatePriceListCommand, PriceListDto>
{
    private readonly ISkuRepository _skuRepository;
    private readonly IPriceListRepository _priceListRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreatePriceListCommandHandler(
        ISkuRepository skuRepository,
        IPriceListRepository priceListRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _skuRepository = skuRepository;
        _priceListRepository = priceListRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PriceListDto>> Handle(CreatePriceListCommand request, CancellationToken cancellationToken)
    {
        if (request.Price < 0)
        {
            return Result<PriceListDto>.Failure(new Error("Price", "Price must be non-negative."));
        }

        if (request.ValidTo.HasValue && request.ValidFrom >= request.ValidTo.Value)
        {
            return Result<PriceListDto>.Failure(new Error("ValidFrom", "ValidFrom must be before ValidTo."));
        }

        var sku = await _skuRepository.GetByIdWithProductAsync(request.SkuId, cancellationToken);
        if (sku == null)
        {
            return Result<PriceListDto>.Failure(new Error("Sku.NotFound", "SKU not found."));
        }

        // Validate overlapping
        var overlapping = await _priceListRepository.IsOverlappingAsync(
            request.SkuId,
            request.CustomerGroup,
            request.ValidFrom,
            request.ValidTo,
            cancellationToken);

        if (overlapping)
        {
            return Result<PriceListDto>.Failure(new Error("PriceList.Overlapping", "The price list dates overlap with an existing price list for the same SKU and Customer Group."));
        }

        var storeId = _currentUser.StoreId ?? sku.StoreId;

        var priceList = new PriceList(
            storeId,
            request.SkuId,
            request.Price,
            request.ValidFrom,
            request.ValidTo,
            request.CustomerGroup,
            _currentUser.EmployeeId ?? Guid.Empty
        );

        await _priceListRepository.AddAsync(priceList, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PriceListDto(
            priceList.Id,
            priceList.StoreId,
            priceList.SkuId,
            priceList.Price,
            priceList.ValidFrom,
            priceList.ValidTo,
            priceList.CustomerGroup,
            priceList.CreatedBy,
            priceList.CreatedAt
        );
    }
}
