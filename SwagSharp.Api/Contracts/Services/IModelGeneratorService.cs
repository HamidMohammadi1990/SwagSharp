using System.Text.Json;

namespace SwagSharp.Api.Contracts.Services;

public interface IModelGeneratorService
{
    Task GenerateAsync(string modelsNameSpace, string outputPath, JsonDocument jsonDocument);
}