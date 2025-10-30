namespace SwagSharp.Application.Contracts.Services;

public interface ICodeGenerationService
{
    Task<string> GenerateAsync(string swaggerJson);
}