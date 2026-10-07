using System.Threading;
using System.Threading.Tasks;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Registers;
using POS.Domain.Common;
using POS.Domain.Stores;

namespace POS.Application.UseCases.Registers.Commands.CreateRegister;

public class CreateRegisterCommandHandler(
    IPosRegisterRepository registerRepository,
    IStoreRepository storeRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateRegisterCommand, RegisterResponse>
{
    public async Task<Result<RegisterResponse>> Handle(
        CreateRegisterCommand command,
        CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
            return new Error(ErrorType.NotFound, "Store.NotFound", "Không tìm thấy cửa hàng.");

        var existing = await registerRepository.GetByCodeAsync(command.StoreId, command.Code.Trim().ToUpperInvariant(), cancellationToken);
        if (existing is not null)
            return new Error(ErrorType.AlreadyExists, "Register.CodeExists", "Mã quầy đã tồn tại trong cửa hàng này.");

        var register = new PosRegister(command.StoreId, command.Name.Trim(), command.Code.Trim().ToUpperInvariant(), true);
        await registerRepository.AddAsync(register, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RegisterResponse>.Success(new RegisterResponse(
            register.Id,
            register.StoreId,
            register.Name,
            register.Code,
            register.IsActive,
            register.CreatedAt
        ));
    }
}
