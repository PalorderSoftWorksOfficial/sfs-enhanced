# Build Fixes

The Windows development node `Cluster-0` exposed two client-side build regressions that were not covered by the dedicated-server build:

- `NetServer` referenced the old `CreateAccount()` tuple field `token` instead of `plainToken`.
- `MultiBuildManager` referenced `BuildSnapshot` and `BuildKind` under `SFSEnhanced.Shared.Models`, but those protocol models live under `SFSEnhanced.Shared.Protocol`.
- The net472 mod project uses `System.Net.Http.HttpClient` and must explicitly reference `System.Net.Http`.

These are compile-time compatibility fixes and are required before testing the mod binary on the Windows host.
