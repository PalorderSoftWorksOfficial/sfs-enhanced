# Secure Steam Authentication

SFS Enhanced currently uses Steam identity information as an account binding signal. That must not be treated as proof of ownership.

The production authentication flow should be:

1. SFS obtains a Steam authentication ticket using Steamworks.
2. The client sends the ticket to the SFS Enhanced authentication service over TLS.
3. The authentication service verifies the ticket with Steam and extracts the canonical SteamID.
4. The service issues a short-lived SFS Enhanced session credential.
5. Dedicated servers validate that credential and use the verified SteamID for account lookup.
6. Servers must reject a client-provided SteamID that does not match the verified identity.

The Steam Web API key must remain only on the trusted authentication service and must never be included in the mod or dedicated-server client configuration.

Offline or non-Steam development builds should remain possible through an explicit development authentication mode that is disabled by default.
