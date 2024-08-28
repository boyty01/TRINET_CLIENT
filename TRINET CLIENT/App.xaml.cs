using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace TRINET_CLIENT
{
    public partial class App : Application
    {
        public App(IConfiguration Config, TrinetCoreApi api)
        {
            try
            {
                InitializeComponent();

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            MainPage = new AppShell();
            Routing.RegisterRoute(nameof(TRINET_CLIENT.MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(Login), typeof(Login));
            Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
        }
    }
}
