using AndroidSideloader.Models;
using AndroidSideloader.Services;
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace AndroidSideloader.Utilities
{
    public sealed class WirelessAdbConnectionStore : IWirelessAdbConnectionStore
    {
        private readonly string filePath;

        public WirelessAdbConnectionStore(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Store path is required", nameof(filePath));
            this.filePath = filePath;
        }

        public WirelessAdbConnectionState Load()
        {
            try
            {
                if (!File.Exists(filePath)) return new WirelessAdbConnectionState();

                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json)) return new WirelessAdbConnectionState();

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(WirelessAdbConnectionState));
                    return (WirelessAdbConnectionState)serializer.ReadObject(stream) ?? new WirelessAdbConnectionState();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to load wireless ADB connection store: " + ex.Message, LogLevel.WARNING);
                return new WirelessAdbConnectionState();
            }
        }

        public void Save(WirelessAdbConnectionState state)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

                using (var stream = new MemoryStream())
                {
                    var serializer = new DataContractJsonSerializer(typeof(WirelessAdbConnectionState));
                    serializer.WriteObject(stream, state ?? new WirelessAdbConnectionState());
                    File.WriteAllText(filePath, Encoding.UTF8.GetString(stream.ToArray()));
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to save wireless ADB connection store: " + ex.Message, LogLevel.WARNING);
            }
        }
    }
}
