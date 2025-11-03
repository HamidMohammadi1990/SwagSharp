using System.Text.Json;

namespace SwagSharp.Application.Contracts.Services;

public interface IModelGeneratorService
{
    Task Generate(string outputPath, JsonDocument jsonDocument);
}