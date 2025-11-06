using System.Text.Json;

namespace SwagSharp.Api.Contracts.Services;

public interface IServiceGeneratorService
{
    Task GenerateAsync(string outputPath, JsonDocument jsonDocument);
}