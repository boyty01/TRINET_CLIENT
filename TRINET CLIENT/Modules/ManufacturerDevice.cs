using TRINET_CLIENT.Enums;
using TRINET_CLIENT.Modules.Wiz;

namespace TRINET_CLIENT.Modules
{

    public class SharedLightBulbValues()
    {
        public int? R;
        public int? G;
        public int? B;
        public double? A;
        public bool State;
    }

    public abstract class ManufacturerDevice : IManufacturerDeviceInterface
    {

        protected Guid DeviceId { get; set; }
        protected string NetworkAddress = null!;
        protected int Port;
        protected string? Name;

        public static ManufacturerDevice? GetDeviceAsManufacturerDevice(Device device)
        {
            switch (device.DeviceManufacturer)
            {
                case ETrinetDeviceManufacturer.WIZ:
                    {
                        var WizModule = IPlatformApplication.Current?.Services.GetService<WizModule>();
                        if (WizModule is not null)
                        {
                            return WizModule.GetManufacturerDeviceByType(device, device.DeviceType);
                        }
                        break;
                    }
            }
            return null;
        }


        abstract public Task<string?> HandleRequest(string request);
    }
}
