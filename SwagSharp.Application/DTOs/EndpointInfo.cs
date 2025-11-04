namespace SwagSharp.Application.DTOs;

public class EndpointInfo
{
    public string Url { get; set; }
    public string HttpMethod { get; set; }
    public string OperationId { get; set; }
    public string Summary { get; set; }
    public List<ParameterInfo> Parameters { get; set; } = [];
    public string ReturnType { get; set; }
    public string Tag { get; set; }
}