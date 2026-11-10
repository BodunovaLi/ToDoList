namespace ToDoList.Core.DTOs;

public class UpdateSubtaskDraft
{
    // Zero means a new subtask; positive values identify existing subtasks.
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
