namespace TRINET_CLIENT;

public partial class Login : ContentPage
{


    public TrinetCoreApi Api { get; set; }

    public Login(TrinetCoreApi api)
    {
        InitializeComponent();

        Api = api;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (UsernameField.Text is null || PasswordField.Text is null)
        {
            ErrorText.Text = "Please enter a valid username and password.";
            return;
        }

        UserLogin userLogin = new UserLogin { Username = UsernameField.Text, Password = PasswordField.Text };
        var response = await Api.Login(userLogin);
        ErrorText.Text = response;
    }
}