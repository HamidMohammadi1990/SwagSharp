using System.Text.Json;
using SwagSharp.Api.DTOs;

namespace SwagSharp.Api.Extensions;

public static class JsonElementExtensions
{
	public static bool HasProperties(this JsonElement definition)
	{
		return definition.TryGetProperty("properties", out _);
	}

	public static bool IsSimpleType(this JsonElement definition)
	{
		return definition.TryGetProperty("type", out _) &&
			   !definition.TryGetProperty("properties", out _) &&
			   !definition.TryGetProperty("enum", out _);
	}

	public static bool IsEnumDefinition(this JsonElement definition)
	{
		return definition.TryGetProperty("enum", out _);
	}

	public static bool IsPropertyRequired(this JsonElement property, string propertyName, JsonElement parentDefinition)
	{
		if (parentDefinition.TryGetProperty("required", out var requiredArray))
		{
			foreach (var requiredProp in requiredArray.EnumerateArray())
			{
				if (requiredProp.GetString() == propertyName)
					return true;
			}
		}

		if (property.TryGetProperty("required", out var requiredElement) &&
			requiredElement.ValueKind == JsonValueKind.True)
		{
			return true;
		}

		if (property.TryGetProperty("nullable", out var nullableElement) &&
			nullableElement.ValueKind == JsonValueKind.True)
		{
			return false;
		}

		if (property.TryGetProperty("type", out var typeElement))
		{
			string type = typeElement.GetString();
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

		return HasValidationConstraints(property);
	}

	private static bool HasValidationConstraints(JsonElement property)
	{
		return property.TryGetProperty("minLength", out _) ||
			   property.TryGetProperty("maxLength", out _) ||
			   property.TryGetProperty("minimum", out _) ||
			   property.TryGetProperty("maximum", out _) ||
			   property.TryGetProperty("pattern", out _) ||
			   property.TryGetProperty("enum", out _);
	}

	public static string GetDescription(this JsonElement element)
	{
		if (element.TryGetProperty("description", out var descriptionElement))
			return descriptionElement.GetString() ?? string.Empty;

		return string.Empty;
	}

	public static string GetCSharpType(this JsonElement property)
	{
		// Check for $ref first
		if (property.TryGetProperty("$ref", out var refElement))
		{
			string refPath = refElement.GetString();
			return (refPath?.Split('/').Last() ?? "object").SanitizeModelName();
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

	public static string GetArrayType(this JsonElement property)
	{
		if (property.TryGetProperty("items", out var items))
		{
			if (items.TryGetProperty("$ref", out var refElement))
			{
				string refType = refElement.GetString()?.Split('/').Last() ?? "object";
				return $"List<{refType.SanitizeModelName()}>";
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

	public static string GetObjectType(this JsonElement property)
	{
		if (property.TryGetProperty("additionalProperties", out var additionalProps))
		{
			if (additionalProps.TryGetProperty("$ref", out var refElement))
			{
				string refType = refElement.GetString()?.Split('/').Last() ?? "object";
				return $"Dictionary<string, {refType.SanitizeModelName()}>";
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

	public static Dictionary<string, List<ModelInfo>> CategorizeByEntityName(this JsonElement definitions)
	{
		var categories = new Dictionary<string, List<ModelInfo>>();

		foreach (var definition in definitions.EnumerateObject())
		{
			string modelName = definition.Name;
			string category = modelName.ExtractEntityCategory();

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

	public static string GetReturnType(this JsonElement endpoint)
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

	public static string GetCSharpTypeFromSwaggerType(this string swaggerType, JsonElement element)
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

	public static string GetParameterType(this JsonElement parameter)
	{
		if (parameter.TryGetProperty("schema", out var schema))
		{
			if (schema.TryGetProperty("$ref", out var refElement))
			{
				string refType = refElement.GetString()?.Split('/').Last();
				return refType ?? "object";
			}

			if (schema.TryGetProperty("type", out var typeElement))
				return typeElement.GetString().GetCSharpTypeFromSwaggerType(schema);
		}

		if (parameter.TryGetProperty("type", out var typeElement2))
			return typeElement2.GetString().GetCSharpTypeFromSwaggerType(parameter);

		return "object";
	}

	public static List<ParameterInfo> GetParameters(this JsonElement endpoint)
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
					Type = param.GetParameterType(),
					Required = param.TryGetProperty("required", out var required) && required.GetBoolean(),
					Description = param.TryGetProperty("description", out var description) ? description.GetString() : ""
				});
			}
		}

		return parameters;
	}

	public static Dictionary<string, List<EndpointInfo>> GroupEndpointsByTag(this JsonElement paths)
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
					Parameters = endpoint.GetParameters(),
					ReturnType = endpoint.GetReturnType(),
					Tag = tag
				});
			}
		}

		return services;
	}
}