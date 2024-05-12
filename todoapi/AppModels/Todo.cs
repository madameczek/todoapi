using System;

namespace todoapi.AppModels;

internal class Todo
{
    public string Id { get; init; } = Guid.NewGuid().ToString("n");
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
}