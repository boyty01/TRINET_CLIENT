using Microsoft.Extensions.Configuration;

namespace TRINET_CLIENT
{
    public partial class MainPage : ContentPage
    {
        public IConfiguration Configuration;
        private bool IsAuthenticated = false;
        public TrinetCoreApi Api { get; set; }

        public MainPage(IConfiguration config, TrinetCoreApi api)
        {
            InitializeComponent();
            Configuration = config;
            Api = api;
            Initialise();
        }

        private async void Initialise()
        {
            LoadingLayout.IsVisible = true;
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
    }

}
