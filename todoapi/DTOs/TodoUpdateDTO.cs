namespace todoapi.DTOs;

internal class TodoUpdateDTO
{
    public string? Name { set; get; }
    public string? Description { set; get; }
    public bool? IsCompleted { get; set; }
}