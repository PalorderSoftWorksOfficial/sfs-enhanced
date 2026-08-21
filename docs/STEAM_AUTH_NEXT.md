# Steam Authentication Implementation Plan

The current SteamID binding must not be treated as secure proof of ownership.

The next implementation should add a Steamworks-backed ticket flow on the client and a separate authentication service.

The client should request `GetAuthTicketForWebApi`, wait for `GetTicketForWebApiResponse_t`, send the resulting ticket to the authentication service, and receive a short-lived session credential.

The authentication service should call Steam's `AuthenticateUserTicket` endpoint over HTTPS, keep its Steam Web API key private, and return the verified 64-bit SteamID to the SFS Enhanced server layer.

The dedicated server should accept a verified session credential rather than trusting a client-provided SteamID. Development-only offline authentication should remain explicit and disabled by default.

The service identity used when requesting the Steam ticket should be stable and documented so Steam can validate that the ticket was created for SFS Enhanced.
