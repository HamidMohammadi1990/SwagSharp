using System.Text.Json;
using SwagSharp.Api.Contracts.Services;

namespace SwagSharp.Api.Services;

public class CodeGenerationService(IModelGeneratorService modelGeneratorService, IServiceGeneratorService serviceGeneratorService) : ICodeGenerationService
{
    public async Task GenerateAsync(string swaggerJson)
    {
        var jsonDocument = JsonDocument.Parse(swaggerJson);

        var currentDirectory = Directory.GetCurrentDirectory();

        var modelsDirectory = Path.Combine(currentDirectory, "Models");
        await modelGeneratorService.Generate(modelsDirectory, jsonDocument);

        var servicesDirectory = Path.Combine(currentDirectory, "Services");
        await serviceGeneratorService.Generate(servicesDirectory, jsonDocument);
    }
}