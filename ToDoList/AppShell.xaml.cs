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

        }


    }
}
