using System.Text;

namespace SwagSharp.Api.Extensions;

public static class StringExtensions
{
    public static string SanitizeModelName(this string modelName)
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

    public static string ToPascalCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return "UnknownProperty";

        // Remove invalid characters and convert to PascalCase
        string cleaned = new(input.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

        if (string.IsNullOrEmpty(cleaned))
            return "UnknownProperty";

        return char.ToUpper(cleaned[0]) + cleaned[1..];
    }

    public static bool IsValueType(this string type)
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

    public static bool IsCommonWord(this string word)
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

    public static List<string> SplitCamelCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return [];

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

    public static string ExtractMainEntityName(this string modelName)
    {
        var words = modelName.SplitCamelCase();

        if (words.Count == 0)
            return "Common";

        foreach (var word in words)
        {
            if (word.Length > 2 && !word.IsCommonWord())
                return word;
        }

        return words.FirstOrDefault() ?? "Common";
    }

    public static string ExtractEntityCategory(this string modelName)
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
        string entityName = cleanedName.ExtractMainEntityName();

        return entityName;
    }
}