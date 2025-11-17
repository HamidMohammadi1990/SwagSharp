using SwagSharp.Api.Models;

namespace SwagSharp.Api.Utilities;

public static class TypeResolverUtility
{
    private static readonly HashSet<string> PrimitiveTypes =
    [
        "string", "int", "long", "decimal", "float", "double", "bool",
        "DateTime", "short", "byte", "char", "Guid", "object", "void"
    ];
    private static readonly HashSet<string> NullablePrimitiveTypes =
    [
        "int?", "long?", "decimal?", "float?", "double?", "bool?",
        "DateTime?", "short?", "byte?", "char?", "Guid?"
    ];

    public static bool IsPrimitiveType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return false;

        type = type.Trim();
        if (PrimitiveTypes.Contains(type))
            return true;

        int open = type.IndexOf('<');
        int close = type.LastIndexOf('>');

        if (open > 0 && close > open)
        {
            string genericName = type[..open].Trim();
            string genericArgs = type[(open + 1)..close].Trim();
            var parts = genericArgs.Split(',')
                                   .Select(p => p.Trim())
                                   .ToList();

            bool allPrimitive = parts.All(IsPrimitiveType);
            return allPrimitive;
        }

        return false;
    }

    public static bool IsNullablePrimitiveType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return false;

        type = type.Trim();

        if (type.EndsWith('?'))
        {
            string inner = type[..^1].Trim();
            return PrimitiveTypes.Contains(inner);
        }

        int open = type.IndexOf('<');
        int close = type.LastIndexOf('>');

        if (open > 0 && close > open)
        {
            string genericName = type[..open].Trim();
            string innerType = type[(open + 1)..close].Trim();

            string cleanName = genericName.Contains('.')
                ? genericName[(genericName.LastIndexOf('.') + 1)..]
                : genericName;

            if (cleanName == "Nullable")
            {
                return NullablePrimitiveTypes.Contains(innerType);
            }
        }

        return false;
    }

    public static string FindModelNameSpace(string typeName, List<ModelNameSpaceInfo> modelNameSpaces)
    {
        string cleanTypeName = ExtractLastGenericType(typeName);
        var model = modelNameSpaces.FirstOrDefault(m =>
            m.Name.Equals(cleanTypeName, StringComparison.OrdinalIgnoreCase));

        return model is not null ? model.NameSpace : "";
    }

    public static string ExtractLastGenericType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return typeName;

        int index = typeName.LastIndexOf('<');
        if (index >= 0 && index < typeName.Length - 1)
        {
            return typeName[(index + 1)..].Trim();
        }

        return typeName;
    }
}