using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SFSEnhanced.Shared.Protocol
{
    /// <summary>
    /// Wire envelope: [1 byte PacketType][4 byte big-endian int32 length][UTF8 JSON payload].
    /// Deliberately simple (no external netcode library) so it compiles under both
    /// .NET Framework 4.8 (the game/mod side) and modern .NET (the server) with zero
    /// extra dependencies beyond Newtonsoft.Json, which SFS already ships with.
    /// </summary>
    public static class NetMessage
    {
        public const int MaxPayloadBytes = 8 * 1024 * 1024; // 8 MB — generous for a world upload chunk

        public static async Task WriteAsync(Stream stream, PacketType type, object payload)
        {
            string json = payload == null ? "null" : JsonConvert.SerializeObject(payload);
            byte[] body = Encoding.UTF8.GetBytes(json);
            if (body.Length > MaxPayloadBytes)
                throw new InvalidOperationException($"Packet {type} payload too large: {body.Length} bytes");

            byte[] header = new byte[5];
            header[0] = (byte)type;
            WriteInt32BigEndian(header, 1, body.Length);

            await stream.WriteAsync(header, 0, header.Length).ConfigureAwait(false);
            if (body.Length > 0)
                await stream.WriteAsync(body, 0, body.Length).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }

        public static async Task<(PacketType type, T payload)> ReadAsync<T>(Stream stream)
        {
            var (type, json) = await ReadRawAsync(stream).ConfigureAwait(false);
            T payload = json == "null" || string.IsNullOrEmpty(json)
                ? default
                : JsonConvert.DeserializeObject<T>(json);
            return (type, payload);
        }

        public static async Task<(PacketType type, string json)> ReadRawAsync(Stream stream)
        {
            byte[] header = await ReadExactAsync(stream, 5).ConfigureAwait(false);
            if (header == null) return (PacketType.Disconnect, null); // stream closed

            var type = (PacketType)header[0];
            int length = ReadInt32BigEndian(header, 1);
            if (length < 0 || length > MaxPayloadBytes)
                throw new InvalidDataException($"Bad packet length {length} for type {type}");

            if (length == 0) return (type, "null");

            byte[] body = await ReadExactAsync(stream, length).ConfigureAwait(false);
            if (body == null) return (PacketType.Disconnect, null);

            return (type, Encoding.UTF8.GetString(body));
        }

        private static async Task<byte[]> ReadExactAsync(Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = await stream.ReadAsync(buffer, offset, count - offset).ConfigureAwait(false);
                if (read == 0) return null; // remote closed the connection
                offset += read;
            }
            return buffer;
        }

        private static void WriteInt32BigEndian(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        private static int ReadInt32BigEndian(byte[] buffer, int offset)
        {
            return (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];
        }
    }
}
