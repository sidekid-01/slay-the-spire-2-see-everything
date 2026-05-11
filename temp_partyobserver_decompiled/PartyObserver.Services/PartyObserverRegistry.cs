using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using PartyObserver.Networking;

namespace PartyObserver.Services;

internal static class PartyObserverRegistry
{
	private static readonly Dictionary<ulong, PartyObserverChoiceSnapshot> Snapshots = new Dictionary<ulong, PartyObserverChoiceSnapshot>();

	private static INetGameService? _netService;

	public static event Action<ulong>? SnapshotChanged;

	public static void BindToNetService(INetGameService? netService)
	{
		if (_netService != netService)
		{
			if (_netService != null)
			{
				_netService.UnregisterMessageHandler<PartyObserverChoiceSnapshotMessage>((MessageHandlerDelegate<PartyObserverChoiceSnapshotMessage>)HandleSnapshotMessage);
			}
			Snapshots.Clear();
			_netService = netService;
			if (_netService != null)
			{
				_netService.RegisterMessageHandler<PartyObserverChoiceSnapshotMessage>((MessageHandlerDelegate<PartyObserverChoiceSnapshotMessage>)HandleSnapshotMessage);
			}
		}
	}

	public static PartyObserverChoiceSnapshot? GetSnapshot(ulong playerId)
	{
		PartyObserverChoiceSnapshot value;
		return Snapshots.TryGetValue(playerId, out value) ? value.Clone() : null;
	}

	public static void UpdateLocalSnapshot(PartyObserverChoiceSnapshot snapshot, bool broadcast = true)
	{
		if (_netService != null)
		{
			PartyObserverChoiceSnapshot partyObserverChoiceSnapshot = snapshot.Clone();
			Snapshots[_netService.NetId] = partyObserverChoiceSnapshot;
			if (broadcast && _netService.IsConnected)
			{
				_netService.SendMessage<PartyObserverChoiceSnapshotMessage>(PartyObserverChoiceSnapshotMessage.Create(partyObserverChoiceSnapshot));
			}
			PartyObserverRegistry.SnapshotChanged?.Invoke(_netService.NetId);
		}
	}

	public static void ClearLocalSnapshot(bool broadcast = true)
	{
		if (_netService != null)
		{
			ulong netId = _netService.NetId;
			bool flag = Snapshots.Remove(netId);
			if (broadcast && _netService.IsConnected)
			{
				_netService.SendMessage<PartyObserverChoiceSnapshotMessage>(PartyObserverChoiceSnapshotMessage.CreateClear());
			}
			if (flag || broadcast)
			{
				PartyObserverRegistry.SnapshotChanged?.Invoke(netId);
			}
		}
	}

	private static void HandleSnapshotMessage(PartyObserverChoiceSnapshotMessage message, ulong senderId)
	{
		PartyObserverChoiceSnapshot partyObserverChoiceSnapshot = message.ToSnapshot();
		if (partyObserverChoiceSnapshot == null)
		{
			Snapshots.Remove(senderId);
		}
		else
		{
			Snapshots[senderId] = partyObserverChoiceSnapshot;
		}
		PartyObserverRegistry.SnapshotChanged?.Invoke(senderId);
	}
}
