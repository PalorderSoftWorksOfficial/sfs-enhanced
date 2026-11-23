using System;

namespace SFSEnhanced.Mod.Runtime
{
    public interface IModRuntimeLifecycle
    {
        string[] HarmonyOwners { get; }
        void OnRuntimeEnable(bool startupComplete);
        void OnRuntimeDisable();
    }
}
