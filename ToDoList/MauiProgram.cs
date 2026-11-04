using Microsoft.Extensions.Logging;
using ToDoList.Data;
using Microsoft.Maui.Storage;

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

            return builder.Build();
        }
    }
}
