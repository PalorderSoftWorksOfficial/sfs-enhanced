using System;

namespace SFSEnhanced.Shared.Networking
{
    public sealed class StagedEndpointStorage : IEndpointStorage
    {
        public const string PendingSuffix = ".pending";

        private readonly IEndpointStorage _inner;
        private readonly object _lock = new object();

        public StagedEndpointStorage(IEndpointStorage inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public string Read(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            lock (_lock)
            {
                return _inner.Read(key) ?? string.Empty;
            }
        }

        public bool Write(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return false;
            string payload = value ?? string.Empty;
            string pendingKey = key + PendingSuffix;

            lock (_lock)
            {
                if (!TryWriteRaw(pendingKey, payload)) return false;
                if (!string.Equals(TryReadRaw(pendingKey), payload, StringComparison.Ordinal))
                {
                    TryRemoveRaw(pendingKey);
                    return false;
                }

                if (!TryWriteRaw(key, payload)) return false;
                bool committed = string.Equals(TryReadRaw(key), payload, StringComparison.Ordinal);
                TryRemoveRaw(pendingKey);
                return committed;
            }
        }

        public void Remove(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            lock (_lock)
            {
                TryRemoveRaw(key);
                TryRemoveRaw(key + PendingSuffix);
            }
        }

        private bool TryWriteRaw(string key, string value)
        {
            try
            {
                return _inner.Write(key, value);
            }
            catch
            {
                return false;
            }
        }

        private string TryReadRaw(string key)
        {
            try
            {
                return _inner.Read(key) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void TryRemoveRaw(string key)
        {
            try
            {
                _inner.Remove(key);
            }
            catch
            {
            }
        }
    }
}
