using System;
using System.Collections.Generic;

namespace AndroidSideloader.Models
{
    public class WirelessAdbConnection
    {
        public string Host { get; set; }
        public int Port { get; set; } = 5555;
        public string Serial { get; set; }
        public DateTime LastConnectedUtc { get; set; }
        public DateTime LastSuccessfulUtc { get; set; }
        public bool AutoReconnect { get; set; } = true;

        public string ToConnectCommand()
        {
            return "connect " + Serial;
        }

        public static WirelessAdbConnection FromSerial(string serial, DateTime timestampUtc)
        {
            if (string.IsNullOrWhiteSpace(serial))
            {
                throw new ArgumentException("Wireless ADB serial is required", nameof(serial));
            }

            string trimmed = serial.Trim();
            string host = trimmed;
            int port = 5555;
            int separator = trimmed.LastIndexOf(':');
            if (separator > 0 && separator < trimmed.Length - 1)
            {
                host = trimmed.Substring(0, separator);
                int parsedPort;
                if (int.TryParse(trimmed.Substring(separator + 1), out parsedPort))
                {
                    port = parsedPort;
                }
            }

            return new WirelessAdbConnection
            {
                Host = host,
                Port = port,
                Serial = host + ":" + port,
                LastConnectedUtc = timestampUtc,
                LastSuccessfulUtc = timestampUtc,
                AutoReconnect = true
            };
        }
    }

    public class WirelessAdbConnectionState
    {
        public string LastActiveSerial { get; set; }
        public List<WirelessAdbConnection> Connections { get; set; } = new List<WirelessAdbConnection>();
    }
}
