using System.Text.Json;
using SwagSharp.Api.Utilities;
using SwagSharp.Api.Extensions;
using SwagSharp.Api.Contracts.Services;

namespace SwagSharp.Api.Services;

public class ServiceGeneratorService : IServiceGeneratorService
{
	public Task GenerateAsync(string outputPath, JsonDocument jsonDocument)
	{
		if (!Directory.Exists(outputPath))
			Directory.CreateDirectory(outputPath);

		var paths = jsonDocument.RootElement.GetProperty("paths");
		var services = paths.GroupEndpointsByTag();

		Console.WriteLine($"Found {services.Count} service groups");

		foreach (var service in services)
		{
			string serviceName = service.Key.ToPascalCase();
			CodeGeneratoUtility.GenerateServiceInterface(serviceName, service.Value, outputPath);
			CodeGeneratoUtility.GenerateServiceImplementation(serviceName, service.Value, outputPath);
		}

		return Task.CompletedTask;
	}	
}