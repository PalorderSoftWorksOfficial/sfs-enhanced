using SFSEnhanced.Shared.Networking;
using UnityEngine;

namespace SFSEnhanced.Mod.Networking
{
    internal sealed class PlayerPrefsEndpointStorage : IEndpointStorage
    {
        public string Read(string key) => PlayerPrefs.GetString(key, string.Empty);

        public bool Write(string key, string value)
        {
            PlayerPrefs.SetString(key, value ?? string.Empty);
            PlayerPrefs.Save();
            return true;
        }

        public void Remove(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
