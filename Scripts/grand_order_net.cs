using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using System;

namespace STS2Advisor.Scripts;

internal sealed class GrandOrderNetSnapshot : IPacketSerializable
{
    public string Scene { get; set; } = "";
    public string Option { get; set; } = "";
    public string Description { get; set; } = "";

    public void Serialize(PacketWriter writer)
    {
        writer.WriteString(Scene);
        writer.WriteString(Option);
        writer.WriteString(Description);
    }

    public void Deserialize(PacketReader reader)
    {
        Scene = reader.ReadString();
        Option = reader.ReadString();
        Description = reader.ReadString();
    }
}

internal sealed class GrandOrderSnapshotMessage : INetMessage, IPacketSerializable
{
    private GrandOrderNetSnapshot? _snapshot;

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => (NetTransferMode)2;

    public LogLevel LogLevel => (LogLevel)0;

    internal static GrandOrderSnapshotMessage Create(GrandOrderNetSnapshot snapshot)
    {
        return new GrandOrderSnapshotMessage
        {
            _snapshot = snapshot,
        };
    }

    internal static GrandOrderSnapshotMessage CreateClear()
    {
        return new GrandOrderSnapshotMessage();
    }

    internal GrandOrderNetSnapshot? ToSnapshot() => _snapshot;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteBool(_snapshot != null);
        if (_snapshot != null)
        {
            writer.Write<GrandOrderNetSnapshot>(_snapshot);
        }
    }

    public void Deserialize(PacketReader reader)
    {
        _snapshot = reader.ReadBool() ? reader.Read<GrandOrderNetSnapshot>() : null;
    }
}

internal static class GrandOrderNetSync
{
    private static INetGameService? _netService;

    internal static void BindToNetService(INetGameService? netService)
    {
        if (ReferenceEquals(_netService, netService))
            return;

        if (_netService != null)
        {
            _netService.UnregisterMessageHandler<GrandOrderSnapshotMessage>(
                (MessageHandlerDelegate<GrandOrderSnapshotMessage>)HandleSnapshotMessage);
        }

        _netService = netService;

        // Clear local cache when switching sessions.
        if (GrandOrderPanel.Instance != null)
            GrandOrderPanel.Instance.ClearAllSnapshots();

        if (_netService != null)
        {
            _netService.RegisterMessageHandler<GrandOrderSnapshotMessage>(
                (MessageHandlerDelegate<GrandOrderSnapshotMessage>)HandleSnapshotMessage);
        }
    }

    private static void HandleSnapshotMessage(GrandOrderSnapshotMessage message, ulong senderId)
    {
        try
        {
            var snap = message.ToSnapshot();
            GrandOrderPanel.Instance?.ApplyNetSnapshot(senderId, snap);
        }
        catch (Exception e)
        {
            Log.Error($"[grand_order] Net snapshot handle failed: {e}");
        }
    }

    internal static void UpdateLocalSnapshot(GrandOrderNetSnapshot snapshot, bool broadcast)
    {
        // Always update local UI/registry immediately.
        ulong localId = _netService?.NetId ?? 0;
        if (localId != 0)
            GrandOrderPanel.Instance?.ApplyNetSnapshot(localId, snapshot);
        else
            GrandOrderPanel.Instance?.ApplyLocalSnapshotWithoutNet(snapshot);

        if (!broadcast || _netService == null || !_netService.IsConnected)
            return;

        _netService.SendMessage(
            GrandOrderSnapshotMessage.Create(snapshot));
    }

    internal static void ClearLocalSnapshot(bool broadcast)
    {
        ulong localId = _netService?.NetId ?? 0;
        if (localId != 0)
            GrandOrderPanel.Instance?.ApplyNetSnapshot(localId, null);
        else
            GrandOrderPanel.Instance?.ClearLocalSnapshots();

        if (!broadcast || _netService == null || !_netService.IsConnected)
            return;

        _netService.SendMessage(GrandOrderSnapshotMessage.CreateClear());
    }
}

