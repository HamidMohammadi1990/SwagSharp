using Scriban;
using System.Text;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using SwagSharp.Application.Contracts.Services;

namespace SwagSharp.Application.Services.CodeGen;

public class CodeGenerationService : ICodeGenerationService
{
    private readonly string _templatesPath;
    private readonly string _outputRoot;

    public CodeGenerationService(string templatesPath)
    {
        _templatesPath = templatesPath;
        _outputRoot = Path.Combine(Path.GetTempPath(), "SwaggerCodeGenOutput");
    }

    public async Task<string> GenerateAndReturnZipAsync(string swaggerJson)
    {
        if (Directory.Exists(_outputRoot))
            Directory.Delete(_outputRoot, true);
        Directory.CreateDirectory(_outputRoot);

        var swagger = JObject.Parse(swaggerJson);

        // 1️⃣ ساخت مدل‌ها از components
        var modelsPath = Path.Combine(_outputRoot, "Core", "DTOs");
        Directory.CreateDirectory(modelsPath);
        await GenerateDtosAndEnumsAsync(swagger, modelsPath);

        // 2️⃣ پردازش مسیرها و ساخت سرویس‌ها
        var paths = swagger["paths"] as JObject;
        if (paths != null)
        {
            foreach (var path in paths)
            {
                var operations = path.Value as JObject;
                if (operations == null) continue;

                foreach (var op in operations)
                {
                    var httpMethod = op.Key;
                    var operation = op.Value as JObject;
                    var tag = operation?["tags"]?.FirstOrDefault()?.ToString() ?? "Common";
                    var serviceName = $"{tag}Service";
                    var methodName = operation?["operationId"]?.ToString() ?? $"{httpMethod}_{path.Key}";
                    var parameters = GetParametersDetailed(operation, modelsPath);
                    var paramSignature = string.Join(", ", parameters.Select(p => $"{p.type} {p.name}"));
                    var responseType = GetResponseType(operation);

                    var operationContext = new
                    {
                        methodName = ToPascalCase(methodName),
                        parameters = paramSignature,
                        responseType = responseType
                    };

                    var context = new
                    {
                        namespace_ = "Application.Interfaces",
                        service_name = serviceName.Replace(" ", ""),
                        operations = new[] { operationContext }
                    };

                    // Interface
                    var interfaceTemplate = await File.ReadAllTextAsync(Path.Combine(_templatesPath, "service_interface.sbn"));
                    var interfaceResult = Template.Parse(interfaceTemplate).Render(context);
                    var interfaceDir = Path.Combine(_outputRoot, "Application", "Interfaces");
                    Directory.CreateDirectory(interfaceDir);
                    var interfacePath = Path.Combine(interfaceDir, $"I{serviceName}.cs");
                    await File.AppendAllTextAsync(interfacePath, interfaceResult + Environment.NewLine);

                    // Implementation
                    var serviceTemplate = await File.ReadAllTextAsync(Path.Combine(_templatesPath, "service_impl.sbn"));
                    var implContext = new
                    {
                        namespace_ = "Infrastructure.Services",
                        service_name = serviceName.Replace(" ", ""),
                        operations = new[] { operationContext }
                    };
                    var serviceResult = Template.Parse(serviceTemplate).Render(implContext);
                    var implDir = Path.Combine(_outputRoot, "Infrastructure", "Services");
                    Directory.CreateDirectory(implDir);
                    var implPath = Path.Combine(implDir, $"{serviceName}.cs");
                    await File.AppendAllTextAsync(implPath, serviceResult + Environment.NewLine);
                }
            }
        }

        // 3️⃣ ZIP خروجی
        var zipPath = Path.Combine(Path.GetTempPath(), $"swagger_codegen_{Guid.NewGuid():N}.zip");
        ZipFile.CreateFromDirectory(_outputRoot, zipPath);
        return zipPath;
    }

    // ----------------------------------------------------
    // 🧠 ساخت DTOها و Enumها از components
    // ----------------------------------------------------
    private async Task GenerateDtosAndEnumsAsync(JObject swagger, string outputDir)
    {
        var components = swagger["components"]?["schemas"] as JObject;
        if (components == null) return;

        foreach (var schema in components)
        {
            await CreateDtoFromSchemaAsync(schema.Key, schema.Value as JObject, outputDir);
        }
    }

    private async Task CreateDtoFromSchemaAsync(string name, JObject? obj, string outputDir)
    {
        if (obj == null) return;

        // Enum
        if (obj["enum"] is JArray enumValues)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace Core.DTOs;");
            sb.AppendLine();
            sb.AppendLine($"public enum {name}Enum");
            sb.AppendLine("{");
            foreach (var ev in enumValues)
                sb.AppendLine($"    {ToPascalCase(ev.ToString())},");
            sb.AppendLine("}");
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"{name}Enum.cs"), sb.ToString());
            return;
        }

        // Object DTO
        var properties = new List<string>();
        var props = obj["properties"] as JObject;
        if (props != null)
        {
            foreach (var prop in props)
            {
                var type = MapSwaggerType(prop.Value as JObject);
                properties.Add($"    public {type} {ToPascalCase(prop.Key)} {{ get; set; }}");
            }
        }

        var sbClass = new StringBuilder();
        sbClass.AppendLine("namespace Core.DTOs;");
        sbClass.AppendLine();
        sbClass.AppendLine($"public class {name}Dto");
        sbClass.AppendLine("{");
        sbClass.AppendLine(string.Join(Environment.NewLine, properties));
        sbClass.AppendLine("}");

        await File.WriteAllTextAsync(Path.Combine(outputDir, $"{name}Dto.cs"), sbClass.ToString());
    }

    // ----------------------------------------------------
    // 🎯 Response Type
    // ----------------------------------------------------
    private string GetResponseType(JObject operation)
    {
        var responses = operation["responses"] as JObject;
        if (responses == null) return "void";

        var successResponse = responses.Properties()
            .FirstOrDefault(p => p.Name.StartsWith("2"))?.Value as JObject;
        if (successResponse == null) return "void";

        var content = successResponse["content"]?["application/json"]?["schema"] as JObject;
        if (content == null) return "void";

        return GetTypeFromSchema(content);
    }

    private string GetTypeFromSchema(JObject schema)
    {
        var refPath = schema["$ref"]?.ToString();
        if (!string.IsNullOrEmpty(refPath))
            return $"{refPath.Split('/').Last()}Dto";

        var type = schema["type"]?.ToString();
        if (type == "array")
        {
            var items = schema["items"] as JObject;
            var itemType = GetTypeFromSchema(items!);
            return $"IEnumerable<{itemType}>";
        }

        return type switch
        {
            "integer" => "int",
            "number" => "double",
            "boolean" => "bool",
            "string" => "string",
            _ => "object"
        };
    }

    // ----------------------------------------------------
    // 🧩 Parameters + inline DTO ساخت
    // ----------------------------------------------------
    private List<dynamic> GetParametersDetailed(JObject operation, string dtoOutputDir)
    {
        var result = new List<dynamic>();

        var parameters = operation["parameters"] as JArray;
        if (parameters != null)
        {
            foreach (var p in parameters)
            {
                var name = p["name"]?.ToString();
                var schema = p["schema"] as JObject;
                var type = MapSwaggerType(schema);
                result.Add(new { name = ToCamelCase(name!), type });
            }
        }

        var requestBody = operation["requestBody"]?["content"]?["application/json"]?["schema"] as JObject;
        if (requestBody != null)
        {
            var refPath = requestBody["$ref"]?.ToString();
            string typeName;

            if (!string.IsNullOrEmpty(refPath))
            {
                typeName = $"{refPath.Split('/').Last()}Dto";
            }
            else
            {
                // inline object schema
                typeName = $"InlineRequest_{Guid.NewGuid():N}_Dto";
                CreateInlineDtoAsync(typeName, requestBody, dtoOutputDir).Wait();
            }

            result.Add(new { name = "body", type = typeName });
        }

        return result;
    }

    private async Task CreateInlineDtoAsync(string name, JObject schema, string outputDir)
    {
        if (schema["type"]?.ToString() != "object")
            return;

        var props = schema["properties"] as JObject;
        if (props == null) return;

        var properties = new List<string>();
        foreach (var prop in props)
        {
            var type = MapSwaggerType(prop.Value as JObject);
            properties.Add($"    public {type} {ToPascalCase(prop.Key)} {{ get; set; }}");
        }

        var sbClass = new StringBuilder();
        sbClass.AppendLine("namespace Core.DTOs;");
        sbClass.AppendLine();
        sbClass.AppendLine($"public class {name}");
        sbClass.AppendLine("{");
        sbClass.AppendLine(string.Join(Environment.NewLine, properties));
        sbClass.AppendLine("}");

        await File.WriteAllTextAsync(Path.Combine(outputDir, $"{name}.cs"), sbClass.ToString());
    }

    // ----------------------------------------------------
    // 🔤 Helpers
    // ----------------------------------------------------
    private string MapSwaggerType(JObject? schema)
    {
        if (schema == null) return "object";

        var type = schema["type"]?.ToString();
        var format = schema["format"]?.ToString();

        if (type == "integer" && format == "int64") return "long";
        if (type == "integer") return "int";
        if (type == "number") return "double";
        if (type == "boolean") return "bool";
        if (type == "string" && format == "date-time") return "DateTime";
        if (type == "string") return "string";

        var refPath = schema["$ref"]?.ToString();
        if (!string.IsNullOrEmpty(refPath))
            return $"{refPath.Split('/').Last()}Dto";

        return "object";
    }

    private string ToPascalCase(string input) =>
        string.IsNullOrEmpty(input) ? input : char.ToUpperInvariant(input[0]) + input[1..];

    private string ToCamelCase(string input) =>
        string.IsNullOrEmpty(input) ? input : char.ToLowerInvariant(input[0]) + input[1..];
}