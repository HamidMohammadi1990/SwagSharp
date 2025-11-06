using System.Text;

namespace SwagSharp.Api.Utilities;

public static class FileUtility
{
    public static async Task WriteFileAsync(string directory, string fileName, string content)
    {
        string filePath = Path.Combine(directory, fileName);
        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
    }
}