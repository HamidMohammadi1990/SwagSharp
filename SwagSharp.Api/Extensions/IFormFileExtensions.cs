namespace SwagSharp.Api.Extensions;

public static class IFormFileExtensions
{
    public static async Task<string> ReadToEndAsync(this IFormFile file)
    {
		try
		{
            using var reader = new StreamReader(file!.OpenReadStream());
            var swaggerJson = await reader.ReadToEndAsync();
            return swaggerJson;
        }
		catch (Exception)
		{
            return string.Empty;
		}
    }
}