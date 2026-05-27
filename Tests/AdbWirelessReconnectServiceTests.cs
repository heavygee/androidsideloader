using System;
using System.Collections.Generic;
using AndroidSideloader.Models;
using AndroidSideloader.Services;

public static class AdbWirelessReconnectServiceTests
{
    public static int Main()
    {
        ReconnectTriesLastActiveConnectionFirstAndMarksSuccess();
        ReconnectFallsBackToOlderConnectionWhenLastActiveFails();
        Console.WriteLine("AdbWirelessReconnectServiceTests passed");
        return 0;
    }

    private static void ReconnectTriesLastActiveConnectionFirstAndMarksSuccess()
    {
        var store = new FakeStore(new WirelessAdbConnectionState
        {
            LastActiveSerial = "192.168.1.20:5555",
            Connections = new List<WirelessAdbConnection>
            {
                new WirelessAdbConnection { Host = "192.168.1.10", Port = 5555, Serial = "192.168.1.10:5555", AutoReconnect = true, LastSuccessfulUtc = DateTime.UtcNow.AddMinutes(-10) },
                new WirelessAdbConnection { Host = "192.168.1.20", Port = 5555, Serial = "192.168.1.20:5555", AutoReconnect = true, LastSuccessfulUtc = DateTime.UtcNow.AddMinutes(-20) }
            }
        });
        var adb = new FakeAdbRunner("connected to 192.168.1.20:5555", "List of devices attached\n192.168.1.20:5555\tdevice\n");
        var service = new AdbWirelessReconnectService(store, adb, () => DateTime.UtcNow);

        var result = service.TryReconnectLastActive();

        Assert(result.Success, "Expected reconnect to succeed");
        Assert(result.Serial == "192.168.1.20:5555", "Expected last active serial to be selected first");
        Assert(adb.Commands[0] == "connect 192.168.1.20:5555", "Expected connect command for last active endpoint");
        Assert(store.SavedState.LastActiveSerial == "192.168.1.20:5555", "Expected successful serial to be persisted as last active");
    }

    private static void ReconnectFallsBackToOlderConnectionWhenLastActiveFails()
    {
        var store = new FakeStore(new WirelessAdbConnectionState
        {
            LastActiveSerial = "192.168.1.20:5555",
            Connections = new List<WirelessAdbConnection>
            {
                new WirelessAdbConnection { Host = "192.168.1.10", Port = 5555, Serial = "192.168.1.10:5555", AutoReconnect = true, LastSuccessfulUtc = DateTime.UtcNow.AddMinutes(-5) },
                new WirelessAdbConnection { Host = "192.168.1.20", Port = 5555, Serial = "192.168.1.20:5555", AutoReconnect = true, LastSuccessfulUtc = DateTime.UtcNow.AddMinutes(-1) }
            }
        });
        var adb = new FakeAdbRunner(
            "cannot connect to 192.168.1.20:5555",
            "connected to 192.168.1.10:5555",
            "List of devices attached\n192.168.1.10:5555\tdevice\n");
        var service = new AdbWirelessReconnectService(store, adb, () => DateTime.UtcNow);

        var result = service.TryReconnectLastActive();

        Assert(result.Success, "Expected fallback reconnect to succeed");
        Assert(result.Serial == "192.168.1.10:5555", "Expected fallback serial");
        Assert(adb.Commands[0] == "connect 192.168.1.20:5555", "Expected last active first");
        Assert(adb.Commands[1] == "connect 192.168.1.10:5555", "Expected fallback second");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class FakeStore : IWirelessAdbConnectionStore
    {
        private readonly WirelessAdbConnectionState state;
        public WirelessAdbConnectionState SavedState;
        public FakeStore(WirelessAdbConnectionState state) { this.state = state; }
        public WirelessAdbConnectionState Load() { return state; }
        public void Save(WirelessAdbConnectionState state) { SavedState = state; }
    }

    private sealed class FakeAdbRunner : IAdbCommandRunner
    {
        private readonly Queue<string> outputs;
        public readonly List<string> Commands = new List<string>();
        public FakeAdbRunner(params string[] outputs) { this.outputs = new Queue<string>(outputs); }
        public AdbCommandResult Run(string command)
        {
            Commands.Add(command);
            return new AdbCommandResult(outputs.Count == 0 ? string.Empty : outputs.Dequeue(), string.Empty);
        }
    }
}
