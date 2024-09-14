using TRINET_CLIENT.Enums;

namespace TRINET_CLIENT.Modules;

public enum ERequestStatus
{
    UNDEFINED,
    FAILED,
    PENDING,
    SENT
}

public abstract class ModuleBase : IDeviceControllerInterface
{

    public abstract ManufacturerDevice? GetManufacturerDeviceByType(Device device, ETrinetDeviceType deviceType);

}
