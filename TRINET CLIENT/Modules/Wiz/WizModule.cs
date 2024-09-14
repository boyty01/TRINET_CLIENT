using TRINET_CLIENT.Enums;
using TRINET_CLIENT.Global;

namespace TRINET_CLIENT.Modules.Wiz

{


    public class WizModule(UserSettings userSettings) : ModuleBase
    {

        public int WizDefaultPort = 38899;
        private bool UseCoreForAllCommands { get; set; } = true;
        private UserSettings UserSettings { get; set; } = userSettings;


        /**
        * Wiz Devices
        */
        public override ManufacturerDevice? GetManufacturerDeviceByType(Device device, ETrinetDeviceType deviceType)
        {
            switch (deviceType)
            {
                case ETrinetDeviceType.LIGHT_BULB:
                    {
                        return new WizBulb(device.Id, device.NetworkAddress ?? "", WizDefaultPort, device.Name ?? "");
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        public void SetUseCoreForAllCommands(bool useCoreForAllCommands)
        {
            UseCoreForAllCommands = useCoreForAllCommands;
        }

        public bool GetUseCoreForAllCommands()
        {
            return UseCoreForAllCommands && !UserSettings.UseOfflineMode;
        }

    }
}
