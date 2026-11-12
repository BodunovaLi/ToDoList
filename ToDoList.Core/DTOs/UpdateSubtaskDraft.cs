namespace ToDoList.Core.DTOs;

public class UpdateSubtaskDraft
{

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}
