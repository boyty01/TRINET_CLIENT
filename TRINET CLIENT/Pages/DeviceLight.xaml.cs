using System.ComponentModel;
using System.Diagnostics;
using TRINET_CLIENT.Modules;

namespace TRINET_CLIENT.Pages;

public partial class DeviceLight : ContentPage, IQueryAttributable, INotifyPropertyChanged
{

    private Device _Device = null!;
    private Room _Room = null!;
    public bool IsTurnedOn { get; set; } = true;
    private Color CurrentColour = new Color();
    private double CurrentBrightness = 0;
    private bool DataSynchronising = true;

    public DeviceLight()
    {
        InitializeComponent();
        Task.Delay(1000).ContinueWith(t => Setup());
    }


    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _Device = query["Device"] as Device;
      

        _Room = query["Room"] as Room;
    }


    public async void Setup()
    {
        DataSynchronising = true;
        var r = await GetLightState();
        if (r is not null)
        {
            SetCurrentState(r);
       //     SyncInterfaceControls();
        }

        DataSynchronising = false;
    }


    private async Task<SharedLightBulbValues?> GetLightState()
    {
        try
        {

        var mDevice = GetAsManufacturerDevice();
        var asInterface = GetManufacturerDeviceAsLightInterface();

        if (asInterface is not null && mDevice is not null)
        {
            var command = asInterface.GetStatusRequest();
            Trace.WriteLine(command);
            var data = await mDevice.HandleRequest(command);

            if (data is not null)
            {
                return asInterface.ToLightBulbValues(data);
            }
        }

        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
            return null;
        }
        return null;
    }


    private void SetCurrentState(SharedLightBulbValues values)
    {
        DataSynchronising = true;
        CurrentColour = new Color(values.R ?? 0, values.G ?? 0, values.B ?? 0);
        CurrentBrightness = values.A ?? 0;
        IsTurnedOn = values.State;
        DataSynchronising = false;
    }


    private void SyncInterfaceControls()
    {
        DataSynchronising = true;
        BrightnessSlider.Value = CurrentBrightness;
     //   PowerSwitch.IsToggled = IsTurnedOn; 
        DataSynchronising = false;
    }


    private ManufacturerDevice? GetAsManufacturerDevice()
    {
        if (_Device is not null) return ManufacturerDevice.GetDeviceAsManufacturerDevice(_Device);

        return null;
    }


    private ILightBulbInterface? GetManufacturerDeviceAsLightInterface()
    {
        ILightBulbInterface? asInterface = null;
        if (_Device is not null)
        {
            ManufacturerDevice? device = ManufacturerDevice.GetDeviceAsManufacturerDevice(_Device);
            asInterface = device as ILightBulbInterface;
        }
        return asInterface;
    }


    private void ColorPicker_PickedColorChanged(object sender, ColorPicker.Maui.PickedColorChangedEventArgs e)
    {
        if (DataSynchronising) return;

        byte r; byte g; byte b;
        ColorPicker.PickedColor.ToRgb(out r, out g, out b);
        CurrentColour = new Color(r, g, b);
        if (_Device is not null)
        {
            ManufacturerDevice? device = GetAsManufacturerDevice();
            ILightBulbInterface? asInterface = device as ILightBulbInterface;

            if (device is not null && asInterface is not null)
            {
                var command = asInterface.GetRGBRequestCommand(r, g, b);
                device.HandleRequest(command);
            }
        }
    }

    private void BrightnessSlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (DataSynchronising)
        {
            Trace.WriteLine("data is synchronising");
            return;
        }
 

        ManufacturerDevice? device = GetAsManufacturerDevice();
        var asInterface = device as ILightBulbInterface;

        CurrentBrightness = e.NewValue;

        if(device is not null && asInterface is not null) 
        {
            byte r; byte g; byte b;
            CurrentColour.ToRgb(out r, out g, out b);
            var command = asInterface.GetBrightnessCommand(CurrentBrightness);
            device.HandleRequest(command);
        }
    }

    private void PowerSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        if (DataSynchronising) return;
        var device = GetAsManufacturerDevice();
        var asInterface = GetManufacturerDeviceAsLightInterface();
        if(device is not null && asInterface is not null)
        {
            var command = asInterface.GetPowerToggleCommand(e.Value);
   
            device.HandleRequest(command);
        }
    }

    private async void Back_Room_Clicked(object sender, EventArgs e)
    {
        var parameters = new Dictionary<string, object>() { { "Room", _Room } };
        await Shell.Current.GoToAsync(nameof(RoomPage), parameters);
    }
    
    private async void Back_Location_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(HomePage), false);
    }
}