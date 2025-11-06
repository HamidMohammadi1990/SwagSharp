using System.Text.Json;
using SwagSharp.Api.DTOs;
using SwagSharp.Api.Extensions;
using SwagSharp.Api.Contracts.Services;

namespace SwagSharp.Api.Services;

public class CodeGenerationService(IModelGeneratorService modelGeneratorService, IServiceGeneratorService serviceGeneratorService) : ICodeGenerationService
{
    public async Task GenerateAsync(GenerateServiceRequest request)
    {
        var swaggerJson = await request.File.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(swaggerJson))
            return;

        var jsonDocument = JsonDocument.Parse(swaggerJson);
        var currentDirectory = Directory.GetCurrentDirectory();

        var modelsDirectory = Path.Combine(currentDirectory, "Models");
        await modelGeneratorService.GenerateAsync(request.ModelsNameSpace, modelsDirectory, jsonDocument);

        var servicesDirectory = Path.Combine(currentDirectory, "Services");
        await serviceGeneratorService.GenerateAsync(servicesDirectory, jsonDocument);
    }
}