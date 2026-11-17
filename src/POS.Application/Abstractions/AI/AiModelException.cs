namespace POS.Application.Abstractions.AI;

public sealed class AiModelException(
    string code,
    string safeMessage,
    int statusCode) : Exception(safeMessage)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}
