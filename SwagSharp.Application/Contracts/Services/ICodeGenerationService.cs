namespace SwagSharp.Application.Contracts.Services;

public interface ICodeGenerationService
{
    Task GenerateAsync(string swaggerJson);
}