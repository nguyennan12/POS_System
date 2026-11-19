namespace POS.Infrastructure.AI.Groq;

public sealed class GroqOptions
{
    public const string SectionName = "Groq";

    public bool Enabled { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://api.groq.com/openai/v1/";
    public string Model { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
    public int MaxCompletionTokens { get; init; } = 1200;
    public double Temperature { get; init; } = 0.1;
    public int MaxHistoryMessages { get; init; } = 20;
}
