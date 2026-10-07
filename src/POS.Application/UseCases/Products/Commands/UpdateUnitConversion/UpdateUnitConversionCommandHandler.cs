using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Products.Commands.UpdateUnitConversion;

public class UpdateUnitConversionCommandHandler : ICommandHandler<UpdateUnitConversionCommand, UnitConversionDto>
{
    private readonly IUnitConversionRepository _unitConversionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUnitConversionCommandHandler(
        IUnitConversionRepository unitConversionRepository,
        IUnitOfWork unitOfWork)
    {
        _unitConversionRepository = unitConversionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UnitConversionDto>> Handle(UpdateUnitConversionCommand request, CancellationToken cancellationToken)
    {
        if (request.ConversionFactor <= 0)
        {
            return Result<UnitConversionDto>.Failure(new Error("ConversionFactor", "Conversion factor must be greater than 0."));
        }

        if (request.SellPrice < 0)
        {
            return Result<UnitConversionDto>.Failure(ProductErrors.InvalidPrice);
        }

        var unitConversion = await _unitConversionRepository.GetByIdAsync(request.Id, cancellationToken);
        if (unitConversion == null)
        {
            return Result<UnitConversionDto>.Failure(new Error("UnitConversion.NotFound", "Unit conversion not found."));
        }

        // Prevent duplicate (SkuId, UnitName) — exclude self from uniqueness check
        if (!await _unitConversionRepository.IsUnitNameUniqueAsync(unitConversion.SkuId, request.UnitName, request.Id, cancellationToken))
        {
            return Result<UnitConversionDto>.Failure(ProductErrors.UnitConversionDuplicate(request.UnitName));
        }

        unitConversion.Update(
            request.UnitName,
            request.ConversionFactor,
            request.SellPrice
        );

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
