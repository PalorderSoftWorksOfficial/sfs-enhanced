using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.Persistence
{
    public class AccountService
    {
        private readonly FileStore _store;

        public AccountService(FileStore store) => _store = store;

        public PlayerAccount FindByName(string playerName)
        {
            foreach (string id in _store.ListIds("accounts"))
            {
                PlayerAccount acc = _store.Load<PlayerAccount>("accounts", id);
                if (acc != null && string.Equals(acc.PlayerName, playerName, StringComparison.OrdinalIgnoreCase))
                    return acc;
            }
            return null;
        }

        public PlayerAccount FindById(string playerId) => _store.Load<PlayerAccount>("accounts", playerId);

        public (PlayerAccount account, string plainToken) CreateAccount(string playerName)
        {
            PlayerAccount account = new PlayerAccount { PlayerName = playerName };
            string token = GenerateToken();
            account.AuthTokenKey = TokenKey(token);
            _store.Save("accounts", account.PlayerId, account);
            return (account, token);
        }

        public bool ValidateToken(PlayerAccount account, string plainToken)
        {
            if (account == null || string.IsNullOrEmpty(account.AuthTokenKey) || string.IsNullOrEmpty(plainToken)) return false;
            return FixedTimeEquals(account.AuthTokenKey, TokenKey(plainToken));
        }

        public bool ValidateProof(PlayerAccount account, byte[] proof, byte[] transcriptHash)
        {
            if (account == null || string.IsNullOrEmpty(account.AuthTokenKey) || proof == null) return false;
            byte[] tokenKey;
            try { tokenKey = Convert.FromBase64String(account.AuthTokenKey); }
            catch { return false; }
            byte[] expected = SessionAuth.ComputeTokenProof(tokenKey, transcriptHash);
            return SessionAuth.FixedTimeEquals(expected, proof);
        }

        public void Touch(PlayerAccount account)
        {
            account.LastSeenUtc = DateTime.UtcNow;
            _store.Save("accounts", account.PlayerId, account);
        }

        public void Save(PlayerAccount account) => _store.Save("accounts", account.PlayerId, account);

        private static string GenerateToken()
        {
            return Convert.ToBase64String(SessionAuth.RandomBytes(24));
        }

        private static string TokenKey(string plainToken) => Convert.ToBase64String(SessionAuth.ComputeTokenKey(plainToken));

        private static bool FixedTimeEquals(string a, string b)
        {
            byte[] left = Encoding.UTF8.GetBytes(a ?? string.Empty);
            byte[] right = Encoding.UTF8.GetBytes(b ?? string.Empty);
            return SessionAuth.FixedTimeEquals(left, right);
        }
    }
}
