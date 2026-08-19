using System.Net.Sockets;
using System.Threading.Tasks;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Server.Networking
{
    /// <summary>One connected client's session state.</summary>
    public class ClientConnection
    {
        public TcpClient Socket { get; }
        public NetworkStream Stream { get; }
        public PlayerAccount Account { get; set; }
        public string CurrentWorldId { get; set; }
        private readonly SemaphoreSlimLite _writeLock = new();

        public ClientConnection(TcpClient socket)
        {
            Socket = socket;
            Stream = socket.GetStream();
        }

        public async Task SendAsync(PacketType type, object payload)
        {
            await _writeLock.WaitAsync();
            try
            {
                await NetMessage.WriteAsync(Stream, type, payload);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public void Close()
        {
            try { Socket.Close(); } catch { /* already closed */ }
        }
    }

    /// <summary>
    /// Tiny async mutex so concurrent SendAsync calls (broadcast + a direct reply
    /// racing on the same connection) don't interleave bytes mid-write.
    /// </summary>
    public class SemaphoreSlimLite
    {
        private readonly System.Threading.SemaphoreSlim _sem = new(1, 1);
        public Task WaitAsync() => _sem.WaitAsync();
        public void Release() => _sem.Release();
    }
}
