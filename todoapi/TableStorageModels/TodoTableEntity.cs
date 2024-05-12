using Azure.Data.Tables;
using Azure;
using System;
using todoapi.AppModels;
using ServerlessFuncs;

namespace todoapi.TableStorageModels;

public class BaseTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = null!;
    public string RowKey { get; set; } = null!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

public class TodoTableEntity : BaseTableEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
}

internal static class Mappings
{
    public static TodoTableEntity AsTableEntity(this Todo todo) => new()
    {
        PartitionKey = TodoApi.PartitionKey,
        RowKey = todo.Id,
        CreatedAt = todo.CreatedAt,
        IsCompleted = todo.IsCompleted,
        Name = todo.Name,
        Description = todo.Description
    };

    internal static Todo AsTodo(this TodoTableEntity todo) => new()
    {
        Id = todo.RowKey,
        CreatedAt = todo.CreatedAt,
        Name = todo.Name,
        Description = todo.Description,
        IsCompleted = todo.IsCompleted
    };
}