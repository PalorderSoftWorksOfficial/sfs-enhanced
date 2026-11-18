using SFSEnhanced.Mod;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Mod.UI.App
{
    internal interface IAppActions
    {
        AppState State { get; }
        ModMain Mod { get; }

        void Navigate(string pageId);
        bool Back();

        void Connect(string host, string portText);
        void CreateAccount(string host, string portText, string registrationPassword);
        void RetryWithPassword(string registrationPassword);
        void Disconnect();
        void HostLocalServer();

        void TrustServer();
        void ReplaceServerIdentity();
        void CancelIdentityPrompt();
        void ForgetTrustedServer(string endpointKey);

        void RefreshServers();
        void RefreshRooms();
        void CreateRoom(string name, string description, bool isPublic, string capacityText);
        void JoinRoom(string roomId, string roomName);
        void JoinWorld(string worldId, string worldName);
        void LeaveRoom();
        void CloseRoom();
        void LeaveWorld();

        void SendChat(string message);
        void ClearChat();

        void ClaimToggle();
        void RequestTimewarp(double multiplier);
        void AnswerVote(bool approved);

        void RefreshFriends();
        void AddFriend(string playerName);
        void RespondFriend(string playerId, bool accept);
        void InviteFriend(string playerName);

        void SaveProfile(ProfileUpdatePacket update);
        void AcceptPendingInvite();

        void SetStatus(string text, bool isError = false);
        void SetPlayerName(string playerName);
    }
}
