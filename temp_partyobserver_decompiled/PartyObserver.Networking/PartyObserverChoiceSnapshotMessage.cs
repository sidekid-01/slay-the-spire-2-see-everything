using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace PartyObserver.Networking;

public class PartyObserverChoiceSnapshotMessage : INetMessage, IPacketSerializable
{
	private PartyObserverChoiceSnapshot? _snapshot;

	public bool ShouldBroadcast => true;

	public NetTransferMode Mode => (NetTransferMode)2;

	public LogLevel LogLevel => (LogLevel)0;

	internal static PartyObserverChoiceSnapshotMessage Create(PartyObserverChoiceSnapshot snapshot)
	{
		return new PartyObserverChoiceSnapshotMessage
		{
			_snapshot = snapshot.Clone()
		};
	}

	internal static PartyObserverChoiceSnapshotMessage CreateClear()
	{
		return new PartyObserverChoiceSnapshotMessage();
	}

	internal PartyObserverChoiceSnapshot? ToSnapshot()
	{
		return _snapshot?.Clone();
	}

	public void Serialize(PacketWriter writer)
	{
		writer.WriteBool(_snapshot != null);
		if (_snapshot != null)
		{
			writer.Write<PartyObserverChoiceSnapshot>(_snapshot);
		}
	}

	public void Deserialize(PacketReader reader)
	{
		_snapshot = (reader.ReadBool() ? reader.Read<PartyObserverChoiceSnapshot>() : null);
	}
}
