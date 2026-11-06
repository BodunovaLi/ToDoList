namespace ToDoList.Core.DTOs;

public class UpdateTaskRequest
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public DateTime? DueDate { get; set; }
}