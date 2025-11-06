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
}