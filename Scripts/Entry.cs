using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;

namespace STS2Advisor.Scripts;

[ModInitializer("Init")]
public static class Entry
{
    public static AdvisorPanel? Panel { get; private set; }
    public static EventAdvisorPanel? EventPanel { get; private set; }
    public static GrandOrderPanel? GrandOrder { get; private set; }

    public static void Init()
    {
        var harmony = new Harmony("gundam11.sts2advisor");
        try
        {
            harmony.PatchAll();
        }
        catch (Exception e)
        {
            // Avoid hard failing mod load; log the root cause for easier troubleshooting.
            Log.Error($"[STS2Advisor] Harmony.PatchAll failed: {e}");
        }
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
        Log.Debug("[STS2Advisor] 加载成功！");

        NGame? game = NGame.Instance;
        if (game != null)
        {
            Panel = new AdvisorPanel();
            ((GodotObject)game).CallDeferred(
                Node.MethodName.AddChild,
                (Variant)(GodotObject)Panel
            );

            EventPanel = new EventAdvisorPanel();
            ((GodotObject)game).CallDeferred(
                Node.MethodName.AddChild,
                (Variant)(GodotObject)EventPanel
            );

            GrandOrder = new GrandOrderPanel();
            ((GodotObject)game).CallDeferred(
                Node.MethodName.AddChild,
                (Variant)(GodotObject)GrandOrder
            );

            var mapPanel = new Sha_Nagba_Imuru();
            ((GodotObject)game).CallDeferred(
                Node.MethodName.AddChild,
                (Variant)(GodotObject)mapPanel
        
            );
        }
        else
        {
            Log.Error("[STS2Advisor] NGame.Instance 为空，面板初始化失败！");
        }
    }
}