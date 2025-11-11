using System.Text.Json;
using SwagSharp.Api.Utilities;
using SwagSharp.Api.Extensions;
using SwagSharp.Api.Contracts.Services;

namespace SwagSharp.Api.Services;

public class ModelGeneratorService : IModelGeneratorService
{
    public async Task GenerateAsync(string modelsNameSpace, string outputPath, JsonDocument jsonDocument)
    {
        EnsureDirectoryExists(outputPath);

        var definitions = jsonDocument.RootElement.GetProperty("definitions");

        Console.WriteLine($"Found {definitions.EnumerateObject().Count()} definitions");

        var categorizedModels = definitions.CategorizeByEntityName();
        foreach (var category in categorizedModels)
        {
            var pluralModelName = GeneralUtility.ToPlural(category.Key);
            string categoryPath = Path.Combine(outputPath, pluralModelName);
            Directory.CreateDirectory(categoryPath);

            Console.WriteLine($"\n📁 Generating {category.Key} models ({category.Value.Count} models)...");

            foreach (var model in category.Value)
            {
                try
                {
                    await GenerateModelFileAsync(model.Name, model.Definition, categoryPath, modelsNameSpace, pluralModelName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error generating {model.Name}: {ex.Message}");
                }
            }
        }
    }

    private static void EnsureDirectoryExists(string outputPath)
    {
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);
    }

    private static async Task GenerateModelFileAsync(string modelName, JsonElement definition, string categoryPath, string modelsNameSpace, string pluralModelName)
    {
        if (definition.IsEnumDefinition())
        {
            string enumCode = CodeGeneratoUtility.GenerateEnumClass(modelName, definition, modelsNameSpace, pluralModelName);
            await FileUtility.WriteFileAsync(categoryPath, $"{modelName}.cs", enumCode);
            Console.WriteLine($"  ✓ {modelName} (Enum)");
        }
        else if (definition.HasProperties())
        {
            var modelProperties = definition.GetProperty("properties");
            string modelCode = CodeGeneratoUtility.GenerateModelClass(modelName, modelProperties, definition, modelsNameSpace, pluralModelName);
            await FileUtility.WriteFileAsync(categoryPath, $"{modelName}.cs", modelCode);
            Console.WriteLine($"  ✓ {modelName}");
        }
        else if (definition.IsSimpleType())
        {
            string simpleTypeCode = CodeGeneratoUtility.GenerateSimpleTypeClass(modelName, definition, modelsNameSpace, pluralModelName);
            await FileUtility.WriteFileAsync(categoryPath, $"{modelName}.cs", simpleTypeCode);
            Console.WriteLine($"  ✓ {modelName} (Simple)");
        }
        else
        {
            string fallbackCode = CodeGeneratoUtility.GenerateFallbackModel(modelName, modelsNameSpace, pluralModelName);
            await FileUtility.WriteFileAsync(categoryPath, $"{modelName}.cs", fallbackCode);
            Console.WriteLine($"  ✓ {modelName} (Fallback)");
        }
    }
}