using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using ToDoList.Data;
using ToDoList.ViewModels;
using ToDoList.Core.Repositories;
using ToDoList.Services;

namespace ToDoList
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<AppDatabase>(_ =>
                new AppDatabase(
                    Path.Combine(FileSystem.AppDataDirectory, "todolist.db3")
                ));

            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<TaskRepository>();
            builder.Services.AddTransient<CategoryRepository>();
            builder.Services.AddTransient<TaskService>();

            return builder.Build();
        }
    }
}
