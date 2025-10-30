namespace SwagSharp.Application.Contracts.Services;

public interface ICodeGenerationService
{
    Task<string> GenerateAndReturnZipAsync(string swaggerJson);
}