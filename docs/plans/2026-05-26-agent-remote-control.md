# Agent Remote Control Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Let AndroidSideloader remember and reconnect wireless ADB devices on restart, then progressively expose safe remote-control surfaces for an AI assistant.

**Architecture:** First extract testable device-control primitives from WinForms event handlers. Persist wireless ADB endpoints in a structured store and reconnect them on startup. Later phases should expose those services through a localhost control API and an MCP sidecar rather than automating UI clicks.

**Tech Stack:** C#/.NET Framework 4.8 WinForms, ADB, JSON persistence via `DataContractJsonSerializer`, future localhost HTTP/named-pipe API, future MCP sidecar.

---

### Task 1: Persist wireless ADB endpoints

**Files:**
- Create: `Models/WirelessAdbConnection.cs`
- Create: `Utilities/WirelessAdbConnectionStore.cs`
- Create: `Services/AdbWirelessReconnectService.cs`
- Test: `Tests/AdbWirelessReconnectServiceTests.cs`

**Steps:**
1. Write failing service tests for reconnect priority and fallback.
2. Add serial/host/port model and persisted state.
3. Add reconnect service with injectable store and ADB runner.
4. Verify tests pass with `mcs` + `mono`.

### Task 2: Wire reconnect into startup and wireless connect flows

**Files:**
- Modify: `MainForm.cs`
- Modify: `AndroidSideloader.csproj`
- Create: `Services/AdbCommandRunner.cs`

**Steps:**
1. Add project includes and `System.Runtime.Serialization` reference.
2. Save successful wireless ADB connections from USB/manual/scan flows.
3. Migrate existing `platform-tools/StoredIP.txt` into `WirelessAdbConnections.json`.
4. Try saved endpoints on startup, last-active first, without a modal failure dialog.
5. Disable auto-reconnect when the user disables wireless ADB.

### Task 3: Next phase — local control API skeleton

**Files TBD:**
- Create a small local-only command surface over extracted services.
- Require random token auth.
- Start with status/device/reconnect tools before install/uninstall.

### Task 4: Next phase — MCP sidecar

**Files TBD:**
- Add an MCP server that calls the local control API.
- Expose conservative tools first: status, list devices, reconnect wireless ADB.
- Gate destructive tools behind explicit confirmation and audit logging.
