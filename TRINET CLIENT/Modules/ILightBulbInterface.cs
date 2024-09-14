namespace TRINET_CLIENT.Modules
{

  
    internal interface ILightBulbInterface
    {
        public string GetRGBRequestCommand(byte r, byte g, byte b);

        public string GetRGBARequestCommand(byte r, byte g, byte b, double a);

        public string GetBrightnessCommand(double brightness);

        public string GetPowerToggleCommand(bool state);

        public string GetStatusRequest();

        public SharedLightBulbValues? ToLightBulbValues(string data);

        
    }
}
