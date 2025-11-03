using System.Text.Json;

namespace SwagSharp.Application.Contracts.Services;

public interface IServiceGeneratorService
{
    Task Generate(string outputPath, JsonDocument jsonDocument);
}