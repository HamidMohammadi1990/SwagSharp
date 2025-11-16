using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SwagSharp.Api.DTOs;
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

    public static string GenerateEnumClass(string modelName, JsonElement definition, string modelsNameSpace, string pluralModelName)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"namespace {modelsNameSpace}.{pluralModelName}.Enums;");
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
                string? enumName = enumValue.ValueKind == JsonValueKind.String
                    ? enumValue.GetString()
                    : enumValue.GetRawText();

                if (string.IsNullOrWhiteSpace(enumName)) continue;

                string pascalEnumName = (enumName.Replace("-", "_").Replace(" ", "_").Replace(".", "_")).ToPascalCase();

                sb.AppendLine($"    {pascalEnumName} = {index},");

                index++;
            }
        }

        // Remove trailing comma from last entry
        string result = sb.ToString().TrimEnd();
        if (result.EndsWith(','))
            result = result[..^1];

        result += "}";
        return result;
    }

    public static string GenerateSimpleTypeClass(string modelName, JsonElement definition, string modelsNameSpace, string pluralModelName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();

        sb.AppendLine($"namespace {modelsNameSpace}.{pluralModelName};");
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

    public static string GenerateFallbackModel(string modelName, string modelsNameSpace, string pluralModelName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();

        sb.AppendLine($"namespace {modelsNameSpace}.{pluralModelName};");
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

    public static string GenerateModelClass(string modelName, JsonElement properties, JsonElement definition, string modelsNameSpace, string pluralModelName)
    {
        var sb = new StringBuilder();

        // Using statements
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {modelsNameSpace}.{pluralModelName};");
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

            string safePropertyName = GetSafePropertyName(propName, modelName, usedPropertyNames);
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

    public static void GenerateServiceContract(string serviceName, List<EndpointInfo> endpoints, string outputPath, string modelsNameSpace, string interfacesNameSpace)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"using {modelsNameSpace}.{GeneralUtility.ToPlural(serviceName)};");
        sb.AppendLine();

        string interfaceName = $"I{serviceName}Service";
        string interfacePath = Path.Combine(outputPath, "Contracts");
        Directory.CreateDirectory(interfacePath);

        sb.AppendLine($"namespace {interfacesNameSpace};");
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

    public static string GenerateMethodSignature(EndpointInfo endpoint)
    {
        try
        {
            var parameters = new List<string>();

            foreach (var param in endpoint.Parameters)
            {
                string paramType = GeneralUtility.GetParameterTypeForSignature(param);
                parameters.Add($"{paramType} {(param.Name).ToCamelCase()}");
            }

            string returnType = endpoint.ReturnType == "void" ? "Task" : $"Task<{endpoint.ReturnType}>";
            string parameterString = string.Join(", ", parameters);

            var cleanOperationId = endpoint.OperationId.CleanOperationId().ToPascalCase();
            return $"{returnType} {cleanOperationId}Async({parameterString})";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error generating signature for {endpoint.OperationId}: {ex.Message}");
            return null;
        }
    }

    public static void GenerateService(string serviceName, List<EndpointInfo> endpoints, string outputPath, string servicesNameSpace, string interfacesNameSpace)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"using {interfacesNameSpace};");
        sb.AppendLine($"using SwagSharp.Api.Models;");
        sb.AppendLine("using SwagSharp.Api.Contracts.Services;");
        sb.AppendLine();

        string className = $"{serviceName}Service";
        string interfaceName = $"I{className}";
        string implementationPath = Path.Combine(outputPath, "Services");
        Directory.CreateDirectory(implementationPath);

        sb.AppendLine($"namespace {servicesNameSpace};");
        sb.AppendLine();
        sb.AppendLine($"public class {className}(ICBaasClientService cBaasClientService) : {interfaceName}");
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

    public static string GenerateMethodImplementation(EndpointInfo endpoint)
    {
        try
        {
            var parameters = endpoint.Parameters.Select(p =>
            {
                string paramType = GeneralUtility.GetParameterTypeForSignature(p);
                var method = endpoint.HttpMethod.ToLower();
                return (method != "post" && method != "put") ? $"{paramType} {p.Name.ToCamelCase()}" : $"{paramType} request";
            }).ToList();

            string parameterString = string.Join(", ", endpoint.Parameters.Select(p => p.Name.ToCamelCase()).ToList());
            string parameterWithTypeString = string.Join(", ", parameters);
            string returnType = endpoint.ReturnType == "void" ? "Task<Response<bool>>" : $"Task<Response<{endpoint.ReturnType}>>";

            var sb = new StringBuilder();
            var cleanOperationId = endpoint.OperationId.CleanOperationId().ToPascalCase();

            if (!string.IsNullOrEmpty(endpoint.Summary))
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {endpoint.Summary}");
                sb.AppendLine("    /// </summary>");
            }

            sb.AppendLine($"    public async {returnType} {cleanOperationId}Async({parameterWithTypeString})");
            sb.AppendLine("    {");

            string clientCall = GenerateClientCall(endpoint);
            sb.AppendLine($"        return {clientCall};");

            sb.Append("    }");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error generating implementation for {endpoint.OperationId}: {ex.Message}");
            return null;
        }
    }

    private static string GenerateClientCall(EndpointInfo endpoint)
    {
        string methodName = GetClientMethodName(endpoint.HttpMethod);
        string url = BuildUrl(endpoint);

        // تشخیص نوع Generic parameters
        string genericParameters = GetGenericParameters(endpoint);

        // ساخت پارامترهای متد
        string methodParameters = BuildMethodParameters(endpoint, url);

        return $"await cBaasClientService.{methodName}{genericParameters}({methodParameters})";
    }

    private static string BuildUrl(EndpointInfo endpoint)
    {
        string url = CleanUrl(endpoint.Url);

        // جایگزینی پارامترهای path
        var pathParams = endpoint.Parameters.Where(p => p.In == "path");
        foreach (var param in pathParams)
        {
            url = url.Replace($"{{{param.Name}}}", $"{{{param.Name.ToCamelCase()}}}");
        }

        // اضافه کردن query parameters برای GET
        if (endpoint.HttpMethod.ToLower() == "get")
        {
            var queryParams = endpoint.Parameters.Where(p => p.In == "query");
            if (queryParams.Any())
            {
                url += "?" + string.Join("&", queryParams.Select(p => $"{p.Name}={{{p.Name.ToCamelCase()}}}"));
            }
        }

        return $"$\"{url}\"";
    }

    private static string CleanUrl(string url)
    {
        return Regex.Replace(url, @"^\/api\/v\d+\/", "");
    }

    private static string GetGenericParameters(EndpointInfo endpoint)
    {
        if (endpoint.HttpMethod.ToLower() == "get")
        {
            if (endpoint.ReturnType != "void" && endpoint.ReturnType != "Task")
            {
                return $"<{endpoint.ReturnType}>";
            }
        }
        else if (endpoint.HttpMethod.ToLower() == "post" || endpoint.HttpMethod.ToLower() == "put")
        {
            var bodyParam = endpoint.Parameters.FirstOrDefault(p => p.In == "body");
            if (bodyParam != null && endpoint.ReturnType != "void" && endpoint.ReturnType != "Task")
            {
                return $"<{bodyParam.Type}, {endpoint.ReturnType}>";
            }
            else if (bodyParam != null)
            {
                return $"<{bodyParam.Type}, bool>";
            }
            else if (endpoint.ReturnType != "void" && endpoint.ReturnType != "Task")
            {
                return $"<{endpoint.ReturnType}>";
            }
        }

        return "";
    }

    private static string BuildMethodParameters(EndpointInfo endpoint, string url)
    {
        var parameters = new List<string> { url };

        // برای POST و PUT، پارامتر body را اضافه کن
        if (endpoint.HttpMethod.ToLower() == "post" || endpoint.HttpMethod.ToLower() == "put")
        {
            var bodyParam = endpoint.Parameters.FirstOrDefault(p => p.In == "body");
            if (bodyParam != null)
                parameters.Add("request");
        }

        return string.Join(", ", parameters);
    }

    private static string GetClientMethodName(string endpointMethodName)
    {
        return endpointMethodName.ToLower() switch
        {
            "post" => "PostAsync",
            "get" => "GetAsync",
            "put" => "PutAsync",
            "delete" => "DeleteAsync",
            _ => endpointMethodName.ToLower() + "Async"
        };
    }
}