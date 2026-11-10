namespace ToDoList.Core.DTOs;

public class UpdateTaskRequest
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public DateTime? DueDate { get; set; }

    // Null preserves existing subtasks; an empty list removes them all.
    public List<UpdateSubtaskDraft>? Subtasks { get; set; }
}
