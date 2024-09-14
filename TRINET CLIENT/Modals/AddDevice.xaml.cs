
/* Unmerged change from project 'TRINET CLIENT (net8.0-windows10.0.19041.0)'
Before:
using TRINET_CLIENT.Enums;
After:
using Mopups.PreBaked.AbstractClasses;
*/

/* Unmerged change from project 'TRINET CLIENT (net8.0-maccatalyst)'
Before:
using TRINET_CLIENT.Enums;
After:
using Mopups.PreBaked.AbstractClasses;
*/

/* Unmerged change from project 'TRINET CLIENT (net8.0-android)'
Before:
using TRINET_CLIENT.Enums;
After:
using Mopups.PreBaked.AbstractClasses;
*/
using Mopups.Services;
using TRINET_CLIENT.Enums;
namespace TRINET_CLIENT.Modals;

public class AddDeviceParams
{
    public bool IsValid = false;
    public string Name { get; set; }
    public ETrinetDeviceManufacturer DeviceManufacturer { get; set; }
    public ETrinetDeviceType DeviceType { get; set; }
    public string NetworkAddress { get; set; }

    public AddDeviceParams()
    {
        Name = "";
        NetworkAddress = "";
    }
}

public partial class AddDevice : Mopups.Pages.PopupPage
{

    public ICollection<string> ManufacturerList { get; set; } = Enum.GetNames(typeof(ETrinetDeviceManufacturer));
    public ICollection<string> DeviceList { get; set; } = Enum.GetNames(typeof(ETrinetDeviceType));

    TaskCompletionSource<AddDeviceParams> PackedData { get; set; } = null!;
    public Task<AddDeviceParams> PopupDismissedTask => PackedData.Task;
    private bool ConfirmedAdd  = false;

    public AddDevice()
    {
        InitializeComponent();
        BindingContext = this;

    }

    private void AddButton_Clicked(object sender, EventArgs e)
    {
        if (ValidateInput())
        {
            ConfirmedAdd = true;
            MopupService.Instance.PopAsync(false);
        }
           
    }

    private bool ValidateInput()
    {
        return (DeviceName is not null &&
            ManufacturerName.SelectedIndex >= 0 &&
            DeviceType.SelectedIndex >= 0 &&
            NetworkAddress.Text is not null);
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();
        PackedData = new TaskCompletionSource<AddDeviceParams>();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (ConfirmedAdd)
        {
            PackedData.SetResult(
                        new AddDeviceParams()
                        {
                            IsValid = true,
                            Name = DeviceName.Text,
                            DeviceManufacturer = (ETrinetDeviceManufacturer)ManufacturerName.SelectedIndex,
                            DeviceType = (ETrinetDeviceType)DeviceType.SelectedIndex,
                            NetworkAddress = NetworkAddress.Text
                        });
            return;
        }               
        PackedData.SetResult(new AddDeviceParams());
        return;

    }

    private void CancelButton_Clicked(object sender, EventArgs e)
    {
        ConfirmedAdd = false;
        MopupService.Instance.PopAsync(false);
    }
}