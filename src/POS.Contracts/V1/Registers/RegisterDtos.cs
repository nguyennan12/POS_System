using System;

namespace POS.Contracts.V1.Registers;

public record RegisterResponse(
    Guid Id,
    Guid StoreId,
    string Name,
    string Code,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateRegisterRequest(
    string Name,
    string Code
);

public record UpdateRegisterRequest(
    string Name,
    string Code,
    bool IsActive
);
