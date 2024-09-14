using TRINET_CLIENT.Pages;
using TRINET_CLIENT.Global;
using System.Text.Json;

namespace TRINET_CLIENT;

public partial class HomePage : ContentPage
{
    private TrinetCoreApi Api;
    private IEnumerable<Room> Rooms = [];
    private UserSettings UserSettings;
    private ISecureStorage _secureStorage = SecureStorage.Default;
    public Command SelectedTagChangedCommand
    {
        get
        {
            return new Command((sender) =>
            {
                Room room = sender as Room ?? new Room();

                Console.WriteLine("Select item in Colletionview" + room.Name);
            });
        }
    }

    public HomePage(UserSettings userSettings, TrinetCoreApi api)
    {
        Api = api;
        UserSettings = userSettings;
        InitializeComponent();
        RefreshRoomList();
        RoomList.ItemsSource = Rooms;
    }


    public async void RefreshRoomList()
    {

        // offline mode
        if (UserSettings.UseOfflineMode)
        {
            var data = await _secureStorage.GetAsync(SecureStorageKeys.LocationData);
            if (data is not null)
            {
                var location = JsonSerializer.Deserialize<Location>(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
                
                if (location is not null)
                {
                    RoomList.ItemsSource = location.Rooms;
                }
            }
            return;
        }

        // online mode 

        RoomList.ItemsSource = null;
        var result = await Api.GetRoomsFromCurrentLocation();
        await Api.GetAndStoreAllLocationDataFromCore();
        if (result is null) return;

        RoomList.ItemsSource = result.Rooms;

    }

    private async void Button_Clicked_AddRoom(object sender, EventArgs e)
    {
        string? result = await DisplayPromptAsync("New Room", "Name:");
        if (result is not null)
        {
            await Api.AddRoom(result);
            RefreshRoomList();
        }
    }


    private async void RoomList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Room? room = e.CurrentSelection.FirstOrDefault() as Room;
        if (room == null) return;
        var parameters = new Dictionary<string, object>()
        {
            { "Room", room }
        };

        await Shell.Current.GoToAsync(nameof(RoomPage), parameters);
    }

    private async void DeleteButton_Clicked(object sender, EventArgs e)
    {
    }
}