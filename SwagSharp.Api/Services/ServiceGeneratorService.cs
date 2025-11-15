using System.Text.Json;
using SwagSharp.Api.Utilities;
using SwagSharp.Api.Extensions;
using SwagSharp.Api.Contracts.Services;

namespace SwagSharp.Api.Services;

public class ServiceGeneratorService : IServiceGeneratorService
{
    public Task GenerateAsync(string outputPath, JsonDocument jsonDocument, string modelsNameSpace, string interfacesNameSpace, string servicesNameSpace)
    {
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        var paths = jsonDocument.RootElement.GetProperty("paths");
        var services = paths.GroupEndpointsByTag();

        Console.WriteLine($"Found {services.Count} service groups");

        foreach (var service in services)
        {            
            string serviceName = GeneralUtility.CleanInterfaceNameAdvanced(service.Key.ToPascalCase().ToValidClassName());
            CodeGeneratoUtility.GenerateServiceContract(serviceName, service.Value, outputPath, modelsNameSpace, interfacesNameSpace);
            CodeGeneratoUtility.GenerateService(serviceName, service.Value, outputPath, servicesNameSpace, interfacesNameSpace);
        }

        return Task.CompletedTask;
    }
}