using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Saves;
using System.Reflection;
using System.Collections.Generic;
using System;

namespace STS2Advisor.Scripts;

[HarmonyPatch]
internal static class GrandOrderLifecyclePatches
{
    [HarmonyPatch(typeof(NRun), "_Ready")]
    [HarmonyPostfix]
    private static void AfterRunReady()
    {
        // Bind network message handler when we have a multiplayer run.
        var net = RunManager.Instance.IsInProgress ? RunManager.Instance.NetService : null;
        GrandOrderNetSync.BindToNetService(net);
    }
}

[HarmonyPatch(typeof(NEventLayout))]
internal static class GrandOrderEventLayoutPatches
{
    [HarmonyPatch("AddOptions")]
    [HarmonyPostfix]
    private static void AfterEventOptionsAdded(NEventLayout __instance)
    {
        var snapshot = GrandOrderEventChoicesBuilder.BuildEventChoices(__instance);
        if (string.IsNullOrWhiteSpace(snapshot.Scene) && string.IsNullOrWhiteSpace(snapshot.Option))
        {
            GrandOrderNetSync.ClearLocalSnapshot(broadcast: true);
            return;
        }

        GrandOrderNetSync.UpdateLocalSnapshot(snapshot, broadcast: true);
    }

    [HarmonyPatch("ClearOptions")]
    [HarmonyPostfix]
    private static void AfterEventOptionsCleared()
    {
        GrandOrderNetSync.ClearLocalSnapshot(broadcast: true);
    }
}

[HarmonyPatch(typeof(NEventRoom))]
internal static class GrandOrderEventRoomPatches
{
    [HarmonyPatch("_ExitTree")]
    [HarmonyPostfix]
    private static void AfterEventRoomExitTree()
    {
        GrandOrderNetSync.ClearLocalSnapshot(broadcast: true);
    }

    [HarmonyPatch("OnEnteringEventCombat")]
    [HarmonyPostfix]
    private static void AfterEnteringEventCombat()
    {
        GrandOrderNetSync.ClearLocalSnapshot(broadcast: true);
    }
}

[HarmonyPatch(typeof(NRewardsScreen))]
internal static class GrandOrderRewardsScreenPatches
{
    [HarmonyPatch("_Ready")]
    [HarmonyPostfix]
    private static void AfterRewardsScreenReady(NRewardsScreen __instance)
    {
        // Initial content sometimes arrives after _Ready; we rely on UpdateScreenState/AfterOverlayShown too.
    }

    [HarmonyPatch("UpdateScreenState")]
    [HarmonyPostfix]
    private static void AfterRewardsScreenStateUpdated(NRewardsScreen __instance)
    {
        var snapshot = GrandOrderChoiceScreensBuilder.BuildRewardsScreen(__instance);
        if (string.IsNullOrWhiteSpace(snapshot.Scene) && string.IsNullOrWhiteSpace(snapshot.Option))
        {
            GrandOrderNetSync.ClearLocalSnapshot(broadcast: true);
            return;
        }

        GrandOrderNetSync.UpdateLocalSnapshot(snapshot, broadcast: true);
    }

    [HarmonyPatch("AfterOverlayShown")]
    [HarmonyPostfix]
    private static void AfterRewardsOverlayShown(NRewardsScreen __instance)
    {
        var snapshot = GrandOrderChoiceScreensBuilder.BuildRewardsScreen(__instance);
        if (string.IsNullOrWhiteSpace(snapshot.Scene) && string.IsNullOrWhiteSpace(snapshot.Option))
        {
            GrandOrderNetSync.ClearLocalSnapshot(broadcast: true);
            return;
        }

        GrandOrderNetSync.UpdateLocalSnapshot(snapshot, broadcast: true);
    }

    // NRewardsScreen in this version does not necessarily override _ExitTree.
    // Use overlay lifecycle callbacks we can reliably find.
    [HarmonyPatch("AfterOverlayClosed")]
    [HarmonyPostfix]
    private static void AfterRewardsOverlayClosed()
    {
        GrandOrderNetSync.ClearLocalSnapshot(broadcast: true);
    }
}

[HarmonyPatch(typeof(NCardRewardSelectionScreen))]
internal static class GrandOrderCardRewardSelectionPatches
{
    [HarmonyPatch("RefreshOptions")]
    [HarmonyPostfix]
    private static void AfterCardRewardOptionsRefreshed(
        NCardRewardSelectionScreen __instance,
        object[] __args)
    {
        // Avoid signature mismatch issues across game versions:
        // RefreshOptions' exact parameter types may differ, so we fish them out from __args.
        IReadOnlyList<CardCreationResult> options = Array.Empty<CardCreationResult>();
        IReadOnlyList<CardRewardAlternative> extraOptions = Array.Empty<CardRewardAlternative>();

        foreach (object? arg in __args)
        {
            if (arg is IReadOnlyList<CardCreationResult> ro1)
                options = ro1;
            else if (options.Count == 0 && arg is IEnumerable<CardCreationResult> en1)
                options = en1.ToList();

            if (arg is IReadOnlyList<CardRewardAlternative> ro2)
                extraOptions = ro2;
            else if (extraOptions.Count == 0 && arg is IEnumerable<CardRewardAlternative> en2)
                extraOptions = en2.ToList();
        }

        var snapshot = GrandOrderChoiceScreensBuilder.BuildCardRewardSelection(options, extraOptions);
        if (string.IsNullOrWhiteSpace(snapshot.Scene) && string.IsNullOrWhiteSpace(snapshot.Option))
        {
            GrandOrderNetSync.ClearLocalSnapshot(broadcast: true);
            return;
        }

        GrandOrderNetSync.UpdateLocalSnapshot(snapshot, broadcast: true);
    }

    [HarmonyPatch("_ExitTree")]
    [HarmonyPostfix]
    private static void AfterCardRewardSelectionExitTree()
    {
        GrandOrderNetSync.ClearLocalSnapshot(broadcast: true);
    }
}

[HarmonyPatch(typeof(NChooseARelicSelection))]
internal static class GrandOrderRelicSelectionPatches
{
    private static readonly FieldInfo? RelicsField =
        typeof(NChooseARelicSelection).GetField("_relics", BindingFlags.Instance | BindingFlags.NonPublic);

    [HarmonyPatch("_Ready")]
    [HarmonyPostfix]
    private static void AfterRelicSelectionReady(NChooseARelicSelection __instance)
    {
        var relics = GetRelics(__instance);
        var snapshot = GrandOrderChoiceScreensBuilder.BuildRelicSelection(relics);
        if (string.IsNullOrWhiteSpace(snapshot.Scene) && string.IsNullOrWhiteSpace(snapshot.Option))
        {
            GrandOrderNetSync.ClearLocalSnapshot(broadcast: true);
            return;
        }

        GrandOrderNetSync.UpdateLocalSnapshot(snapshot, broadcast: true);
    }

    [HarmonyPatch("_ExitTree")]
    [HarmonyPostfix]
    private static void AfterRelicSelectionExitTree()
    {
        GrandOrderNetSync.ClearLocalSnapshot(broadcast: true);
    }

    private static IReadOnlyList<RelicModel> GetRelics(NChooseARelicSelection selectionScreen)
    {
        if (RelicsField?.GetValue(selectionScreen) is IReadOnlyList<RelicModel> relics)
            return relics;
        if (RelicsField?.GetValue(selectionScreen) is List<RelicModel> list)
            return list;
        return Array.Empty<RelicModel>();
    }
}

[HarmonyPatch(typeof(NMerchantInventory))]
internal static class GrandOrderMerchantInventoryPatches
{
    [HarmonyPatch("Open")]
    [HarmonyPostfix]
    private static void AfterMerchantInventoryOpened(NMerchantInventory __instance)
    {
        UpdateMerchant(__instance);
    }

    // OnPurchaseCompleted in STS2 is a non-public method:
    //   void OnPurchaseCompleted(PurchaseStatus status, MerchantEntry entry)
    [HarmonyPatch(typeof(NMerchantInventory), "OnPurchaseCompleted",
        new Type[] { typeof(PurchaseStatus), typeof(MerchantEntry) })]
    [HarmonyPostfix]
    private static void AfterMerchantPurchaseCompleted(
        NMerchantInventory __instance,
        PurchaseStatus status,
        MerchantEntry entry)
    {
        UpdateMerchant(__instance);
    }

    // Close() is also non-public; keep postfix signature empty and match no-arg overload explicitly.
    [HarmonyPatch(typeof(NMerchantInventory), "Close", new Type[] { })]
    [HarmonyPostfix]
    private static void AfterMerchantInventoryClosed()
    {
        GrandOrderNetSync.ClearLocalSnapshot(broadcast: true);
    }

    [HarmonyPatch("_ExitTree")]
    [HarmonyPostfix]
    private static void AfterMerchantInventoryExitTree()
    {
        GrandOrderNetSync.ClearLocalSnapshot(broadcast: true);
    }

    private static void UpdateMerchant(NMerchantInventory instance)
    {
        var snapshot = GrandOrderChoiceScreensBuilder.BuildMerchantInventory(instance);
        if (string.IsNullOrWhiteSpace(snapshot.Scene) && string.IsNullOrWhiteSpace(snapshot.Option))
        {
            GrandOrderNetSync.ClearLocalSnapshot(broadcast: true);
            return;
        }

        GrandOrderNetSync.UpdateLocalSnapshot(snapshot, broadcast: true);
    }
}

