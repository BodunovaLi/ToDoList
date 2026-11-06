
namespace ToDoList.Core.DTOs;

public class CreateTaskRequest
{
    public string Name { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    public DateTime? DueDate { get; set; }

    public List<SubtaskDraft> Subtasks { get; set; } = new();
}
