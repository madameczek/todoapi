using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using todoapi.AppModels;
using todoapi.DTOs;
using todoapi.TableStorageModels;

namespace todoapi;

public class TodoApi
{
    public const string TableName = "todos";
    public const string PartitionKey = "TODO";

    private readonly ILogger<TodoApi> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public TodoApi(ILogger<TodoApi> logger, JsonSerializerOptions jsonSerializerOptions)
    {
        _logger = logger;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    [Function("CreateTodo")]
    public async Task<TodoResponseDTO> Add([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo")] HttpRequest req)
    {
        _logger.LogInformation("Create a todo");

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var response = req.HttpContext.Response;

        try
        {
            var data = JsonSerializer.Deserialize<TodoCreateDTO>(requestBody, _jsonSerializerOptions);
            var todo = new Todo { Name = data!.Name };
            response.StatusCode = StatusCodes.Status200OK;
            await response.WriteAsJsonAsync(todo);
            return new TodoResponseDTO(todo.AsTableEntity(), response);
        }
        catch
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
            return new TodoResponseDTO(null, response);
        }
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
      
        if(!string.IsNullOrEmpty(updatedTodo.Name))
            existingRow.Name = updatedTodo.Name;

        if (!string.IsNullOrEmpty(updatedTodo.Description))
            existingRow.Description = updatedTodo.Description;

        if (updatedTodo.IsCompleted != null)
            existingRow.IsCompleted = (bool)updatedTodo.IsCompleted;

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