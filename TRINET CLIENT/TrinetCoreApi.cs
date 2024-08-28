using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

    }


    public class TrinetCoreApi
    {

        private HttpClient CoreClient = new HttpClient();
        CoreSettings CoreApiSettings;
        private ISecureStorage _SecureStorage { get; } = SecureStorage.Default;


        public TrinetCoreApi(IConfiguration config)
        {
            CoreApiSettings = config.GetRequiredSection("CoreSettings").Get<CoreSettings>();
            CoreClient.BaseAddress = new Uri(CoreApiSettings.CoreUrl);
            _ = LoadCredentialsFromStorage();
            return;
        }


        private async Task<string?> GetJwtTokenFromStorage()
        {
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
            await _SecureStorage.SetAsync(SecureStorageKeys.JwtToken, loginResponse.JwtToken);
            await _SecureStorage.SetAsync(SecureStorageKeys.RefreshToken, loginResponse.RefreshToken);
            await _SecureStorage.SetAsync(SecureStorageKeys.JwtExpiration, loginResponse.Expiration.ToString());
            await _SecureStorage.SetAsync(SecureStorageKeys.LocationId, loginResponse.LocationId.ToString());
            Trace.WriteLine(loginResponse.LocationId.ToString());
            return;
        }


        /**
         * Load credentials from storage and configure required properties for authenticated session.
         */
        private async Task LoadCredentialsFromStorage()
        {
            string? activeToken = await GetJwtTokenFromStorage();
            if (activeToken == null) return;

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
            if (accessToken is null || refreshToken is null) return false;
            RefreshAuth rAuth = new() { AccessToken = accessToken, RefreshToken = refreshToken };
            JsonContent content = JsonContent.Create(rAuth);

            try
            {
                var response = await CoreClient.PostAsync("/user/refresh_token", content);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string? responseAsString = await response.Content.ReadAsStringAsync();
                    if (responseAsString is not null)
                    {
                        LoginResponse? loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseAsString, new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
                        if (loginResponse is not null)
                        {
                            await SetSecureStorageKeyValues(loginResponse);
                            await LoadCredentialsFromStorage();
                            return true;
                        }
                    }
                }

                // clear invalid auth data.
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
                var response = await CoreClient.GetAsync($"/locations/{locationId}/rooms");
                if (response.IsSuccessStatusCode)
                {

                    var resString = await response.Content.ReadAsStringAsync();
                    if (resString is not null)
                    {
                        Location? location = JsonSerializer.Deserialize<Location>(resString, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                        if (location is not null)
                        {
                            return location;
                        }
                    }
                }
                return new Location();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception - GetRoomsFromCurrentLocation: " + ex.Message);
                return new Location();
            }

        }


        public async Task<bool> AddRoom(string name)
        {
            string? location = await GetLocationFromStorage();
            if (location is null) return false;

            Room room = new() { Id = Guid.Empty, LocationId = Guid.Parse(location), Name = name };
            JsonContent asJson = JsonContent.Create(room);
            string? asString = await asJson.ReadAsStringAsync();
            Trace.WriteLine(asString);
            var response = await CoreClient.PostAsync("/rooms", asJson);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            return false;
        }


        /*Synchronise list of available devices at the given location */
        public static Dictionary<int, string> GetDevicesFromRoom(int LocationId, int RoomId)
        {
            throw new NotImplementedException();
        }


    }
}
