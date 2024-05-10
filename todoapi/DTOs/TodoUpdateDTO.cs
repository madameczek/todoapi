namespace todoapi.DTOs;

internal class TodoUpdateDTO
{
    public string TaskDescription { set; get; } = null!;
    public bool? IsCompleted { get; set; }
}