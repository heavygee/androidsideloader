using AndroidSideloader.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AndroidSideloader.Services
{
    public interface IWirelessAdbConnectionStore
    {
        WirelessAdbConnectionState Load();
        void Save(WirelessAdbConnectionState state);
    }

    public interface IAdbCommandRunner
    {
        AdbCommandResult Run(string command);
    }

    public sealed class AdbCommandResult
    {
        public AdbCommandResult(string output, string error)
        {
            Output = output ?? string.Empty;
            Error = error ?? string.Empty;
        }

        public string Output { get; private set; }
        public string Error { get; private set; }
    }

    public sealed class AdbReconnectResult
    {
        public bool Success { get; set; }
        public string Serial { get; set; }
        public string Message { get; set; }
    }

    public sealed class AdbWirelessReconnectService
    {
        private readonly IWirelessAdbConnectionStore store;
        private readonly IAdbCommandRunner adbRunner;
        private readonly Func<DateTime> utcNow;

        public AdbWirelessReconnectService(IWirelessAdbConnectionStore store, IAdbCommandRunner adbRunner, Func<DateTime> utcNow)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (adbRunner == null) throw new ArgumentNullException(nameof(adbRunner));
            if (utcNow == null) throw new ArgumentNullException(nameof(utcNow));

            this.store = store;
            this.adbRunner = adbRunner;
            this.utcNow = utcNow;
        }

        public AdbReconnectResult TryReconnectLastActive()
        {
            WirelessAdbConnectionState state = store.Load() ?? new WirelessAdbConnectionState();
            List<WirelessAdbConnection> candidates = GetReconnectCandidates(state);
            if (candidates.Count == 0)
            {
                return new AdbReconnectResult { Success = false, Message = "No wireless ADB connections are saved." };
            }

            string lastError = string.Empty;
            foreach (WirelessAdbConnection connection in candidates)
            {
                AdbCommandResult connectResult = adbRunner.Run(connection.ToConnectCommand());
                string combined = (connectResult.Output + "\n" + connectResult.Error).ToLowerInvariant();
                bool hasFailure = combined.Contains("cannot connect") ||
                    combined.Contains("failed") ||
                    combined.Contains("unable") ||
                    combined.Contains("refused") ||
                    combined.Contains("timed out");

                if (!hasFailure && (combined.Contains("connected") || combined.Contains("already connected") || IsDeviceListed(connection.Serial)))
                {
                    MarkConnected(state, connection.Serial);
                    return new AdbReconnectResult { Success = true, Serial = connection.Serial, Message = connectResult.Output };
                }

                lastError = string.IsNullOrWhiteSpace(connectResult.Error) ? connectResult.Output : connectResult.Error;
            }

            return new AdbReconnectResult { Success = false, Message = lastError };
        }

        public void RememberSuccessfulConnection(string serial)
        {
            if (string.IsNullOrWhiteSpace(serial)) return;

            WirelessAdbConnectionState state = store.Load() ?? new WirelessAdbConnectionState();
            MarkConnected(state, serial.Trim());
        }

        public void DisableAutoReconnect(string serial = null)
        {
            WirelessAdbConnectionState state = store.Load() ?? new WirelessAdbConnectionState();
            if (state.Connections == null) return;

            foreach (WirelessAdbConnection connection in state.Connections)
            {
                if (connection == null) continue;
                if (string.IsNullOrWhiteSpace(serial) || string.Equals(connection.Serial, serial, StringComparison.OrdinalIgnoreCase))
                {
                    connection.AutoReconnect = false;
                }
            }

            if (string.IsNullOrWhiteSpace(serial) || string.Equals(state.LastActiveSerial, serial, StringComparison.OrdinalIgnoreCase))
            {
                state.LastActiveSerial = string.Empty;
            }

            store.Save(state);
        }

        private List<WirelessAdbConnection> GetReconnectCandidates(WirelessAdbConnectionState state)
        {
            IEnumerable<WirelessAdbConnection> connections = state.Connections ?? Enumerable.Empty<WirelessAdbConnection>();
            List<WirelessAdbConnection> valid = connections
                .Where(c => c != null && c.AutoReconnect && !string.IsNullOrWhiteSpace(c.Serial))
                .GroupBy(c => c.Serial.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(c => c.LastSuccessfulUtc).First())
                .ToList();

            return valid
                .OrderByDescending(c => string.Equals(c.Serial, state.LastActiveSerial, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(c => c.LastSuccessfulUtc)
                .ThenByDescending(c => c.LastConnectedUtc)
                .ToList();
        }

        private bool IsDeviceListed(string serial)
        {
            AdbCommandResult devices = adbRunner.Run("devices");
            string[] lines = devices.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                if (line.StartsWith(serial, StringComparison.OrdinalIgnoreCase) && line.IndexOf("device", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private void MarkConnected(WirelessAdbConnectionState state, string serial)
        {
            if (state.Connections == null)
            {
                state.Connections = new List<WirelessAdbConnection>();
            }

            WirelessAdbConnection existing = state.Connections.FirstOrDefault(c => c != null && string.Equals(c.Serial, serial, StringComparison.OrdinalIgnoreCase));
            DateTime now = utcNow();
            if (existing == null)
            {
                existing = WirelessAdbConnection.FromSerial(serial, now);
                state.Connections.Add(existing);
            }
            else
            {
                existing.LastConnectedUtc = now;
                existing.LastSuccessfulUtc = now;
                existing.AutoReconnect = true;
            }

            state.LastActiveSerial = existing.Serial;
            store.Save(state);
        }
    }
}
