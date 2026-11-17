namespace POS.Infrastructure.AI.Gemini;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public bool Enabled { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://generativelanguage.googleapis.com/v1/";
    public string Model { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
    public int MaxOutputTokens { get; init; } = 1200;
    public double Temperature { get; init; } = 0.1;
    public int MaxHistoryMessages { get; init; } = 20;
}
