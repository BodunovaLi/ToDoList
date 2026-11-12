using System;
using System.Collections.Generic;
using System.Text;

namespace ToDoList.Navigation
{
    public interface INavigationService
    {
        Task OpenTaskEditorAsync(int? taskId = null);
        Task OpenTaskDetailsAsync(int taskId);
        Task GoBackAsync();
    }
}
