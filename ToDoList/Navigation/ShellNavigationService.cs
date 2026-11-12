using System;
using System.Collections.Generic;
using System.Text;

namespace ToDoList.Navigation
{
    public sealed class ShellNavigationService : INavigationService
    {
        public Task OpenTaskEditorAsync(int? taskId = null)
        {
            if (taskId is <= 0) throw new ArgumentOutOfRangeException(nameof(taskId));
            return Shell.Current.GoToAsync(taskId.HasValue
                ? $"task-editor?mode=edit&taskId={taskId.Value}"
                : "task-editor?mode=create");
        }

        public Task OpenTaskDetailsAsync(int taskId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(taskId);
            return Shell.Current.GoToAsync($"task-details?taskId={taskId}");
        }

        public Task GoBackAsync() => Shell.Current.GoToAsync("..");
    }
}
