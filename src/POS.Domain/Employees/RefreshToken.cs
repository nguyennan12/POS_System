using POS.Domain.Common;

namespace POS.Domain.Employees;

public class RefreshToken : BaseEntity
{
  public RefreshToken() : base()
  {
  }

  public RefreshToken(Guid employeeId, Employee employee, string tokenHash, DateTime expiresAt, Guid? id = null) : base(id)
  {
    EmployeeId = employeeId;
    Employee = employee;
    TokenHash = tokenHash;
    ExpiresAt = expiresAt;
  }

  public Guid EmployeeId { get; private set; }
  public Employee Employee { get; private set; } = default!;

  public string TokenHash { get; private set; } = default!;
  public DateTime ExpiresAt { get; private set; }
  public DateTime? RevokedAt { get; private set; }
  public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

  public void Revoke(DateTime utcNow)
  {
    RevokedAt = utcNow;
  }
}
