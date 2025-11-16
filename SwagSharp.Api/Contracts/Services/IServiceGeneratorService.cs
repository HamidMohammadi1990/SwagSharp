using System.Text.Json;
using SwagSharp.Api.Models;

namespace SwagSharp.Api.Contracts.Services;

public interface IServiceGeneratorService
{
    Task GenerateAsync(string outputPath, JsonDocument jsonDocument, string modelsNameSpace, string interfacesNameSpace, string servicesNameSpace, List<ModelNameSpaceInfo> modelNameSpaces);
}