using System.Text.Json.Serialization;

namespace todoapi.DTOs;

internal class TodoCreateDTO
{
    [JsonRequired]
    public string Name { set; get; } = null!;
}