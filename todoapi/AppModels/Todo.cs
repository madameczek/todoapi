using System;

namespace todoapi.AppModels;

internal class Todo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string TaskDescription { get; set; } = null!;
    public bool IsCompleted { get; set; }
}