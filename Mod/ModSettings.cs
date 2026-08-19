using UnityEngine;

namespace SFSEnhanced.Mod
{
    /// <summary>Persists connection identity across game launches via PlayerPrefs.</summary>
    public static class ModSettings
    {
        private const string HostKey = "sfs_enhanced.host";
        private const string PortKey = "sfs_enhanced.port";
        private const string NameKey = "sfs_enhanced.name";
        private const string TokenKey = "sfs_enhanced.token";

        public static string Host
        {
            get => PlayerPrefs.GetString(HostKey, "127.0.0.1");
            set { PlayerPrefs.SetString(HostKey, value ?? "127.0.0.1"); PlayerPrefs.Save(); }
        }

        public static int Port
        {
            get => PlayerPrefs.GetInt(PortKey, 7777);
            set { PlayerPrefs.SetInt(PortKey, value); PlayerPrefs.Save(); }
        }

        public static string PlayerName
        {
            get => PlayerPrefs.GetString(NameKey, "Pilot");
            set { PlayerPrefs.SetString(NameKey, value ?? "Pilot"); PlayerPrefs.Save(); }
        }

        public static string AuthToken
        {
            get => PlayerPrefs.GetString(TokenKey, "");
            set { PlayerPrefs.SetString(TokenKey, value ?? ""); PlayerPrefs.Save(); }
        }
    }
}
