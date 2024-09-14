using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using TRINET_CLIENT.Enums;

namespace TRINET_CLIENT
{

    public class Location
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }


    public class Room
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public Guid LocationId { get; set; }
        public ICollection<Device> Devices { get; set; } = new List<Device>();
        public string ImageUrl { get; set; } = "living_room.png";
    }


    public class Device
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? InternalName { get; set; }
        public string? NetworkAddress { get; set; }
        public ETrinetDeviceType DeviceType { get; set; } = ETrinetDeviceType.UNDEFINED;
        public ETrinetDeviceManufacturer DeviceManufacturer { get; set; } = ETrinetDeviceManufacturer.NONE;
        public Guid RoomId { get; set; }
    }


    public class RefreshAuth
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
    }


    public static class SecureStorageKeys
    {
        public static string JwtToken { get; } = "JwtToken";
        public static string RefreshToken { get; } = "RefreshToken";
        public static string JwtExpiration { get; } = "TokenExpiration";

        public static string LocationId { get; } = "LocationGuid";
        public static string LocationData { get; } = "Location";
    }


    public class UserLogin
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }


    public class LoginResponse
    {
        public required string JwtToken { get; set; }
        public DateTime Expiration { get; set; }
        public required string RefreshToken { get; set; }
        public required Guid LocationId { get; set; }

        public override string ToString()
        {
            return
                $"Jwt: {JwtToken}, Expiration: {Expiration}, RefreshToken: {RefreshToken}, LocationId:{LocationId}";
        }

    }


    public class DeviceRequest
    {
        public required Guid DeviceId { get; set; }
        public string Request { get; set; } = null!;
    }


    public class TrinetCoreApi
    {
        private HttpClient CoreClient = new HttpClient();
        private int RequestTimeoutSeconds = 5;
        CoreSettings CoreApiSettings;
        private bool Initialised = false;
        private ISecureStorage _SecureStorage { get; } = SecureStorage.Default;


        public TrinetCoreApi(IConfiguration config)
        {
            CoreApiSettings = config.GetRequiredSection("CoreSettings").Get<CoreSettings>();
            CoreClient.BaseAddress = new Uri(CoreApiSettings.CoreUrl);
            CoreClient.Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds);
            _ = AssertInitialised();
            return;
        }


        public async Task<bool> AssertServerConnection()
        {
            try
            {
                var result = await CoreClient.GetAsync("/status");
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                return false;
            }
            catch(Exception)
            {
                return false;
            }         
        }

        public CoreSettings GetCoreSettings()
        {
            return CoreApiSettings;
        }


        public async Task<bool> AssertInitialised()
        {
            await LoadCredentialsFromStorage();
            Initialised = true;
            return Initialised;
        }

        private async Task<string?> GetJwtTokenFromStorage()
        {
            string? j = await _SecureStorage.GetAsync(SecureStorageKeys.JwtToken);
            Trace.WriteLineIf(j != null, j) ;
            return await _SecureStorage.GetAsync(SecureStorageKeys.JwtToken);
        }


        private async Task<string?> GetRefreshTokenFromStorage()
        {
            return await _SecureStorage.GetAsync(SecureStorageKeys.RefreshToken);
        }


        private async Task<string?> GetTokenExpiryFromStorage()
        {
            return await _SecureStorage.GetAsync(SecureStorageKeys.JwtExpiration);
        }


        private async Task<string?> GetLocationFromStorage()
        {
            return await _SecureStorage.GetAsync(SecureStorageKeys.LocationId);
        }


        /**
         * Store loginResponse data in secure storage.
         */
        private async Task SetSecureStorageKeyValues(LoginResponse loginResponse)
        {
            Trace.WriteLine("Stored secure values");
            await _SecureStorage.SetAsync(SecureStorageKeys.JwtToken, loginResponse.JwtToken);
            await _SecureStorage.SetAsync(SecureStorageKeys.RefreshToken, loginResponse.RefreshToken);
            await _SecureStorage.SetAsync(SecureStorageKeys.JwtExpiration, loginResponse.Expiration.ToString());
            await _SecureStorage.SetAsync(SecureStorageKeys.LocationId, loginResponse.LocationId.ToString());
            
            return;
        }


        /**
         * Load credentials from storage and configure required properties for authenticated session.
         */
        private async Task LoadCredentialsFromStorage()
        {
            string? activeToken = await GetJwtTokenFromStorage();
            if (activeToken == null)
            {
                Trace.WriteLine("no active Jwt Token in storage");
                return;
            }

            CoreClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", activeToken);
            string? expirationString = await GetTokenExpiryFromStorage();
            if (expirationString is not null)
            {
                DateTime asDT;
                if (DateTime.TryParse(expirationString, out asDT))
                {
                    if (asDT < DateTime.UtcNow) await RefreshAuthenticationToken();
                    return;
                }
            }
            return;
        }


        /**
         * Clear the secure storage keys, Removes all auth data and keys.
         */
        private void ClearClientLogin()
        {
            Trace.WriteLine("Clearing secure storage");
            _SecureStorage.Remove(SecureStorageKeys.JwtToken);
            _SecureStorage.Remove(SecureStorageKeys.RefreshToken);
            _SecureStorage.Remove(SecureStorageKeys.JwtExpiration);
            _SecureStorage.Remove(SecureStorageKeys.LocationId);

            CoreClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "");
            return;
        }


        /**
         * Validate token authenticity with Core. Typically used on app startup to decide initial page.
         */
        public async Task<bool> ValidateToken()
        {
            try
            {
                //try refresh token
                var _ = await RefreshAuthenticationToken();

                // test protected end point
                var Response = await CoreClient.GetAsync("/user/validate");
                return Response.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        /**
         * Submit authentication credentials to Core. If successful, store and assign Jwt Token for all subsequent api calls.
         */
        public async Task<string> Login(UserLogin credentials)
        {
            try
            {
                JsonContent content = JsonContent.Create(credentials);
                var response = await CoreClient.PostAsync("/user/login", content);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string? rContent = await response.Content.ReadAsStringAsync();
                    LoginResponse? loginResponse = JsonSerializer.Deserialize<LoginResponse>(rContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                    if (loginResponse is not null)
                    {

                        await SetSecureStorageKeyValues(loginResponse);
                        await LoadCredentialsFromStorage();
                        await GetAndStoreAllLocationDataFromCore();                            
                        
                        await Shell.Current.GoToAsync(nameof(HomePage));
                        return "OK";
                    }
                }
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return "Invalid username or password";
            }
            catch (Exception ex)
            {
                return "Something went wrong." + ex.Message;
            }
            return "Internal Error: Unexpected response code.";
        }


        /**
         * Refresh an authentication token after it's expiration.
         */
        public async Task<bool> RefreshAuthenticationToken()
        {
            string? accessToken = await GetJwtTokenFromStorage();
            string? refreshToken = await GetRefreshTokenFromStorage();
            if (accessToken is null || refreshToken is null)
            {
                Console.WriteLine($"access Token: {accessToken ?? "NONE"}, refresh {refreshToken ?? "NONE"}");
                return false;
            }
            RefreshAuth rAuth = new() { AccessToken = accessToken, RefreshToken = refreshToken };
            JsonContent content = JsonContent.Create(rAuth);

            try
            {
                var response = await CoreClient.PostAsync("/user/refresh_token", content);

                // don't use AssertHttpResponseStatus here or risk infinite recursion. 

                // token refreshed
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    LoginResponse? loginResponse = (LoginResponse?)await GetObjectFromResponse_Nullable<LoginResponse>(response, GetDefaultJsonSerializerOptions());

                    if (loginResponse is not null)
                    {
                        await SetSecureStorageKeyValues(loginResponse);
                        await LoadCredentialsFromStorage();
                        return true;
                    }
                   
                }

                // Authenticated, but token not refreshed.
                if (response.StatusCode == HttpStatusCode.Accepted)
                {
                    return true;
                }


                // Unauthorised
                ClearClientLogin();
                await Shell.Current.GoToAsync(nameof(Login));
                return false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Failed to deserialize Token " + ex.Message);
                return false;
            }
        }


        /**
         * Synchronise list of available rooms at the given location
         */
        public async Task<Location> GetRoomsFromCurrentLocation()
        {
            var locationId = await GetLocationFromStorage();


            if (locationId is null)
            {
                Trace.WriteLine("No location active.");
                ClearClientLogin();
                await Shell.Current.GoToAsync(nameof(Login));
                return new Location();
            }
            try
            {
                Trace.WriteLine(locationId);
                Trace.WriteLine("Getting rooms.");
                var response = await CoreClient.GetAsync($"/locations/{locationId}/rooms");

                if(response.StatusCode == HttpStatusCode.OK) { }

                if(await AssertHttpResponseStatus(response))
                {
                    var outLocation = await GetObjectFromResponse<Location>(response, GetDefaultJsonSerializerOptions());
                    return outLocation;
                }


                return new Location();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception - GetRoomsFromCurrentLocation: " + ex.Message);
                return new Location();
            }

        }

        public async Task<bool> GetAndStoreAllLocationDataFromCore()
        {
            var locationId = await GetLocationFromStorage();
            try
            {
                var response = await CoreClient.GetAsync($"/locations/{locationId}/sync");
                Trace.WriteLine("downloading location data");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    var location = JsonSerializer.Deserialize<Location>(data, new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    
                    if (location is null)
                    {
                        Trace.WriteLine("Error trying to deserialize Location data");
                        return false;
                    }
                    Trace.WriteLine("downloaded and stored location data");
                    await _SecureStorage.SetAsync(SecureStorageKeys.LocationData, data);
                    return true;
                }
                Trace.WriteLine("failed");
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                return false;
            }

            return false;
        }


        private void StoreOfflineDeviceData(ICollection<Device> device) 
        {

        }


        public async Task<bool> AddRoom(string name)
        {
            string? location = await GetLocationFromStorage();
            if (location is null) return false;

            Room room = new() { Id = Guid.Empty, LocationId = Guid.Parse(location), Name = name };
            JsonContent asJson = JsonContent.Create(room);
            string? asString = await asJson.ReadAsStringAsync();
            var response = await CoreClient.PostAsync("/rooms", asJson);

            return await AssertHttpResponseStatus(response);
        }

        public async Task<bool> DeleteRoom(Room room)
        {
            var response = await CoreClient.DeleteAsync($"rooms/{room.Id}");
            return response.StatusCode == HttpStatusCode.OK;
        }

        /*Synchronise list of available devices at the given location */
        public async Task<ICollection<Device>> GetDevicesFromRoom(Room room)
        {
            var location = await GetLocationFromStorage();
            var response = await CoreClient.GetAsync($"/locations/{location}/rooms/{room.Id}/devices");

            await AssertHttpResponseStatus(response);
            var outRoom = await GetObjectFromResponse<Room>(response, GetDefaultJsonSerializerOptions());
            return outRoom.Devices;

        }

        public async Task<Device> ValidateDevice(Device device)
        {
            JsonContent asJson = JsonContent.Create(device);
            var response = await CoreClient.PostAsync("devices/validate", asJson);

            await AssertHttpResponseStatus(response);

            return await GetObjectFromResponse<Device>(response, GetDefaultJsonSerializerOptions());
        }


        public async Task<Device?> AddDevice(Device device)
        {   
            JsonContent asJson = JsonContent.Create(device);
            var response = await CoreClient.PostAsync("/devices", asJson);
            await AssertHttpResponseStatus(response);

            return (Device?) await GetObjectFromResponse_Nullable<Device>(response, new JsonSerializerOptions());

        }


        private async Task<bool> AssertHttpResponseStatus(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Trace.WriteLine("Failed assertion");
                bool valid = await ValidateToken();
                if (valid) return await AssertHttpResponseStatus(response);
            }

            return false;
        }

        private async Task<T> GetObjectFromResponse<T>(HttpResponseMessage response, JsonSerializerOptions? options) where T : new()
        {
            var rAsString = await response.Content.ReadAsStringAsync();
            if (rAsString is null) return new T();

            var outObject = JsonSerializer.Deserialize<T>(rAsString, options ?? new JsonSerializerOptions());
            return outObject ?? new T();
        }

        private async Task<object?> GetObjectFromResponse_Nullable<T>(HttpResponseMessage response, JsonSerializerOptions? options)
        {
            var rAsString = await response.Content.ReadAsStringAsync();
            if (rAsString is null) return null;

            var outObject = JsonSerializer.Deserialize<T>(rAsString, options ?? new JsonSerializerOptions());
            return outObject;
        }

        private JsonSerializerOptions GetDefaultJsonSerializerOptions()
        {
            return new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        public async Task<string?> SendDeviceCommand(string endPoint, DeviceRequest request)
        {
            try
            {
                var data = JsonSerializer.Serialize(request, new JsonSerializerOptions()); //{ Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });  ;

                JsonContent content = JsonContent.Create(request);
                var asString = await content.ReadAsStringAsync();

                var response = await CoreClient.PostAsync(endPoint, content);
                var rContent = await response.Content.ReadAsStreamAsync();
                using var streamReader = new StreamReader(rContent);
                return streamReader.ReadToEnd();
            }
            catch(Exception e)
            {
                Trace.WriteLine($"Failed to send device command. {e.Message}");
                return null;
            }
        }

        private string ClearDoubleQuotes(string jsonString)
        {
            return jsonString.Replace("\\u0022", "");
        }
    }
}
