namespace POS.Application.Abstractions.Persistence;

public sealed class PersistenceConflictException : Exception
{
    public PersistenceConflictException(string constraintName, Exception innerException)
        : base($"Persistence conflict: {constraintName}", innerException)
    {
        ConstraintName = constraintName;
    }

    public string ConstraintName { get; }
}
