using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SFSEnhanced.Shared.Models;

namespace SFSEnhanced.Server.Persistence
{
    /// <summary>
    /// Very lightweight account handling: a player picks a name, gets a random
    /// auth token back, and reconnects with that token next time. This is enough
    /// for "friends and persistent identity across sessions" on a community
    /// server; if you want Steam/itch account linking later, swap the token
    /// issuing in HandleHello (NetServer) for a verified external login and
    /// keep everything downstream (PlayerAccount, friends, claims) the same.
    /// </summary>
    public class AccountService
    {
        private readonly FileStore _store;

        public AccountService(FileStore store) => _store = store;

        public PlayerAccount FindByName(string playerName)
        {
            foreach (var id in _store.ListIds("accounts"))
            {
                var acc = _store.Load<PlayerAccount>("accounts", id);
                if (acc != null && string.Equals(acc.PlayerName, playerName, StringComparison.OrdinalIgnoreCase))
                    return acc;
            }
            return null;
        }

        public PlayerAccount FindById(string playerId) => _store.Load<PlayerAccount>("accounts", playerId);

        public (PlayerAccount account, string plainToken) CreateAccount(string playerName)
        {
            var account = new PlayerAccount { PlayerName = playerName };
            string token = GenerateToken();
            account.AuthTokenHash = Hash(token);
            _store.Save("accounts", account.PlayerId, account);
            return (account, token);
        }

        public bool ValidateToken(PlayerAccount account, string plainToken) =>
            account != null && account.AuthTokenHash == Hash(plainToken ?? "");

        public void Touch(PlayerAccount account)
        {
            account.LastSeenUtc = DateTime.UtcNow;
            _store.Save("accounts", account.PlayerId, account);
        }

        public void Save(PlayerAccount account) => _store.Save("accounts", account.PlayerId, account);

        private static string GenerateToken()
        {
            byte[] bytes = new byte[24];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string Hash(string input)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }
    }
}
