using POS.Domain.Common;

namespace POS.Application.Common;

public static class CommonErrors
{
    public static Error NotFound(string entity) =>
        new(
            ErrorType.NotFound,
            $"{entity}.NotFound",
            $"{entity} không tồn tại.");

    public static Error AlreadyExists(string entity) =>
        new(
            ErrorType.AlreadyExists,
            $"{entity}.AlreadyExists",
            $"{entity} đã tồn tại.");

    public static Error Invalid(string entity) =>
        new(
            ErrorType.Invalid,
            $"{entity}.Invalid",
            $"{entity} không hợp lệ.");
}