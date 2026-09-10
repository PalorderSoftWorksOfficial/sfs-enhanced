using UnityEngine;

namespace SFSEnhanced.Mod
{
    public static class ModSettings
    {
        private const string HostKey = "sfs_enhanced.host";
        private const string PortKey = "sfs_enhanced.port";
        private const string NameKey = "sfs_enhanced.name";
        private const string TokenKey = "sfs_enhanced.token";
        private const string DirectoryKey = "sfs_enhanced.directory";
        private const string ServerExecutableKey = "sfs_enhanced.server_executable";

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

        public static string DirectoryUrl
        {
            get => PlayerPrefs.GetString(DirectoryKey, "");
            set { PlayerPrefs.SetString(DirectoryKey, value ?? ""); PlayerPrefs.Save(); }
        }

        public static string ServerExecutablePath
        {
            get => PlayerPrefs.GetString(ServerExecutableKey, "");
            set { PlayerPrefs.SetString(ServerExecutableKey, value ?? ""); PlayerPrefs.Save(); }
        }
    }
}
