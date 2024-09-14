using Microsoft.Extensions.Configuration;
using TRINET_CLIENT.Global;

namespace TRINET_CLIENT
{
    public partial class MainPage : ContentPage
    {
        public IConfiguration Configuration;
        private bool IsAuthenticated = false;
        public TrinetCoreApi Api { get; set; }
        private UserSettings UserSettings;

        public MainPage(UserSettings userSettings, IConfiguration config, TrinetCoreApi api)
        {
            InitializeComponent();

            Configuration = config;
            Api = api;
            UserSettings = userSettings;
            Initialise();
        }


        private async void Initialise()
        {
            LoadingLayout.IsVisible = true;
            ErrorLayout.IsVisible = false;

            var serverUp = await Api.AssertServerConnection();
            if (!serverUp)
            {
                LoadingLayout.IsVisible = false;
                ErrorLayout.IsVisible = true;
                return;
            }


            await Api.AssertInitialised();
            bool authenticated = await Api.ValidateToken();            
            if (!authenticated)
            {
                await Shell.Current.GoToAsync(nameof(Login));
                return;
            }

            if (authenticated)
            {
                await Shell.Current.GoToAsync(nameof(HomePage));
            }
        }


        private void RetryButton_Clicked(object sender, EventArgs e)
        {
            Initialise();
        }


        private void OfflineButton_Clicked(object sender, EventArgs e)
        {
            UserSettings.SetOfflineMode(true);
            Shell.Current.GoToAsync(nameof(HomePage));
        }
    }

}
