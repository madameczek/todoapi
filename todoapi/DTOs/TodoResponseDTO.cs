using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using todoapi.TableStorageModels;

namespace todoapi.DTOs;

public class TodoResponseDTO(TodoTableEntity? entity, HttpResponse response)
{
    [TableOutput(TodoApi.TableName, Connection = "AzureWebJobsStorage")]
    public TodoTableEntity? Entity { get; } = entity;

    public HttpResponse Response { get; } = response;
}