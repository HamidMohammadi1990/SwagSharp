namespace SwagSharp.Api.DTOs;

public record GenerateServiceRequest
{
    public IFormFile File { get; init; }
    public string ModelsNameSpace { get; init; }
    public string InterfacesNameSpace { get; init; }
    public string ServicesNameSpace { get; init; }


    public bool IsValid()
    {
        return File is not null &&
            File?.Length > 0 &&
            !string.IsNullOrWhiteSpace(ModelsNameSpace) &&
            !string.IsNullOrWhiteSpace(InterfacesNameSpace) &&
            !string.IsNullOrWhiteSpace(ServicesNameSpace);
    }
}