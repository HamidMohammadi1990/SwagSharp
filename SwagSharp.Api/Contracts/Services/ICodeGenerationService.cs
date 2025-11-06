using SwagSharp.Api.DTOs;

namespace SwagSharp.Api.Contracts.Services;

public interface ICodeGenerationService
{
    Task GenerateAsync(GenerateServiceRequest request);
}