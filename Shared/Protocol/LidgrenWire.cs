using System;
using System.Text;
using Newtonsoft.Json;

namespace SFSEnhanced.Shared.Protocol
{
    public static class LidgrenWire
    {
        public const int HeaderBytes = 5;
        public const int MaxPayloadBytes = 8 * 1024 * 1024;
        public const int MaxPacketBytes = MaxPayloadBytes + HeaderBytes;

        public static byte[] Encode(PacketType type, object payload)
        {
            string json = payload == null ? "null" : JsonConvert.SerializeObject(payload);
            byte[] body = Encoding.UTF8.GetBytes(json);
            if (body.Length > MaxPayloadBytes) throw new InvalidOperationException("Packet payload is too large.");
            var data = new byte[body.Length + HeaderBytes];
            data[0] = (byte)type;
            data[1] = (byte)(body.Length >> 24);
            data[2] = (byte)(body.Length >> 16);
            data[3] = (byte)(body.Length >> 8);
            data[4] = (byte)body.Length;
            Buffer.BlockCopy(body, 0, data, HeaderBytes, body.Length);
            return data;
        }

        public static (PacketType type, string json) Decode(byte[] data)
        {
            if (data == null || data.Length < HeaderBytes) throw new InvalidOperationException("Malformed network packet.");
            int length = (data[1] << 24) | (data[2] << 16) | (data[3] << 8) | data[4];
            if (length < 0 || length > MaxPayloadBytes || data.Length != length + HeaderBytes) throw new InvalidOperationException("Invalid network packet length.");
            return ((PacketType)data[0], Encoding.UTF8.GetString(data, HeaderBytes, length));
        }

        public static bool IsHotStream(PacketType type)
        {
            return type == PacketType.BuildStateUpdate || type == PacketType.RocketPrimaryState || type == PacketType.RocketSecondaryState;
        }

        public static int GetChannel(PacketType type) => IsHotStream(type) ? 1 : 0;
    }
}
