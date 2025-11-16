using System.Text.Json;
using SwagSharp.Api.Models;

namespace SwagSharp.Api.Contracts.Services;

public interface IModelGeneratorService
{
    Task<List<ModelNameSpaceInfo>> GenerateAsync(string modelsNameSpace, string outputPath, JsonDocument jsonDocument);
}