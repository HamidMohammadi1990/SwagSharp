using System.Text.Json;

namespace SwagSharp.Api.DTOs;

public record ModelInfo
{
    public string Name { get; set; }
    public JsonElement Definition { get; set; }
}