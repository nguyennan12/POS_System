using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Shifts.Commands.OpenShift;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Employees.Enums;
using POS.Domain.Stores;
using Xunit;

namespace POS.Application.Tests.Shifts;

public class OpenShiftCommandHandlerTests
{
    private readonly IShiftRepository _shiftRepository = Substitute.For<IShiftRepository>();
    private readonly IPosRegisterRepository _posRegisterRepository = Substitute.For<IPosRegisterRepository>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private readonly OpenShiftCommandHandler _handler;

    public OpenShiftCommandHandlerTests()
    {
        _unitOfWork.ExecuteSerializableAsync(Arg.Any<Func<CancellationToken, Task<Result<ShiftDto>>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var func = callInfo.Arg<Func<CancellationToken, Task<Result<ShiftDto>>>>();
                return func(CancellationToken.None);
            });

        _handler = new OpenShiftCommandHandler(
            _shiftRepository,
            _posRegisterRepository,
            _employeeRepository,
            _unitOfWork,
            _currentUser);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenRegisterAndEmployeeAreValid()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var registerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        _currentUser.EmployeeId.Returns(employeeId);

        var employee = new Employee(
            "Thu Ngan A",
            "cashier_a",
            "hash",
            "pinhash",
            Guid.NewGuid(),
            isChainOwner: false,
            storeId: storeId,
            isActive: true,
            id: employeeId);

        var register = new PosRegister(storeId, "Quầy 01", "POS-01", true, registerId);

        _employeeRepository.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(employee);
        _posRegisterRepository.GetByIdAsync(registerId, Arg.Any<CancellationToken>()).Returns(register);
        _shiftRepository.GetOpenShiftByRegisterAsync(registerId, Arg.Any<CancellationToken>()).Returns((Shift?)null);

        var command = new OpenShiftCommand(storeId, registerId, 500_000, "Đầu ngày");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StoreId.Should().Be(storeId);
        result.Value.RegisterId.Should().Be(registerId);
        result.Value.RegisterName.Should().Be("Quầy 01");
        result.Value.OpeningCash.Should().Be(500_000);
        result.Value.Status.Should().Be(ShiftStatus.Open.ToString());

        await _shiftRepository.Received(1).AddAsync(Arg.Is<Shift>(s =>
            s.StoreId == storeId &&
            s.RegisterId == registerId &&
            s.EmployeeId == employeeId &&
            s.OpeningCash == 500_000), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenRegisterAlreadyHasOpenShift()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var registerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        _currentUser.EmployeeId.Returns(employeeId);

        var employee = new Employee(
            "Thu Ngan A",
            "cashier_a",
            "hash",
            "pinhash",
            Guid.NewGuid(),
            isChainOwner: false,
            storeId: storeId,
            isActive: true,
            id: employeeId);

        var register = new PosRegister(storeId, "Quầy 01", "POS-01", true, registerId);
        var existingShift = Shift.Open(storeId, registerId, employeeId, 200_000);

        _employeeRepository.GetByIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(employee);
        _posRegisterRepository.GetByIdAsync(registerId, Arg.Any<CancellationToken>()).Returns(register);
        _shiftRepository.GetOpenShiftByRegisterAsync(registerId, Arg.Any<CancellationToken>()).Returns(existingShift);

        var command = new OpenShiftCommand(storeId, registerId, 500_000, "Đầu ngày");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ShiftErrors.AlreadyOpen.Code);
    }
}
