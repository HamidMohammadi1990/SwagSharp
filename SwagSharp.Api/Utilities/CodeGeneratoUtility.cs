using System.Text;
using System.Text.Json;
using SwagSharp.Api.Extensions;

namespace SwagSharp.Api.Utilities;

public static class CodeGeneratoUtility
{
    public static string GetSafePropertyName(string propertyName, string className, HashSet<string> usedNames)
    {
        string pascalName = propertyName.ToPascalCase();
        if (pascalName == className || pascalName == className.SanitizeModelName())
            pascalName += "Value";

        if (usedNames.Contains(pascalName))
        {
            int counter = 1;
            string newName = pascalName;
            while (usedNames.Contains(newName))
            {
                newName = pascalName + counter;
                counter++;
            }
            pascalName = newName;
        }

        return pascalName;
    }

    public static string GenerateEnumClass(string modelName, JsonElement definition, string modelsNameSpace, string categoryPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"namespace {modelsNameSpace}.{categoryPath};");
        sb.AppendLine();

        string description = definition.GetDescription();
        if (!string.IsNullOrEmpty(description))
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// {description}");
            sb.AppendLine("/// </summary>");
        }

        sb.AppendLine($"public enum {modelName.SanitizeModelName()}");
        sb.AppendLine("{");

        if (definition.TryGetProperty("enum", out var enumValues))
        {
            int index = 0;
            foreach (var enumValue in enumValues.EnumerateArray())
            {
                string enumName = enumValue.ValueKind == JsonValueKind.String
                    ? enumValue.GetString()
                    : enumValue.GetRawText();

                if (string.IsNullOrEmpty(enumName)) continue;

                string pascalEnumName = (enumName?.Replace("-", "_").Replace(" ", "_").Replace(".", "_")).ToPascalCase();

                sb.AppendLine($"    {pascalEnumName} = {index},");

                index++;
            }
        }

        // Remove trailing comma from last entry
        string result = sb.ToString().TrimEnd();
        if (result.EndsWith(','))
        {
            result = result[..^1];
        }

        result += "}";
        return result;
    }

    public static string GenerateSimpleTypeClass(string modelName, JsonElement definition, string modelsNameSpace, string categoryPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();

        sb.AppendLine($"namespace {modelsNameSpace}.{categoryPath};");
        sb.AppendLine();

        string description = definition.GetDescription();
        if (!string.IsNullOrEmpty(description))
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// {description}");
            sb.AppendLine("/// </summary>");
        }

        string type = definition.GetCSharpType();

        sb.AppendLine($"public record {modelName.SanitizeModelName()}");
        sb.AppendLine("{");
        sb.AppendLine($"    public {type} Value {{ get; set; }}");
        sb.AppendLine("}");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public static string GenerateFallbackModel(string modelName, string modelsNameSpace, string categoryPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();

        sb.AppendLine($"namespace {modelsNameSpace}.{categoryPath};");
        sb.AppendLine("{");
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated fallback model");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public record {modelName.SanitizeModelName()}");
        sb.AppendLine("{");
        sb.AppendLine("   // This is a fallback model");
        sb.AppendLine("}");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public static string GenerateModelClass(string modelName, JsonElement properties, JsonElement definition, string modelsNameSpace, string categoryPath)
    {
        var sb = new StringBuilder();

        // Using statements
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {modelsNameSpace}.{categoryPath};");
        sb.AppendLine();

        // Class definition with description
        string description = definition.GetDescription();
        if (!string.IsNullOrEmpty(description))
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// {description.Replace("\n", "\n    /// ")}");
            sb.AppendLine("/// </summary>");
        }

        sb.AppendLine($"public record {modelName.SanitizeModelName()}");
        sb.AppendLine("{");

        // Property conflicts tracking
        var usedPropertyNames = new HashSet<string>();

        // Properties
        var enumerateProperties = properties.EnumerateObject().ToList();
        for (int i = 0; i < enumerateProperties.Count; i++)
        {
            string propName = enumerateProperties[i].Name;
            JsonElement propValue = enumerateProperties[i].Value;

            string propDescription = propValue.GetDescription();
            string propType = propValue.GetCSharpType();
            string jsonPropertyName = propName;
            bool isRequired = propValue.IsPropertyRequired(propName, definition);

            string safePropertyName = CodeGeneratoUtility.GetSafePropertyName(propName, modelName, usedPropertyNames);
            usedPropertyNames.Add(safePropertyName);

            if (!string.IsNullOrEmpty(propDescription))
            {
                sb.AppendLine("/// <summary>");
                sb.AppendLine($"/// {propDescription.Replace("\n", "\n        /// ")}");
                sb.AppendLine("/// </summary>");
            }

            sb.AppendLine($"    [JsonPropertyName(\"{jsonPropertyName}\")]");

            string defaultValue = GeneralUtility.GetDefaultValue(isRequired);
            string nullableIndicator = GeneralUtility.GetNullableIndicator(isRequired);

            sb.AppendLine($"    public {propType}{nullableIndicator} {safePropertyName} {{ get; set; }}{defaultValue}");

            bool isLast = i == enumerateProperties.Count - 1;
            if (!isLast)
                sb.AppendLine();
        }

        sb.Append('}');

        return sb.ToString();
    }
}