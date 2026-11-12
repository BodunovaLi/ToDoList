namespace ToDoList
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(
                            "task-editor",
                            typeof(ToDoList.Views.TaskEditorPage));
            Routing.RegisterRoute("task-details", typeof(ToDoList.Views.TaskDetailsPage));

        }


    }
}
