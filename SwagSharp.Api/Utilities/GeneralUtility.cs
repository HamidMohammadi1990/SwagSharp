using SwagSharp.Api.DTOs;
using System.Text.RegularExpressions;

namespace SwagSharp.Api.Utilities;

public static class GeneralUtility
{
    public static string GetNullableIndicator(bool isRequired)
    {
        return isRequired ? "" : "?";
    }

    public static string GetDefaultValue(bool isRequired)
    {
        return isRequired ? " = default;" : "";
    }

    public static string GetParameterTypeForSignature(ParameterInfo param)
    {
        // For optional parameters, make them nullable
        if (!param.Required && !param.Type.StartsWith("List<") && !param.Type.StartsWith("Dictionary<"))
            return param.Type + "?";

        return param.Type;
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

    public static string ToPlural(string singular)
    {
        if (string.IsNullOrWhiteSpace(singular))
            return singular;

        // کلمات غیرقابل شمارش
        var uncountable = new HashSet<string>
        {
            "information", "advice", "news", "money", "music", "water",
            "rice", "sugar", "electricity", "physics", "economics"
        };

        string lower = singular.ToLower();

        if (uncountable.Contains(lower))
            return singular;

        // استثناهای خاص
        var irregulars = new Dictionary<string, string>
        {
            // حیوانات
            {"fish", "fish"}, {"deer", "deer"}, {"sheep", "sheep"},
            {"swine", "swine"}, {"trout", "trout"}, {"salmon", "salmon"},
        
            // سایر استثناها
            {"aircraft", "aircraft"}, {"series", "series"}, {"species", "species"},
            {"means", "means"}, {"headquarters", "headquarters"}
        };

        if (irregulars.TryGetValue(lower, out string? value))
            return ApplyCase(singular, value);

        // قاعده‌های مختلف
        return ApplyPluralRule(singular, lower);
    }

    public static string ApplyPluralRule(string word, string lower)
    {
        // کلمات ending with s, x, z, ch, sh
        if (lower.EndsWith('s') || lower.EndsWith('x') || lower.EndsWith('z') ||
            lower.EndsWith("ch") || lower.EndsWith("sh"))
        {
            return word + "es";
        }

        // کلمات ending with y
        if (lower.EndsWith('y') && lower.Length > 1)
        {
            char beforeY = lower[lower.Length - 2];
            if (IsConsonant(beforeY))
            {
                return word.Substring(0, word.Length - 1) + "ies";
            }
            return word + "s";
        }

        // کلمات ending با f یا fe
        if (lower.EndsWith("f"))
        {
            return string.Concat(word.AsSpan(0, word.Length - 1), "ves");
        }
        if (lower.EndsWith("fe"))
        {
            return string.Concat(word.AsSpan(0, word.Length - 2), "ves");
        }

        // کلمات ending با o
        if (lower.EndsWith('o') && lower.Length > 1)
        {
            char beforeO = lower[^2];
            if (IsConsonant(beforeO))
            {
                return word + "es";
            }
        }

        // قاعده کلی
        return word + "s";
    }

    public static bool IsConsonant(char c)
    {
        return !IsVowel(c);
    }

    public static bool IsVowel(char c)
    {
        char lowerC = char.ToLower(c);
        return lowerC == 'a' || lowerC == 'e' || lowerC == 'i' || lowerC == 'o' || lowerC == 'u';
    }

    public static string ApplyCase(string original, string newWord)
    {
        if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(newWord))
            return newWord;

        if (char.IsUpper(original[0]))
        {
            return char.ToUpper(newWord[0]) + newWord[1..];
        }

        return newWord;
    }

    public static string ExtractType(string input)
    {
        if (input.StartsWith("List<") && input.EndsWith(">"))
            return input[5..^1];

        return input;
    }
}