namespace SwagSharp.Application.DTOs;

public class ParameterInfo
{
    public string Name { get; set; }
    public string In { get; set; } // path, query, body, header
    public string Type { get; set; }
    public bool Required { get; set; }
    public string Description { get; set; }
}