namespace TRINET_CLIENT;

public partial class HomePage : ContentPage
{
    private TrinetCoreApi Api;
    private IEnumerable<Room> Rooms = [];
    public HomePage(TrinetCoreApi api)
    {
        Api = api;
        InitializeComponent();
        RefreshRoomList();
        RoomList.ItemsSource = Rooms;
    }


    public async void RefreshRoomList()
    {
        RoomList.ItemsSource = null;
        var result = await Api.GetRoomsFromCurrentLocation();

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

    private void CollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }
}