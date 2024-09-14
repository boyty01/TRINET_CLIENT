using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Reflection;
using TRINET_CLIENT.Global;
using TRINET_CLIENT.Pages;
using TRINET_CLIENT.Modules;
/* Unmerged change from project 'TRINET CLIENT (net8.0-maccatalyst)'
Before:
using System.Diagnostics;
After:
using System.Reflection;
*/

/* Unmerged change from project 'TRINET CLIENT (net8.0-ios)'
Before:
using System.Diagnostics;
After:
using System.Reflection;
*/

/* Unmerged change from project 'TRINET CLIENT (net8.0-android)'
Before:
using System.Diagnostics;
After:
using System.Reflection;
*/


namespace TRINET_CLIENT
{

    public class Settings
    {
        public CoreSettings CoreSettings { get; set; }
    }

    public class CoreSettings
    {
        public string CoreUrl { get; set; } = null!;
        public string? JwtToken { get; set; }
    }
    public static class MauiProgram
    {


        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureMopups()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });


            var a = Assembly.GetExecutingAssembly();
            using var stream = a.GetManifestResourceStream("TRINET_CLIENT.appsettings.json");
            var config = new ConfigurationBuilder()
                        .AddJsonStream(stream)
                        .Build();

            builder.UseSkiaSharp();

            builder.Configuration.AddConfiguration(config);


            builder.Services.AddSingleton<TrinetCoreApi>();
            builder.Services.AddSingleton<UserSettings>();
            builder.Services.AddSingleton<Modules.Wiz.WizModule>();
            builder.Services.AddTransient<Login>();
            builder.Services.AddTransient<RoomPage>();
            builder.Services.AddTransient<DeviceLight>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<MainPage>();
#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();

        }
    }
}
