using System;
using System.Text;

namespace SFSEnhanced.Shared.Protocol
{
    public enum ServerIdentityStatus
    {
        Valid,
        MissingKey,
        MalformedKey,
        OffCurveKey
    }

    public sealed class ServerIdentity
    {
        public ServerIdentityStatus Status;
        public string Error;
        public string Fingerprint;
        public string ShortFingerprint;
        public byte[] StaticX;
        public byte[] StaticY;

        public bool Valid => Status == ServerIdentityStatus.Valid;
    }

    public static class ServerIdentityFormat
    {
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string Hex(byte[] digest)
        {
            if (digest == null) return string.Empty;
            var text = new StringBuilder(digest.Length * 2);
            foreach (byte value in digest) text.Append(value.ToString("x2"));
            return text.ToString();
        }

        public static string Short(byte[] digest)
        {
            if (digest == null || digest.Length < 10) return string.Empty;
            var bits = new StringBuilder(16);
            int accumulator = 0;
            int accumulated = 0;
            for (int index = 0; index < 10; index++)
            {
                accumulator = (accumulator << 8) | digest[index];
                accumulated += 8;
                while (accumulated >= 5)
                {
                    accumulated -= 5;
                    bits.Append(Base32Alphabet[(accumulator >> accumulated) & 0x1F]);
                }
            }
            if (accumulated > 0)
                bits.Append(Base32Alphabet[(accumulator << (5 - accumulated)) & 0x1F]);

            string raw = bits.ToString();
            var grouped = new StringBuilder(raw.Length + 3);
            for (int index = 0; index < raw.Length; index++)
            {
                if (index > 0 && index % 4 == 0) grouped.Append('-');
                grouped.Append(raw[index]);
            }
            return grouped.ToString();
        }
    }

    public static class ServerIdentityCalculator
    {
        public static ServerIdentity FromStaticPoint(byte[] staticX, byte[] staticY)
        {
            var identity = new ServerIdentity { StaticX = staticX, StaticY = staticY };

            if (staticX == null || staticY == null || staticX.Length == 0 || staticY.Length == 0)
            {
                identity.Status = ServerIdentityStatus.MissingKey;
                identity.Error = "The server did not present an identity key, so it cannot be verified. Refusing to connect.";
                return identity;
            }

            if (staticX.Length != SessionAuth.CoordinateBytes || staticY.Length != SessionAuth.CoordinateBytes)
            {
                identity.Status = ServerIdentityStatus.MalformedKey;
                identity.Error = "The server presented a malformed identity key. Refusing to connect.";
                return identity;
            }

            if (!P256.IsOnCurve(staticX, staticY))
            {
                identity.Status = ServerIdentityStatus.OffCurveKey;
                identity.Error = "The server presented an invalid identity key that is not on the agreed curve. Refusing to connect.";
                return identity;
            }

            byte[] digest = SessionAuth.ComputeIdentityDigest(staticX, staticY);
            identity.Status = ServerIdentityStatus.Valid;
            identity.Fingerprint = ServerIdentityFormat.Hex(digest);
            identity.ShortFingerprint = ServerIdentityFormat.Short(digest);
            return identity;
        }
    }
}
