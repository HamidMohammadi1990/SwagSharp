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
}