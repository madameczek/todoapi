using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Azure.Data.Tables;
using Azure;
using todoapi.DTOs;
using todoapi.TableStorageModels;
using todoapi.AppModels;

namespace ServerlessFuncs;

public class TodoApi
{
    private const string TableName = "todos";
    public const string PartitionKey = "TODO";

    private readonly ILogger<TodoApi> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public TodoApi(ILogger<TodoApi> logger, JsonSerializerOptions jsonSerializerOptions)
    {
        _logger = logger;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public class TodoResponse(TodoTableEntity entity, HttpResponse response)
    {
        [TableOutput(TableName, Connection = "AzureWebJobsStorage")]
        public TodoTableEntity Entity { get; } = entity;

        public HttpResponse Response { get; } = response;
    }

    [Function("CreateTodo")]
    public async Task<TodoResponse> Add([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo")] HttpRequest req)
    {
        _logger.LogInformation("Create a todo");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonSerializer.Deserialize<TodoCreateDTO>(requestBody);

        var todo = new Todo() { TaskDescription = data.TaskDescription };
        var response = req.HttpContext.Response;
        response.StatusCode = StatusCodes.Status200OK;
        await response.WriteAsJsonAsync(todo);
        return new TodoResponse(todo.AsTableEntity(), response);
    }

    [Function("GetTodos")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo")] HttpRequest req,
        [TableInput(TableName, Connection = "AzureWebJobsStorage")] TableClient todoTable)
    {
        var todosCount = await todoTable.QueryAsync<TodoTableEntity>().CountAsync();
        _logger.LogInformation("Getting todos. There is {Count} entries.", todosCount);

        // Be careful, it loads all items into memory.
        var todos = await todoTable.QueryAsync<TodoTableEntity>().ToArrayAsync();
        return new OkObjectResult(todos.Select(Mappings.AsTodo));
    }

    [Function("GetTodoById")]
    public async Task<IActionResult> GetById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo/{id}")] HttpRequest req,
        [TableInput(TableName, PartitionKey, "{id}", Connection = "AzureWebJobsStorage")] TableClient todoTable,
        string id)
    {
        _logger.LogInformation("Getting todo by id {Id}", id);

        TodoTableEntity existingRow;
        try
        {
            var findResult = await todoTable.GetEntityAsync<TodoTableEntity>(PartitionKey, id);
            existingRow = findResult.Value;
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            return new NotFoundResult();
        }
        return new OkObjectResult(existingRow.AsTodo());
    }

    [Function("UpdateTodo")]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todo/{id}")] HttpRequest req,
        [TableInput(TableName, Connection = "AzureWebJobsStorage")] TableClient todoTable,
        string id)
    {
        _logger.LogInformation("Updating todo {Id}", id);

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var updatedTodo = JsonSerializer.Deserialize<TodoUpdateDTO>(requestBody, _jsonSerializerOptions);
        if (updatedTodo is null)
            return new BadRequestResult();

        var findResult = await todoTable.GetEntityIfExistsAsync<TodoTableEntity>(PartitionKey, id);
        if (!findResult.HasValue)
            return new NotFoundResult();

        TodoTableEntity existingRow;
        existingRow = findResult.Value!;
      
        if (updatedTodo.IsCompleted != null) 
            existingRow.IsCompleted = (bool)updatedTodo.IsCompleted; 

        if(!string.IsNullOrEmpty(updatedTodo.TaskDescription))
            existingRow.TaskDescription = updatedTodo.TaskDescription;

        await todoTable.UpdateEntityAsync(existingRow, existingRow.ETag, TableUpdateMode.Replace);

        return new OkObjectResult(existingRow.AsTodo());
    }

    [Function("DeleteTodo")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todo/{id}")] HttpRequest req,
        [TableInput(TableName, Connection = "AzureWebJobsStorage")] TableClient todoTable,
        string id)
    {
        _logger.LogInformation("Deleting todo {id}", id);

        var response = await todoTable.DeleteEntityAsync(PartitionKey, id, ETag.All);
        if(response.Status == 404)
            return new NotFoundResult();
        
        return new NoContentResult();
    }
}