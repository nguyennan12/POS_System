namespace POS.Domain.Common
{
  public class Result<T> : Result
  {
    public T? Value { get; }

    protected Result(
        bool isSuccess,
        T? value,
        Error error)
        : base(isSuccess, error)
    {
      Value = value;
    }

    public static Result<T> Success(T value)
        => new(true, value, Error.None);

    public static new Result<T> Failure(Error error)
        => new(false, default, error);

    public static implicit operator Result<T>(T value)
        => Success(value);

    public static implicit operator Result<T>(Error error)
        => Failure(error);
  }
}
