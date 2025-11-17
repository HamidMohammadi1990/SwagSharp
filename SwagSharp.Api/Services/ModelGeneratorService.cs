using System.Text.Json;
using SwagSharp.Api.Models;
using SwagSharp.Api.Utilities;
using SwagSharp.Api.Extensions;
using SwagSharp.Api.Contracts.Services;

namespace SwagSharp.Api.Services;

public class ModelGeneratorService : IModelGeneratorService
{
    public async Task<List<ModelNameSpaceInfo>> GenerateAsync(string modelsNameSpace, string outputPath, JsonDocument jsonDocument)
    {
        EnsureDirectoryExists(outputPath);

        var definitions = jsonDocument.RootElement.GetProperty("definitions");

        Console.WriteLine($"Found {definitions.EnumerateObject().Count()} definitions");
        var modelNameSpaces = new List<ModelNameSpaceInfo>();

        var categorizedModels = definitions.CategorizeByEntityName();
        foreach (var category in categorizedModels)
        {
            var pluralModelName = GeneralUtility.ToPlural(category.Key);

            foreach (var model in category.Value)
            {
                if (model.Definition.IsEnumDefinition())
                {
                    modelNameSpaces.Add(new ModelNameSpaceInfo(model.Name, $"using {modelsNameSpace}.{pluralModelName}.Enums;"));
                }
                else if (model.Definition.HasProperties())
                {
                    modelNameSpaces.Add(new ModelNameSpaceInfo(model.Name, $"using {modelsNameSpace}.{pluralModelName};"));
                }
                else if (model.Definition.IsSimpleType())
                {
                    modelNameSpaces.Add(new ModelNameSpaceInfo(model.Name, $"using {modelsNameSpace}.{pluralModelName};"));
                }
                else
                {
                    modelNameSpaces.Add(new ModelNameSpaceInfo(model.Name, $"using {modelsNameSpace}.{pluralModelName};"));
                }
            }
        }

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
                    var nameSpace = await GenerateModelFileAsync(model.Name, model.Definition, categoryPath, modelsNameSpace, pluralModelName, modelNameSpaces);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error generating {model.Name}: {ex.Message}");
                }
            }
        }

        return modelNameSpaces;
    }

    private static void EnsureDirectoryExists(string outputPath)
    {
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);
    }

    private static async Task<string> GenerateModelFileAsync(string modelName, JsonElement definition, string categoryPath, string modelsNameSpace, string pluralModelName, List<ModelNameSpaceInfo> modelNameSpaces)
    {
        if (definition.IsEnumDefinition())
        {
            string enumCode = CodeGeneratoUtility.GenerateEnum(modelName, definition, modelsNameSpace, pluralModelName);
            await FileUtility.WriteFileAsync(categoryPath, $"{modelName}.cs", enumCode);
            Console.WriteLine($"  ✓ {modelName} (Enum)");

            return $"using {modelsNameSpace}.{pluralModelName}.Enums;";
        }
        else if (definition.HasProperties())
        {
            var modelProperties = definition.GetProperty("properties");
            string modelCode = CodeGeneratoUtility.GenerateModelClass(modelName, modelProperties, definition, modelsNameSpace, pluralModelName, modelNameSpaces);
            await FileUtility.WriteFileAsync(categoryPath, $"{modelName}.cs", modelCode);
            Console.WriteLine($"  ✓ {modelName}");

            return $"using {modelsNameSpace}.{pluralModelName};";
        }
        else if (definition.IsSimpleType())
        {
            string simpleTypeCode = CodeGeneratoUtility.GenerateSimpleTypeClass(modelName, definition, modelsNameSpace, pluralModelName);
            await FileUtility.WriteFileAsync(categoryPath, $"{modelName}.cs", simpleTypeCode);
            Console.WriteLine($"  ✓ {modelName} (Simple)");

            return $"using {modelsNameSpace}.{pluralModelName};";
        }
        else
        {
            string fallbackCode = CodeGeneratoUtility.GenerateFallbackModel(modelName, modelsNameSpace, pluralModelName);
            await FileUtility.WriteFileAsync(categoryPath, $"{modelName}.cs", fallbackCode);
            Console.WriteLine($"  ✓ {modelName} (Fallback)");

            return $"using {modelsNameSpace}.{pluralModelName};";
        }
    }
}