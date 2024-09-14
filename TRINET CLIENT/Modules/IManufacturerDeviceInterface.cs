namespace TRINET_CLIENT.Modules
{
    internal interface IManufacturerDeviceInterface
    {

        public Task<string?> HandleRequest(string request);
    }
}
