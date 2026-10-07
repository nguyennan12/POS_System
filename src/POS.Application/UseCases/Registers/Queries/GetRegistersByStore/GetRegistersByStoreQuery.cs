using System;
using System.Collections.Generic;
using POS.Application.Abstractions.Messaging;
using POS.Contracts.V1.Registers;

namespace POS.Application.UseCases.Registers.Queries.GetRegistersByStore;

public record GetRegistersByStoreQuery(Guid StoreId) : IQuery<IReadOnlyList<RegisterResponse>>;
