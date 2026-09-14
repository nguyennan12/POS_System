using POS.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using POS.Domain.Common;

namespace POS.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_employee_store_access_employee_id_store_id"
        })
        {
            throw new DuplicateStoreAccessException(ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_employees_store_id_pin_lookup_hash"
        })
        {
            throw new EmployeeAssignmentConflictException(ex);
        }
    }

    public async Task<Result<T>> ExecuteSerializableAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var committed = false;
        try
        {
            var result = await operation(cancellationToken);
            if (result.IsSuccess)
            {
                await transaction.CommitAsync(cancellationToken);
                committed = true;
            }
            return result;
        }
        catch (Exception ex) when (IsSerializationFailure(ex))
        {
            // Never replay a decision made using stale authorization/source-store data.
            // Invalid state uses the existing Result/API error contract (HTTP 400).
            return new Error(ErrorType.Invalid, "Persistence.ConcurrentModification",
                "Dữ liệu đã thay đổi trong một thao tác đồng thời. Hãy tải lại và gửi lại yêu cầu.");
        }
        finally
        {
            // Disposal rolls back an uncommitted transaction. Do not retain entities from it.
            if (!committed) _context.ChangeTracker.Clear();
        }
    }

    private static bool IsSerializationFailure(Exception exception)
    {
        // EF/provider execution can wrap the server error more than once.
        // Match only PostgreSQL's serialization SQLSTATE, not arbitrary failures.
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure })
                return true;
        }
        return false;
    }
}
