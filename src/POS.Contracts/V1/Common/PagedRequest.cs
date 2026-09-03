namespace POS.Contracts.V1.Common;

public record PagedRequest(
    int PageNumber = 1,
    int PageSize = 20
);
