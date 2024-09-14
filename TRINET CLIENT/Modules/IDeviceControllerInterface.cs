using TRINET_CLIENT.Enums;

namespace TRINET_CLIENT.Modules
{
    public interface IDeviceControllerInterface
    {


        public ManufacturerDevice? GetManufacturerDeviceByType(Device device, ETrinetDeviceType deviceType);
    }
}