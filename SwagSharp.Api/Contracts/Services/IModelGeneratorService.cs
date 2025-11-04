using System.Text.Json;

namespace SwagSharp.Api.Contracts.Services;

public interface IModelGeneratorService
{
    Task Generate(string outputPath, JsonDocument jsonDocument);
}