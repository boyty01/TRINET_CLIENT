using Mopups.Services;
using System.ComponentModel;
using System.Diagnostics;
using TRINET_CLIENT.Enums;
using TRINET_CLIENT.Modals;
using TRINET_CLIENT.Global;
using System.Text.Json;

namespace TRINET_CLIENT.Pages;

public partial class RoomPage : ContentPage, IQueryAttributable, INotifyPropertyChanged
{
    private Room Room { get; set; } = new();

    private TrinetCoreApi Api { get; set; }

    private ICollection<Device> Devices { get; set; } = [];
    private UserSettings UserSettings;
    private ISecureStorage _secureStorage = SecureStorage.Default;
    public RoomPage(UserSettings userSettings, TrinetCoreApi api)
    {
        InitializeComponent();
        Api = api;
        UserSettings = userSettings;
        DeviceList.ItemsSource = Devices;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Room = query["Room"] as Room ?? new Room();
        OnPropertyChanged("Room");
        SetupPage();
    }


    private async void SetupPage()
    {
        DeviceList.ItemsSource = new List<Device>();
        RoomName.Text = Room.Name ?? "UNDEFINED ROOM";
        Devices = await GetDevices();
        Trace.WriteLine(Devices.Count + "Devices found");
        DeviceList.ItemsSource = Devices;
    }

    private async Task<ICollection<Device>> GetDevices()
    {

        // Offline mode
        if (UserSettings.UseOfflineMode)
        {
           var data = await _secureStorage.GetAsync(SecureStorageKeys.LocationData);
            if(data is not null)
            {
                var asJson = JsonSerializer.Deserialize<Location>(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                if(asJson is not null)
                {
                    var room = asJson.Rooms.Where(r => r.Id == Room.Id).FirstOrDefault();
                    if (room is not null)
                    {
                        return room.Devices;
                    }
                }
            } 
            return [];
        }

        // online
        return await Api.GetDevicesFromRoom(Room);
    }

    private async void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Device? device = e.CurrentSelection.FirstOrDefault() as Device;

        if (device is not null)
        {
            var parameters = new Dictionary<string, object>() { { "Device", device }, { "Room", Room } };
            switch (device.DeviceType)
            {

                case (ETrinetDeviceType.LIGHT_BULB):
                    {
                        await Shell.Current.GoToAsync(nameof(DeviceLight), parameters);
                        break;
                    }
                default:
                    break;

            }
        }
    }

    private async void AddButton_Clicked(object sender, EventArgs e)
    {
        var addDevicePopup = new AddDevice();
        await MopupService.Instance.PushAsync(addDevicePopup, false);

        var rValue = await addDevicePopup.PopupDismissedTask;

        if (!rValue.IsValid) return;

        var newDevice = new Device()
        {
            Id = Guid.Empty,
            Name = rValue.Name,
            DeviceManufacturer = rValue.DeviceManufacturer,
            DeviceType = rValue.DeviceType,
            NetworkAddress = rValue.NetworkAddress,
            RoomId = Room.Id
        };

        var addedDevice = await Api.AddDevice(newDevice);
        SetupPage();
    }

    private async void DeleteButton_Clicked(object sender, EventArgs e)
    {
        var confirmed = await DisplayAlert("Confirm Delete", $"Really delete {Room.Name}?", "Yes", "No");

        if (confirmed)
        {
            var deleted = await Api.DeleteRoom(Room);
            if (!deleted)
            {
                await DisplayAlert("Error", "Failed to delete room", "Ok");
                return;
            }

            await Shell.Current.GoToAsync(nameof(HomePage));
        }
    }

    private async void Back_Location_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(HomePage), false);
    }
}