using ToDoList.Data;

namespace ToDoList
{
    public partial class App : Application
    {
        private readonly AppDatabase _database;

        public App(AppDatabase database)
        {
            InitializeComponent();
            _database = database;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new ContentPage
            {
                Content = new ActivityIndicator
                {
                    IsRunning = true,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            });

            _ = InitializeAppAsync(window);

            return window;
        }

        private async Task InitializeAppAsync(Window window)
        {
            try
            {
                await _database.InitializeAsync();

                window.Page = new AppShell();
            }
            catch (Exception ex)
            {
                window.Page = new ContentPage
                {
                    Content = new Label
                    {
                        Text = $"Ошибка инициализации базы данных: {ex.Message}",
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                };
            }
        }
    }
}