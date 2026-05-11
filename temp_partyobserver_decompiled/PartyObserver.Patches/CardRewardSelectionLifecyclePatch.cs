using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using PartyObserver.Services;

namespace PartyObserver.Patches;

[HarmonyPatch(typeof(NCardRewardSelectionScreen))]
public static class CardRewardSelectionLifecyclePatch
{
	[HarmonyPostfix]
	[HarmonyPatch("_Ready")]
	private static void AfterSelectionScreenReady(NCardRewardSelectionScreen __instance)
	{
		PartyObserverService.AttachOverlay(__instance);
	}

	[HarmonyPostfix]
	[HarmonyPatch("RefreshOptions")]
	private static void AfterOptionsRefreshed(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> extraOptions)
	{
		PartyObserverRegistry.UpdateLocalSnapshot(PartyObserverChoiceSnapshotBuilder.BuildCardRewardSelection(options, extraOptions));
	}

	[HarmonyPostfix]
	[HarmonyPatch("_ExitTree")]
	private static void AfterSelectionScreenExitTree()
	{
		PartyObserverRegistry.ClearLocalSnapshot();
	}
}
