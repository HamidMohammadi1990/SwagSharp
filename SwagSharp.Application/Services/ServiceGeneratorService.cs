using System.Text;
using System.Text.Json;
using SwagSharp.Application.DTOs;
using System.Text.RegularExpressions;
using SwagSharp.Application.Contracts.Services;

namespace SwagSharp.Application.Services;

public class ServiceGeneratorService : IServiceGeneratorService
{
    public Task Generate(string outputPath, JsonDocument jsonDocument)
    {
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        var paths = jsonDocument.RootElement.GetProperty("paths");
        var services = GroupEndpointsByTag(paths);

        Console.WriteLine($"Found {services.Count} service groups");

        foreach (var service in services)
        {
            string serviceName = ToPascalCase(service.Key);
            GenerateServiceInterface(serviceName, service.Value, outputPath);
            GenerateServiceImplementation(serviceName, service.Value, outputPath);
        }

        return Task.CompletedTask;
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

    private Dictionary<string, List<EndpointInfo>> GroupEndpointsByTag(JsonElement paths)
    {
        var services = new Dictionary<string, List<EndpointInfo>>();

        foreach (var path in paths.EnumerateObject())
        {
            string url = path.Name;

            foreach (var method in path.Value.EnumerateObject())
            {
                string httpMethod = method.Name.ToUpper();
                var endpoint = method.Value;

                // Skip if required properties are missing
                if (!endpoint.TryGetProperty("tags", out var tagsElement) ||
                    !endpoint.TryGetProperty("operationId", out var operationIdElement))
                {
                    Console.WriteLine($"⚠ Skipping endpoint {url} - Missing required properties");
                    continue;
                }

                var tags = tagsElement.EnumerateArray()
                    .Select(t => t.GetString())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                string tag = tags.FirstOrDefault() ?? "General";
                string operationId = operationIdElement.GetString();

                if (string.IsNullOrEmpty(operationId))
                {
                    Console.WriteLine($"⚠ Skipping endpoint {url} - Empty operationId");
                    continue;
                }

                if (!services.ContainsKey(tag))
                    services[tag] = new List<EndpointInfo>();

                services[tag].Add(new EndpointInfo
                {
                    Url = url,
                    HttpMethod = httpMethod,
                    OperationId = operationId,
                    Summary = endpoint.TryGetProperty("summary", out var summary) ? summary.GetString() : "",
                    Parameters = GetParameters(endpoint),
                    ReturnType = GetReturnType(endpoint),
                    Tag = tag
                });
            }
        }

        return services;
    }

    private void GenerateServiceInterface(string serviceName, List<EndpointInfo> endpoints, string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using GeneratedCode.Models;");
        sb.AppendLine();

        serviceName = CleanInterfaceNameAdvanced(ToValidClassName(serviceName));
        string interfaceName = $"I{serviceName}Service";
        string interfacePath = Path.Combine(outputPath, "Interfaces");
        Directory.CreateDirectory(interfacePath);

        sb.AppendLine("namespace GeneratedCode.Services.Interfaces;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Service interface for {serviceName} operations");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public interface {interfaceName}");
        sb.AppendLine("{");

        foreach (var endpoint in endpoints)
        {
            string methodSignature = GenerateMethodSignature(endpoint);
            if (!string.IsNullOrEmpty(methodSignature))
            {
                if (!string.IsNullOrEmpty(endpoint.Summary))
                {
                    sb.AppendLine("    /// <summary>");
                    sb.AppendLine($"    /// {endpoint.Summary}");
                    sb.AppendLine("    /// </summary>");
                }

                sb.AppendLine($"    {methodSignature};");

                if (endpoints.LastIndexOf(endpoint) != -1)
                    sb.AppendLine();
            }
        }

        sb.Append('}');

        string filePath = Path.Combine(interfacePath, $"{interfaceName}.cs");
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

        Console.WriteLine($"✓ Generated interface: {interfaceName}");
    }

    private void GenerateServiceImplementation(string serviceName, List<EndpointInfo> endpoints, string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using GeneratedCode.Models;");
        sb.AppendLine("using GeneratedCode.Services.Interfaces;");
        sb.AppendLine();

        string className = $"{CleanInterfaceNameAdvanced(ToValidClassName(serviceName))}Service";
        string interfaceName = $"I{className}";
        string implementationPath = Path.Combine(outputPath, "Implementations");
        Directory.CreateDirectory(implementationPath);

        sb.AppendLine("namespace GeneratedCode.Services.Implementations;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Service implementation for {serviceName} operations");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class {className} : {interfaceName}");
        sb.AppendLine("{");

        foreach (var endpoint in endpoints)
        {
            string methodImplementation = GenerateMethodImplementation(endpoint);
            if (!string.IsNullOrEmpty(methodImplementation))
            {
                sb.AppendLine($"        {methodImplementation}");

                if (endpoints.LastIndexOf(endpoint) != -1)
                    sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        string filePath = Path.Combine(implementationPath, $"{className}.cs");
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

        Console.WriteLine($"✓ Generated implementation: {className}");
    }

    private string GenerateMethodSignature(EndpointInfo endpoint)
    {
        try
        {
            var parameters = new List<string>();

            foreach (var param in endpoint.Parameters)
            {
                string paramType = GetParameterTypeForSignature(param);
                parameters.Add($"{paramType} {ToCamelCase(param.Name)}");
            }

            string returnType = endpoint.ReturnType == "void" ? "Task" : $"Task<{endpoint.ReturnType}>";
            string parameterString = string.Join(", ", parameters);

            var cleanOperationId = ToPascalCase(CleanOperationId(endpoint.OperationId));
            return $"{returnType} {cleanOperationId}Async({parameterString})";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error generating signature for {endpoint.OperationId}: {ex.Message}");
            return null;
        }
    }

    private string CleanOperationId(string operationId)
    {
        if (string.IsNullOrEmpty(operationId))
            return operationId;

        int usingIndex = operationId.IndexOf("Using", StringComparison.OrdinalIgnoreCase);
        return usingIndex >= 0 ? operationId.Substring(0, usingIndex) : operationId;
    }

    private string GenerateMethodImplementation(EndpointInfo endpoint)
    {
        try
        {
            var parameters = endpoint.Parameters.Select(p =>
            {
                string paramType = GetParameterTypeForSignature(p);
                return $"{paramType} {ToCamelCase(p.Name)}";
            }).ToList();

            string parameterString = string.Join(", ", endpoint.Parameters.Select(p => ToCamelCase(p.Name)).ToList());
            string parameterWithTypeString = string.Join(", ", parameters);
            string returnType = endpoint.ReturnType == "void" ? "Task" : $"Task<{endpoint.ReturnType}>";

            var sb = new StringBuilder();
            var cleanOperationId = ToPascalCase(CleanOperationId(endpoint.OperationId));

            if (!string.IsNullOrEmpty(endpoint.Summary))
            {
                sb.AppendLine("/// <summary>");
                sb.AppendLine($"    /// {endpoint.Summary}");
                sb.AppendLine("    /// </summary>");
            }

            sb.AppendLine($"    public async {returnType} {cleanOperationId}Async({parameterWithTypeString})");
            sb.AppendLine("    {");

            if (endpoint.ReturnType != "void")
            {
                sb.AppendLine($"        return await _apiClient.{cleanOperationId}Async({parameterString});");
            }
            else
            {
                sb.AppendLine($"        await _apiClient.{cleanOperationId}Async({parameterString});");
            }

            sb.Append("    }");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error generating implementation for {endpoint.OperationId}: {ex.Message}");
            return null;
        }
    }

    private List<ParameterInfo> GetParameters(JsonElement endpoint)
    {
        var parameters = new List<ParameterInfo>();

        if (endpoint.TryGetProperty("parameters", out var paramsElement))
        {
            foreach (var param in paramsElement.EnumerateArray())
            {
                // Skip if required properties are missing
                if (!param.TryGetProperty("name", out var nameElement) ||
                    !param.TryGetProperty("in", out var inElement))
                {
                    continue;
                }

                string name = nameElement.GetString();
                string inType = inElement.GetString();

                parameters.Add(new ParameterInfo
                {
                    Name = name,
                    In = inType,
                    Type = GetParameterType(param),
                    Required = param.TryGetProperty("required", out var required) && required.GetBoolean(),
                    Description = param.TryGetProperty("description", out var description) ? description.GetString() : ""
                });
            }
        }

        return parameters;
    }

    private string GetParameterType(JsonElement parameter)
    {
        if (parameter.TryGetProperty("schema", out var schema))
        {
            if (schema.TryGetProperty("$ref", out var refElement))
            {
                string refType = refElement.GetString()?.Split('/').Last();
                return refType ?? "object";
            }

            if (schema.TryGetProperty("type", out var typeElement))
            {
                return GetCSharpTypeFromSwaggerType(typeElement.GetString(), schema);
            }
        }

        if (parameter.TryGetProperty("type", out var typeElement2))
        {
            return GetCSharpTypeFromSwaggerType(typeElement2.GetString(), parameter);
        }

        return "object";
    }

    private string GetParameterTypeForSignature(ParameterInfo param)
    {
        // For optional parameters, make them nullable
        if (!param.Required && !param.Type.StartsWith("List<") && !param.Type.StartsWith("Dictionary<"))
        {
            return param.Type + "?";
        }

        return param.Type;
    }

    private string GetReturnType(JsonElement endpoint)
    {
        if (endpoint.TryGetProperty("responses", out var responses) &&
            responses.TryGetProperty("200", out var response200))
        {
            if (response200.TryGetProperty("schema", out var schema))
            {
                if (schema.TryGetProperty("$ref", out var refElement))
                {
                    string refType = refElement.GetString()?.Split('/').Last();
                    return refType ?? "object";
                }

                if (schema.TryGetProperty("type", out var typeElement))
                {
                    string type = typeElement.GetString();
                    if (type == "array" && schema.TryGetProperty("items", out var items))
                    {
                        if (items.TryGetProperty("$ref", out var itemRef))
                        {
                            string itemType = itemRef.GetString()?.Split('/').Last();
                            return $"List<{itemType ?? "object"}>";
                        }
                    }
                    return GetCSharpTypeFromSwaggerType(type, schema);
                }
            }

            // If no schema but 200 response exists, assume success
            return "void";
        }

        return "void";
    }

    private string GetCSharpTypeFromSwaggerType(string swaggerType, JsonElement element)
    {
        return swaggerType switch
        {
            "string" => "string",
            "integer" => element.TryGetProperty("format", out var format) && format.GetString() == "int64"
                        ? "long" : "int",
            "number" => "decimal",
            "boolean" => "bool",
            "array" => "List<object>",
            "object" => "object",
            _ => "object"
        };
    }

    private string ToValidClassName(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "General";

        // Remove invalid characters and convert to PascalCase
        string cleaned = new(input.Where(char.IsLetterOrDigit).ToArray());

        if (string.IsNullOrEmpty(cleaned))
            return "General";

        return char.ToUpper(cleaned[0]) + cleaned[1..];
    }

    private string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "param";

        return char.ToLower(input[0]) + input.Substring(1);
    }

    public static string CleanInterfaceNameAdvanced(string interfaceName, bool removeResource = true, bool removeVersion = true)
    {
        if (string.IsNullOrEmpty(interfaceName))
            return interfaceName;

        string pattern = @"(?<!^)(?:";
        List<string> patterns = [];

        if (removeResource)
            patterns.Add(@"resource");

        if (removeVersion)
            patterns.Add(@"v\d+");

        pattern += string.Join("|", patterns) + @")";

        if (patterns.Count == 0)
            return interfaceName;

        string result = Regex.Replace(interfaceName, pattern, "", RegexOptions.IgnoreCase);
        return result;
    }
}