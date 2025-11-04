namespace SwagSharp.Api.Contracts.Services;

public interface ICodeGenerationService
{
    Task GenerateAsync(string swaggerJson);
}