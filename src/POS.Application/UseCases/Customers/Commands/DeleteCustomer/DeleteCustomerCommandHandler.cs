using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Customers.Errors;

namespace POS.Application.UseCases.Customers.Commands.DeleteCustomer;

public class DeleteCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteCustomerCommand, bool>
{
    public async Task<Result<bool>> Handle(
        DeleteCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetEntityByIdAsync(request.Id, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        var hasOrders = await customerRepository.HasOrdersAsync(customer.Id, cancellationToken);
        if (hasOrders)
        {
            // Rule: Nếu đã có đơn hàng, chỉ deactivate chứ không xóa cứng
            customer.Deactivate();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }

        customerRepository.Remove(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
