using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRINET_CLIENT.Global
{
    public class UserSettings
    {

        public bool UseOfflineMode { get; private set; } = false;
        private ISecureStorage _secureStorage { get; } = SecureStorage.Default;
       

        public UserSettings()
        {
            LoadSettingsFromStorage();
        }


        private async void LoadSettingsFromStorage()
        {
            var useOfflineMode = await _secureStorage.GetAsync("OfflineMode");            
            UseOfflineMode = useOfflineMode != null && useOfflineMode.Equals("true");            
        }


        public void SetOfflineMode(bool state)
        {
            UseOfflineMode = state;
            SaveSettingsToStorage();
        }


        private async Task SaveOfflineMode()
        {
            await _secureStorage.SetAsync("OfflineMode", UseOfflineMode.ToString());
        }
        

        private async void SaveSettingsToStorage()
        {
            await SaveOfflineMode();
        }
    }
}
