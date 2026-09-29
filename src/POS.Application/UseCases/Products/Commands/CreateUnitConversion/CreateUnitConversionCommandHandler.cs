using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Products;
using POS.Domain.Common;

namespace POS.Application.UseCases.Products.Commands.CreateUnitConversion;

public class CreateUnitConversionCommandHandler : ICommandHandler<CreateUnitConversionCommand, UnitConversionDto>
{
    private readonly ISkuRepository _skuRepository;
    private readonly IUnitConversionRepository _unitConversionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUnitConversionCommandHandler(
        ISkuRepository skuRepository,
        IUnitConversionRepository unitConversionRepository,
        IUnitOfWork unitOfWork)
    {
        _skuRepository = skuRepository;
        _unitConversionRepository = unitConversionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UnitConversionDto>> Handle(CreateUnitConversionCommand request, CancellationToken cancellationToken)
    {
        if (request.ConversionFactor <= 0)
        {
            return Result<UnitConversionDto>.Failure(new Error("ConversionFactor", "Conversion factor must be greater than 0."));
        }

        var sku = await _skuRepository.GetByIdWithProductAsync(request.SkuId, cancellationToken);
        if (sku == null)
        {
            return Result<UnitConversionDto>.Failure(new Error("Sku.NotFound", "SKU not found."));
        }

        var unitConversion = new UnitConversion(
            request.SkuId,
            request.UnitName,
            request.ConversionFactor,
            request.SellPrice
        );

        await _unitConversionRepository.AddAsync(unitConversion, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UnitConversionDto(
            unitConversion.Id,
            unitConversion.SkuId,
            unitConversion.UnitName,
            unitConversion.ConversionFactor,
            unitConversion.SellPrice
        );
    }
}
