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
    public string TaskDescription { get; set; } = null!;
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
        TaskDescription = todo.TaskDescription
    };

    internal static Todo AsTodo(this TodoTableEntity todo) => new()
    {
        Id = todo.RowKey,
        CreatedAt = todo.CreatedAt,
        TaskDescription = todo.TaskDescription,
        IsCompleted = todo.IsCompleted
    };
}