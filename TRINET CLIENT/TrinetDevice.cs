using TRINET_CLIENT.Enums;

namespace TRINET_CLIENT
{

    internal class TrinetDevice
    {
        public ETrinetDeviceType DeviceType = ETrinetDeviceType.UNDEFINED;
        public ETrinetDeviceManufacturer DeviceManufacturer = ETrinetDeviceManufacturer.NONE;
        public string? DeviceId;
        public string? NetworkAddress;

        public TrinetDevice()
            : this(ETrinetDeviceType.UNDEFINED, ETrinetDeviceManufacturer.NONE, "", "")
        { }

        public TrinetDevice(ETrinetDeviceType _DeviceType, ETrinetDeviceManufacturer _DeviceManufacturer, string _DeviceId, string _NetworkAddress)
        {
            DeviceType = _DeviceType;
            DeviceManufacturer = _DeviceManufacturer;
            DeviceId = _DeviceId;
            NetworkAddress = _NetworkAddress;
        }

        override public string ToString()
        {
            return DeviceManufacturer.ToString() + " " + DeviceType.ToString() + " : " + DeviceId;
        }
    }
}
