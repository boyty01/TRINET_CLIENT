
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace TRINET_CLIENT.Modules.Wiz
{

    public enum EWizBulbRequestMethod
    {
        setPilot,
        getPilot,
        setState
    }


    // empty params
    public class WizBulbRequestParamsBase { }


    // set pilot params
    public class SetPilotParams : WizBulbRequestParamsBase
    {
        public bool? State { get; set; }
        public int? SceneId { get; set; }
        public int? R { get; set; }
        public int? G { get; set; }
        public int? B { get; set; }
        public int? C { get; set; }
        public int? W { get; set; }
        public int? Temp { get; set; }
        public int? Dimming { get; set; }
    }


    // set state params
    public class SetStateParams : WizBulbRequestParamsBase
    {
        public bool State { get; set; }
    }


    // container for request body from Trinet client
    public class WizRequestBase
    {
        public EWizBulbRequestMethod Method { get; set; }
        public string Request { get; set; } = null!;
    }


    // set pilot request 
    public class SetPilotRequest
    {
        public int? Id { get; set; }
        public string Method { get; set; } = EWizBulbRequestMethod.setPilot.ToString();
        public SetPilotParams? Params { get; set; }

    }


    // prepared get pilot request. 
    public class GetPilotRequest
    {
        public string Method { get; set; } = EWizBulbRequestMethod.getPilot.ToString();
        public WizBulbRequestParamsBase Params { get; set; } = new WizBulbRequestParamsBase();

    }


    // get pilot response 
    public class GetPilotResponse
    {
        public string? Method { get; set; }
        public string? Env { get; set; }
        public GetPilotResponseResult? Result { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this).ToString();
        }
    }

    // get pilot response Result
    public class GetPilotResponseResult
    {
        public string? Mac { get; set; }
        public int? Rssi { get; set; }
        public bool? State { get; set; }
        public int? SceneId { get; set; }
        public int? R { get; set; }
        public int? G { get; set; }
        public int? B { get; set; }
        public int? C { get; set; }
        public int? W { get; set; }
        public int? Temp { get; set; }
        public int? Dimming { get; set; }
    }

    public class WizBulb : ManufacturerDevice, ILightBulbInterface
    {

        public WizBulb(Guid deviceId, string networkAddress, int port, string name = "")
        {
            DeviceId = deviceId;
            NetworkAddress = networkAddress;
            Port = port;
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            if (obj is WizBulb)
            {
                return this == obj;
            }

            if (obj is Device)
            {
                return this == (Device)obj;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(WizBulb bulb, Device device)
        {
            return bulb.NetworkAddress == device.NetworkAddress;
        }

        public static bool operator !=(WizBulb bulb, Device device)
        {
            return !(bulb.NetworkAddress == device.NetworkAddress);
        }


        /**
         * Handle sending all requests
         */
        public async override Task<string?> HandleRequest(string request)
        {
            Trace.WriteLine($"Handling request {request}");
            var obj = JsonSerializer.Deserialize<WizRequestBase>(request); //, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            if (obj is null) return "{\"error\":\"Bad request. Bad Payload.\"}";
            
            // Get state
            if (obj.Method == EWizBulbRequestMethod.getPilot)
            {
                return await GetState();
            }

            // send request command. SetPilot and SetState 
            if (obj.Method == EWizBulbRequestMethod.setPilot || obj.Method == EWizBulbRequestMethod.setState)
            {
                var jsonRequest = JsonSerializer.Deserialize<SetPilotRequest>(obj.Request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
               
                if (jsonRequest is null) return "{\"error\":\"Bad request. Bad Payload. \"}";
                jsonRequest.Method = obj.Method == EWizBulbRequestMethod.setPilot ? "setPilot" : "setState";
              
                return await SendRequest(jsonRequest);
            }
         
            return "{\"error\":\"Bad request. Unknown method.\"}";
        }


        public SharedLightBulbValues? ToLightBulbValues(string data)
        {
            string? d = JsonSerializer.Deserialize<string>(data);      
            if (d is null) return null;

            try
            {
                GetPilotResponse? deserialized = JsonSerializer.Deserialize<GetPilotResponse>(d, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

                if (deserialized is not null && deserialized.Result is not null)
            {
                    if(deserialized.Result.State == true)
                    {
                        Trace.WriteLine("Light is on");
                    }

                    if (deserialized.Result.State == false)
                    {
                        Trace.WriteLine("Light is off");
                    }



                    Trace.WriteLine(JsonSerializer.Serialize(deserialized));
                    return new SharedLightBulbValues()
                    {
                        R = deserialized.Result.R ?? 0,
                        G = deserialized.Result.G ?? 0,
                        B = deserialized.Result.B ?? 0,
                        A = deserialized.Result.Dimming / 100,
                        State = deserialized.Result.State == true
                };
            }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
            return null;
        }


        /** 
         * send a getPilot command.
         */
        private async Task<string?> GetState()
        {
            //Send command to core if configured to do so.
            WizModule? module = IPlatformApplication.Current?.Services.GetService<WizModule>();
            if (module is not null && module.GetUseCoreForAllCommands())
            {
                DeviceRequest dR = new DeviceRequest() { DeviceId = DeviceId, Request = JsonSerializer.Serialize(new GetPilotRequest()) };
                return await SendDeviceRequestFromCore(dR);
            }

            // send command directly to the bulb.
            var command = new GetPilotRequest();
            var cConv = JsonSerializer.Serialize(command, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            byte[] cBytes = System.Text.Encoding.UTF8.GetBytes(cConv);
            try
            {
                UdpClient client = new UdpClient();
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(NetworkAddress), Port);
                await client.SendAsync(cBytes, ep);

                var buffer = await client.ReceiveAsync();
                var asString = System.Text.Encoding.UTF8.GetString(buffer.Buffer, 0, buffer.Buffer.Length);
                client.Close();
                return asString;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                return null;
            }

        }


        /**
         * Send a set state / pilot command.
         */
        private async Task<string?> SendRequest(SetPilotRequest request)
        {
            // Send to core if configured to do so.
            WizModule? module = IPlatformApplication.Current?.Services.GetService<WizModule>();
            if (module is not null && module.GetUseCoreForAllCommands())
            {
                DeviceRequest dR = new DeviceRequest() { DeviceId = DeviceId, Request = JsonSerializer.Serialize(request, new JsonSerializerOptions() {PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }) };
                return await SendDeviceRequestFromCore(dR);
            }

            try
            {
                var data = JsonSerializer.Serialize(request, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull});

            if (data == null)
                {
                    Trace.WriteLine("Failed to serialize request");
                    return "";
                }
                byte[] cBytes = System.Text.Encoding.UTF8.GetBytes(data);

           
                UdpClient client = new UdpClient(NetworkAddress, Port);
                client.Client.ReceiveTimeout = 200;
                await client.SendAsync(cBytes);
                client.Close();
                return "OK";
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                return "Error";
            }
        }

        private async Task<string?> SendDeviceRequestFromCore(DeviceRequest request)
        {
            TrinetCoreApi? api = IPlatformApplication.Current?.Services.GetService<TrinetCoreApi>();

            if (api is null)
            {
                Trace.WriteLine("Failed to get api");
                return null;
            }
           
            return await api.SendDeviceCommand("/wiz/command", request);
        }

        public string GetRGBRequestCommand(byte r, byte g, byte b)
        {
            var request = JsonSerializer.Serialize(new SetPilotRequest() { Params = new SetPilotParams() { R = r, G = g, B = b } }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull});
            var wrapper = new WizRequestBase() { Method = EWizBulbRequestMethod.setPilot, Request =  request };
            return JsonSerializer.Serialize(wrapper);
        }

        public string GetRGBARequestCommand(byte r, byte g, byte b, double a)
        {

            var request = JsonSerializer.Serialize(new SetPilotRequest() { Id=1, Params = new SetPilotParams() { R = r, G = g, B = b, Dimming = Math.Clamp((int)Math.Round(a * 100), 5, 100) } }, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull});
            var wrapper = new WizRequestBase() { Method = EWizBulbRequestMethod.setPilot, Request = request };
            return JsonSerializer.Serialize(wrapper);
        }

        public string GetBrightnessCommand(double brightness)
        {
            var request = JsonSerializer.Serialize(new SetPilotRequest()
            {
                Id=1,
                Params = new SetPilotParams()
                {
                    Dimming = Math.Clamp( (int) Math.Round(brightness * 100), 5, 100) 
                }
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });

            var wrapper = new WizRequestBase() { Method = EWizBulbRequestMethod.setPilot, Request = request };
            return JsonSerializer.Serialize(wrapper);
        }

        public string GetPowerToggleCommand(bool state)
        {
            SetPilotRequest request = new SetPilotRequest {Method="setState", Params = new SetPilotParams() { State = state } };
            var wrapper = new WizRequestBase() { Method = EWizBulbRequestMethod.setState, Request = JsonSerializer.Serialize(request, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull}) };
            return JsonSerializer.Serialize(wrapper);
        }

        public string GetStatusRequest()
        {
            var request = JsonSerializer.Serialize(new GetPilotRequest());
            var wrapper = new WizRequestBase() { Method = EWizBulbRequestMethod.getPilot, Request = JsonSerializer.Serialize(request) };
            return JsonSerializer.Serialize(wrapper); 
        }
    }

}
