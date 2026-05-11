using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using PartyObserver.UI;

namespace PartyObserver.Services;

internal static class PartyObserverService
{
	public static void InitializeRunContext()
	{
		PartyObserverRegistry.BindToNetService(RunManager.Instance.IsInProgress ? RunManager.Instance.NetService : null);
	}

	public static void PersistSettings(PartyObserverSettings settings)
	{
		PartyObserverSettingsStore.Save(settings);
	}

	public static void AttachOverlay(NCombatUi combatUi)
	{
		EnsureOverlay();
	}

	public static void AttachOverlay(NRewardsScreen rewardsScreen)
	{
		EnsureOverlay();
	}

	public static void AttachOverlay(NEventRoom eventRoom)
	{
		EnsureOverlay();
	}

	public static void AttachOverlay(NCardRewardSelectionScreen selectionScreen)
	{
		EnsureOverlay();
	}

	public static void AttachOverlay(NChooseARelicSelection relicSelectionScreen)
	{
		EnsureOverlay();
	}

	public static void AttachOverlay(NMerchantInventory merchantInventory)
	{
		EnsureOverlay();
	}

	public static void RegisterPlayerState(NMultiplayerPlayerState playerState)
	{
		EnsureOverlay()?.RegisterPlayerState(playerState);
	}

	public static void UnregisterPlayerState(NMultiplayerPlayerState playerState)
	{
		TryGetOverlay()?.UnregisterPlayerState(playerState);
	}

	public static void UpdateRewardsSnapshot(NRewardsScreen rewardsScreen)
	{
		List<Reward> list = new List<Reward>();
		Control nodeOrNull = ((Node)rewardsScreen).GetNodeOrNull<Control>(NodePath.op_Implicit("%RewardsContainer"));
		if (nodeOrNull == null)
		{
			PartyObserverRegistry.ClearLocalSnapshot();
			return;
		}
		foreach (Node child in ((Node)nodeOrNull).GetChildren(false))
		{
			Node val = child;
			Node val2 = val;
			NRewardButton val3 = (NRewardButton)(object)((val2 is NRewardButton) ? val2 : null);
			if (val3 == null)
			{
				NLinkedRewardSet val4 = (NLinkedRewardSet)(object)((val2 is NLinkedRewardSet) ? val2 : null);
				if (val4 != null && val4.LinkedRewardSet != null)
				{
					list.AddRange(val4.LinkedRewardSet.Rewards);
				}
			}
			else if (val3.Reward != null)
			{
				list.Add(val3.Reward);
			}
		}
		if (list.Count == 0)
		{
			PartyObserverRegistry.ClearLocalSnapshot();
		}
		else
		{
			PartyObserverRegistry.UpdateLocalSnapshot(PartyObserverChoiceSnapshotBuilder.BuildRewardsScreen(list));
		}
	}

	public static void UpdateRelicSelectionSnapshot(IReadOnlyList<RelicModel> relics)
	{
		if (relics.Count == 0)
		{
			PartyObserverRegistry.ClearLocalSnapshot();
		}
		else
		{
			PartyObserverRegistry.UpdateLocalSnapshot(PartyObserverChoiceSnapshotBuilder.BuildRelicSelection(relics));
		}
	}

	public static void UpdateMerchantSnapshot(NMerchantInventory merchantInventory)
	{
		if (!merchantInventory.IsOpen || merchantInventory.Inventory == null)
		{
			PartyObserverRegistry.ClearLocalSnapshot();
		}
		else
		{
			PartyObserverRegistry.UpdateLocalSnapshot(PartyObserverChoiceSnapshotBuilder.BuildMerchantInventory(merchantInventory.Inventory));
		}
	}

	private static PartyObserverOverlay? EnsureOverlay()
	{
		PartyObserverSettings partyObserverSettings = PartyObserverSettingsStore.Load();
		if (!partyObserverSettings.Enabled)
		{
			return null;
		}
		InitializeRunContext();
		RunState val = RunManager.Instance.DebugOnlyGetState();
		IPlayerCollection val2 = (IPlayerCollection)(object)val;
		if (val2 == null || val2.Players.Count() <= 1)
		{
			return null;
		}
		if (NRun.Instance == null)
		{
			return null;
		}
		PartyObserverOverlay partyObserverOverlay = TryGetOverlay();
		if (partyObserverOverlay != null)
		{
			partyObserverOverlay.Initialize(partyObserverSettings);
			return partyObserverOverlay;
		}
		PartyObserverOverlay partyObserverOverlay2 = new PartyObserverOverlay();
		((Node)partyObserverOverlay2).Name = StringName.op_Implicit("PartyObserverOverlay");
		partyObserverOverlay = partyObserverOverlay2;
		partyObserverOverlay.Initialize(partyObserverSettings);
		((Node)NRun.Instance).AddChild((Node)(object)partyObserverOverlay, false, (InternalMode)0);
		GD.Print("PartyObserver: attached observer overlay");
		return partyObserverOverlay;
	}

	private static PartyObserverOverlay? TryGetOverlay()
	{
		NRun instance = NRun.Instance;
		return (instance != null) ? ((Node)instance).GetNodeOrNull<PartyObserverOverlay>(NodePath.op_Implicit("PartyObserverOverlay")) : null;
	}
}
