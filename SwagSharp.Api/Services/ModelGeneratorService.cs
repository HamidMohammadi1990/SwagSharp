using System.Text;
using System.Text.Json;
using SwagSharp.Api.Contracts.Services;

namespace SwagSharp.Api.Services;

public class ModelGeneratorService : IModelGeneratorService
{
    public Task Generate(string outputPath, JsonDocument jsonDocument)
    {
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        var definitions = jsonDocument.RootElement.GetProperty("definitions");

        Console.WriteLine($"Found {definitions.EnumerateObject().Count()} definitions");

        var categorizedModels = CategorizeByEntityName(definitions);
        foreach (var category in categorizedModels)
        {
            string categoryPath = Path.Combine(outputPath, category.Key);
            Directory.CreateDirectory(categoryPath);

            Console.WriteLine($"\n📁 Generating {category.Key} models ({category.Value.Count} models)...");

            foreach (var model in category.Value)
            {
                try
                {
                    GenerateModelFile(model.Name, model.Definition, categoryPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error generating {model.Name}: {ex.Message}");
                }
            }
        }

        return Task.CompletedTask;
    }

    private Dictionary<string, List<ModelInfo>> CategorizeByEntityName(JsonElement definitions)
    {
        var categories = new Dictionary<string, List<ModelInfo>>();

        foreach (var definition in definitions.EnumerateObject())
        {
            string modelName = definition.Name;
            string category = ExtractEntityCategory(modelName);

            if (!categories.ContainsKey(category))
                categories[category] = [];

            categories[category].Add(new ModelInfo
            {
                Name = modelName,
                Definition = definition.Value
            });
        }

        return categories;
    }

    private string ExtractEntityCategory(string modelName)
    {
        // حذف پسوندهای رایج
        string cleanedName = modelName
            .Replace("Dto", "")
            .Replace("DTO", "")
            .Replace("Request", "")
            .Replace("Response", "")
            .Replace("Proxy", "")
            .Replace("Create", "")
            .Replace("Update", "")
            .Replace("Delete", "")
            .Replace("Filter", "")
            .Replace("Paged", "");

        // استخراج entity name اصلی
        string entityName = ExtractMainEntityName(cleanedName);

        return entityName;
    }

    private string ExtractMainEntityName(string modelName)
    {
        // لیست entityهای اصلی
        var mainEntities = new List<string>();

        // شکستن نام به کلمات
        var words = SplitCamelCase(modelName);

        if (words.Count == 0)
            return "Common";

        // پیدا کردن entity اصلی (اولین کلمه معنادار)
        foreach (var word in words)
        {
            if (word.Length > 2 && !IsCommonWord(word))
            {
                return word;
            }
        }

        // اگر entity اصلی پیدا نشد، از اولین کلمه استفاده کن
        return words.FirstOrDefault() ?? "Common";
    }

    private bool IsCommonWord(string word)
    {
        var commonWords = new HashSet<string>
            {
                "and", "or", "the", "of", "in", "to", "for", "by", "with",
                "on", "at", "from", "as", "is", "are", "was", "were", "be",
                "has", "have", "had", "do", "does", "did", "get", "set",
                "add", "remove", "update", "delete", "create", "new", "old",
                "current", "previous", "next", "first", "last", "multiple",
                "single", "all", "any", "each", "every", "some", "no", "not",
                "type", "status", "category", "filter", "search", "find",
                "list", "page", "paged", "count", "total", "sum", "average",
                "min", "max", "value", "values", "data", "info", "detail",
                "details", "item", "items", "element", "elements", "object",
                "objects", "entity", "entities", "model", "models", "class",
                "record", "struct", "enum", "interface", "base", "abstract",
                "virtual", "override", "static", "public", "private", "protected",
                "internal", "sealed", "partial", "async", "await", "task"
            };

        return commonWords.Contains(word.ToLower());
    }

    private List<string> SplitCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return new List<string>();

        var words = new List<string>();
        var currentWord = new StringBuilder();

        foreach (char c in input)
        {
            if (char.IsUpper(c) && currentWord.Length > 0)
            {
                words.Add(currentWord.ToString());
                currentWord.Clear();
            }
            currentWord.Append(c);
        }

        if (currentWord.Length > 0)
            words.Add(currentWord.ToString());

        return words;
    }

    private void GenerateModelFile(string modelName, JsonElement definition, string categoryPath)
    {
        if (IsEnumDefinition(definition))
        {
            string enumCode = GenerateEnumClass(modelName, definition);
            WriteFile(categoryPath, $"{modelName}.cs", enumCode);
            Console.WriteLine($"  ✓ {modelName} (Enum)");
        }
        else if (HasProperties(definition))
        {
            var modelProperties = definition.GetProperty("properties");
            string modelCode = GenerateModelClass(modelName, modelProperties, definition);
            WriteFile(categoryPath, $"{modelName}.cs", modelCode);
            Console.WriteLine($"  ✓ {modelName}");
        }
        else if (IsSimpleType(definition))
        {
            string simpleTypeCode = GenerateSimpleTypeClass(modelName, definition);
            WriteFile(categoryPath, $"{modelName}.cs", simpleTypeCode);
            Console.WriteLine($"  ✓ {modelName} (Simple)");
        }
        else
        {
            string fallbackCode = GenerateFallbackModel(modelName);
            WriteFile(categoryPath, $"{modelName}.cs", fallbackCode);
            Console.WriteLine($"  ✓ {modelName} (Fallback)");
        }
    }

    private void WriteFile(string directory, string fileName, string content)
    {
        string filePath = Path.Combine(directory, fileName);
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    private bool HasProperties(JsonElement definition)
    {
        return definition.TryGetProperty("properties", out _);
    }

    private bool IsEnumDefinition(JsonElement definition)
    {
        return definition.TryGetProperty("enum", out _);
    }

    private bool IsSimpleType(JsonElement definition)
    {
        return definition.TryGetProperty("type", out _) &&
               !definition.TryGetProperty("properties", out _) &&
               !definition.TryGetProperty("enum", out _);
    }

    private string GenerateModelClass(string modelName, JsonElement properties, JsonElement definition)
    {
        var sb = new StringBuilder();

        // Using statements
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine("namespace GeneratedCode.Models;");
        sb.AppendLine();

        // Class definition with description
        string description = GetDescription(definition);
        if (!string.IsNullOrEmpty(description))
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// {description.Replace("\n", "\n    /// ")}");
            sb.AppendLine("/// </summary>");
        }

        sb.AppendLine($"public record {SanitizeModelName(modelName)}");
        sb.AppendLine("{");

        // Property conflicts tracking
        var usedPropertyNames = new HashSet<string>();

        // Properties
        var enumerateProperties = properties.EnumerateObject().ToList();
        for (int i = 0; i < enumerateProperties.Count; i++)
        {
            string propName = enumerateProperties[i].Name;
            JsonElement propValue = enumerateProperties[i].Value;

            string propDescription = GetDescription(propValue);
            string propType = GetCSharpType(propValue);
            string jsonPropertyName = propName;
            bool isRequired = IsPropertyRequired(propValue, propName, definition);

            string safePropertyName = GetSafePropertyName(propName, modelName, usedPropertyNames);
            usedPropertyNames.Add(safePropertyName);

            if (!string.IsNullOrEmpty(propDescription))
            {
                sb.AppendLine("/// <summary>");
                sb.AppendLine($"/// {propDescription.Replace("\n", "\n        /// ")}");
                sb.AppendLine("/// </summary>");
            }

            sb.AppendLine($"    [JsonPropertyName(\"{jsonPropertyName}\")]");

            string defaultValue = GetDefaultValue(propType, propValue, isRequired);
            string nullableIndicator = GetNullableIndicator(propType, isRequired);

            sb.AppendLine($"    public {propType}{nullableIndicator} {safePropertyName} {{ get; set; }}{defaultValue}");

            bool isLast = i == enumerateProperties.Count - 1;
            if (!isLast)
                sb.AppendLine();
        }

        sb.Append('}');

        return sb.ToString();
    }

    private bool IsPropertyRequired(JsonElement property, string propertyName, JsonElement parentDefinition)
    {
        // روش 1: بررسی آرایه required در parent object
        if (parentDefinition.TryGetProperty("required", out var requiredArray))
        {
            foreach (var requiredProp in requiredArray.EnumerateArray())
            {
                if (requiredProp.GetString() == propertyName)
                    return true;
            }
        }

        // روش 2: بررسی مستقیم required property
        if (property.TryGetProperty("required", out var requiredElement) &&
            requiredElement.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        // روش 3: بررسی nullable بودن
        if (property.TryGetProperty("nullable", out var nullableElement) &&
            nullableElement.ValueKind == JsonValueKind.True)
        {
            return false;
        }

        // روش 4: برای value types، اگر nullable نباشند required هستند
        if (property.TryGetProperty("type", out var typeElement))
        {
            string type = typeElement.GetString();

            // انواع value type که به صورت پیش‌فرض required هستند
            var nonNullableValueTypes = new HashSet<string> { "integer", "number", "boolean" };

            if (nonNullableValueTypes.Contains(type))
            {
                // مگر اینکه explicitly nullable باشند
                if (property.TryGetProperty("x-nullable", out var xNullable) &&
                    xNullable.ValueKind == JsonValueKind.True)
                    return false;

                return true;
            }
        }

        // روش 5: بررسی constraints
        if (HasValidationConstraints(property))
        {
            return true;
        }

        return false;
    }

    private bool HasValidationConstraints(JsonElement property)
    {
        // اگر حداقل یکی از constraintها وجود داشته باشد، پراپرتی required است
        return property.TryGetProperty("minLength", out _) ||
               property.TryGetProperty("maxLength", out _) ||
               property.TryGetProperty("minimum", out _) ||
               property.TryGetProperty("maximum", out _) ||
               property.TryGetProperty("pattern", out _) ||
               property.TryGetProperty("enum", out _);
    }

    private string GetNullableIndicator(string type, bool isRequired)
    {
        return isRequired ? "" : "?";
    }

    private bool IsValueType(string type)
    {
        var valueTypes = new HashSet<string>
        {
            "int", "long", "decimal", "float", "double", "bool", "DateTime",
            "short", "byte", "char", "Guid"
        };

        return valueTypes.Contains(type) ||
               type.StartsWith("int?") || type.StartsWith("long?") ||
               type.StartsWith("decimal?") || type.StartsWith("bool?") ||
               type.StartsWith("DateTime?");
    }

    private string GetSafePropertyName(string propertyName, string className, HashSet<string> usedNames)
    {
        string pascalName = ToPascalCase(propertyName);

        // اگر نام پراپرتی با نام کلاس یکی شد
        if (pascalName == className || pascalName == SanitizeModelName(className))
            pascalName += "Value";

        // اگر نام تکراری در همین کلاس وجود داشت
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

    private string GenerateEnumClass(string modelName, JsonElement definition)
    {
        var sb = new StringBuilder();

        sb.AppendLine("namespace GeneratedCode.Models;");
        sb.AppendLine();

        string description = GetDescription(definition);
        if (!string.IsNullOrEmpty(description))
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// {description}");
            sb.AppendLine("/// </summary>");
        }

        sb.AppendLine($"public enum {SanitizeModelName(modelName)}");
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

                string pascalEnumName = ToPascalCase(enumName?.Replace("-", "_").Replace(" ", "_").Replace(".", "_"));

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

    private string GenerateSimpleTypeClass(string modelName, JsonElement definition)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();

        sb.AppendLine("namespace GeneratedCode.Models;");
        sb.AppendLine();

        string description = GetDescription(definition);
        if (!string.IsNullOrEmpty(description))
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// {description}");
            sb.AppendLine("/// </summary>");
        }

        string type = GetCSharpType(definition);

        sb.AppendLine($"public record {SanitizeModelName(modelName)}");
        sb.AppendLine("{");
        sb.AppendLine($"    public {type} Value {{ get; set; }}");
        sb.AppendLine("}");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateFallbackModel(string modelName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();

        sb.AppendLine("namespace GeneratedCode.Models;");
        sb.AppendLine("{");
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated fallback model");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public record {SanitizeModelName(modelName)}");
        sb.AppendLine("{");
        sb.AppendLine("   // This is a fallback model");
        sb.AppendLine("}");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GetDescription(JsonElement element)
    {
        if (element.TryGetProperty("description", out var descriptionElement))
        {
            return descriptionElement.GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    private string GetDefaultValue(string type, JsonElement property, bool isRequired)
    {
        return isRequired ? " = default;" : "";

        // اگر default value مشخص شده باشد
        if (property.TryGetProperty("default", out var defaultElement))
        {
            //if (defaultElement.ValueKind == JsonValueKind.String)
            //    return $" = \"{defaultElement.GetString()}\";";
            //else if (defaultElement.ValueKind == JsonValueKind.True)
            //    return " = true;";
            //else if (defaultElement.ValueKind == JsonValueKind.False)
            //    return " = false;";
            //else if (defaultElement.ValueKind == JsonValueKind.Number)
            //    return $" = {defaultElement.GetRawText()};";
            //else if (defaultElement.ValueKind == JsonValueKind.Null)
            //    return " = null;";

            return " = default;";
        }

        // اگر required نباشد و value type باشد، default value قرار بده
        if (!isRequired && IsValueType(type))
        {
            //return type switch
            //{
            //    "int" or "int?" => " = 0;",
            //    "long" or "long?" => " = 0L;",
            //    "decimal" or "decimal?" => " = 0m;",
            //    "float" or "float?" => " = 0f;",
            //    "double" or "double?" => " = 0d;",
            //    "bool" or "bool?" => " = false;",
            //    "DateTime" or "DateTime?" => " = default;",
            //    _ => " = default;"
            //};

            return " = default;";
        }

        // برای reference types و nullable value types
        if (type.StartsWith("List<") || type.StartsWith("Dictionary<"))
            return " = [];";

        // برای stringهای required
        //if (type == "string" && isRequired)
        //    return " = default;";

        return "";
    }

    private string GetCSharpType(JsonElement property)
    {
        // Check for $ref first
        if (property.TryGetProperty("$ref", out var refElement))
        {
            string refPath = refElement.GetString();
            return SanitizeModelName(refPath?.Split('/').Last() ?? "object");
        }

        // Check for type
        if (property.TryGetProperty("type", out var typeElement))
        {
            string type = typeElement.GetString();
            string format = property.TryGetProperty("format", out var formatElement) ? formatElement.GetString() : null;

            return type switch
            {
                "string" => format switch
                {
                    "date-time" => "DateTime",
                    "date" => "DateTime",
                    "byte" => "byte[]",
                    "binary" => "byte[]",
                    _ => "string"
                },
                "integer" => format switch
                {
                    "int32" => "int",
                    "int64" => "long",
                    _ => "int"
                },
                "number" => format switch
                {
                    "float" => "float",
                    "double" => "double",
                    "decimal" => "decimal",
                    _ => "decimal"
                },
                "boolean" => "bool",
                "array" => GetArrayType(property),
                "object" => GetObjectType(property),
                _ => "object"
            };
        }

        return "object";
    }

    private string GetArrayType(JsonElement property)
    {
        if (property.TryGetProperty("items", out var items))
        {
            if (items.TryGetProperty("$ref", out var refElement))
            {
                string refType = refElement.GetString()?.Split('/').Last() ?? "object";
                return $"List<{SanitizeModelName(refType)}>";
            }

            if (items.TryGetProperty("type", out var typeElement))
            {
                string itemType = typeElement.GetString();
                return itemType switch
                {
                    "string" => "List<string>",
                    "integer" => items.TryGetProperty("format", out var itemFormat) && itemFormat.GetString() == "int64"
                                ? "List<long>" : "List<int>",
                    "number" => "List<decimal>",
                    "boolean" => "List<bool>",
                    _ => "List<object>"
                };
            }
        }

        return "List<object>";
    }

    private string GetObjectType(JsonElement property)
    {
        if (property.TryGetProperty("additionalProperties", out var additionalProps))
        {
            if (additionalProps.TryGetProperty("$ref", out var refElement))
            {
                string refType = refElement.GetString()?.Split('/').Last() ?? "object";
                return $"Dictionary<string, {SanitizeModelName(refType)}>";
            }

            if (additionalProps.TryGetProperty("type", out var typeElement))
            {
                string valueType = typeElement.GetString() switch
                {
                    "string" => "string",
                    "integer" => "int",
                    "number" => "decimal",
                    "boolean" => "bool",
                    _ => "object"
                };
                return $"Dictionary<string, {valueType}>";
            }

            return "Dictionary<string, object>";
        }

        return "object";
    }

    private string SanitizeModelName(string modelName)
    {
        if (string.IsNullOrEmpty(modelName))
            return "UnknownModel";

        // Remove special characters and ensure it starts with letter
        string cleaned = new(modelName.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

        if (string.IsNullOrEmpty(cleaned))
            return "UnknownModel";

        if (!char.IsLetter(cleaned[0]))
            cleaned = "Model" + cleaned;

        return cleaned;
    }

    private string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "UnknownProperty";

        // Remove invalid characters and convert to PascalCase
        string cleaned = new(input.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

        if (string.IsNullOrEmpty(cleaned))
            return "UnknownProperty";

        return char.ToUpper(cleaned[0]) + cleaned[1..];
    }
}

public class ModelInfo
{
    public string Name { get; set; }
    public JsonElement Definition { get; set; }
}