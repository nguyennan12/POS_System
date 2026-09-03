namespace POS.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; private set; }

    protected BaseEntity(Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
    }
}
