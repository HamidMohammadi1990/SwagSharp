using System.Text.Json;

namespace SwagSharp.Api.Contracts.Services;

public interface IServiceGeneratorService
{
    Task Generate(string outputPath, JsonDocument jsonDocument);
}