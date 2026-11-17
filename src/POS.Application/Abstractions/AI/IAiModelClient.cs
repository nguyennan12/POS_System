namespace POS.Application.Abstractions.AI;

public interface IAiModelClient
{
    Task<AiGenerationResponse> GenerateAsync(
        AiGenerationRequest request,
        CancellationToken cancellationToken);
}
