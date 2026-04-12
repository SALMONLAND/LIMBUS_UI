using BattleUI;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace BattleSpeed;

[BepInPlugin("com.samhuelt.battlespeed", "BattleSpeed", "2.0.0")]
public class BattleSpeedPlugin : BasePlugin
{
    internal static ManualLogSource Logger;
    internal static int ClashCount = 0;
    internal static bool IsClashing = false;
    internal static int LastClashFrame = -1;

    public override void Load()
    {
        Logger = Log;

        var harmony = new Harmony("com.samhuelt.battlespeed");

        // BattleDuelViewer 패치
        var bdvType = AccessTools.TypeByName("BattleDuelViewer");
        if (bdvType == null) { Log.LogError("BattleDuelViewer 타입을 못 찾음"); }
        else
        {
            var startCoin = AccessTools.Method(bdvType, "StartCoinToss", new[] { typeof(int) });
            var afterCamera = AccessTools.Method(bdvType, "SetAfterCointossCamera");
            if (startCoin != null) harmony.Patch(startCoin, prefix: new HarmonyMethod(typeof(BattleSpeedPlugin), nameof(OnCoinStart)));
            else Log.LogError("StartCoinToss 메서드를 못 찾음");
            if (afterCamera != null) harmony.Patch(afterCamera, postfix: new HarmonyMethod(typeof(BattleSpeedPlugin), nameof(OnCoinEnd)));
            else Log.LogError("SetAfterCointossCamera 메서드를 못 찾음");
        }

        // BattleSkillViewSkin 패치
        var bsvsType = AccessTools.TypeByName("BattleSkillViewSkin");
        if (bsvsType == null) { Log.LogError("BattleSkillViewSkin 타입을 못 찾음"); }
        else
        {
            var parryEvent = AccessTools.Method(bsvsType, "OnParryingEvent");
            if (parryEvent != null) harmony.Patch(parryEvent, postfix: new HarmonyMethod(typeof(BattleSpeedPlugin), nameof(OnClashStep)));
            else Log.LogError("OnParryingEvent 메서드를 못 찾음");
        }

        // BattleUnitView 패치
        var buvType = AccessTools.TypeByName("BattleUnitView");
        if (buvType == null) { Log.LogError("BattleUnitView 타입을 못 찾음"); }
        else
        {
            var parryEnd = AccessTools.Method(buvType, "OnParryingEnd");
            if (parryEnd != null) harmony.Patch(parryEnd, postfix: new HarmonyMethod(typeof(BattleSpeedPlugin), nameof(OnClashEnd)));
            else Log.LogError("OnParryingEnd 메서드를 못 찾음");
        }

        // ActRoundEndEffectUI 패치
        var roundEnd = AccessTools.Method(typeof(ActRoundEndEffectUI), "OnEnable");
        if (roundEnd != null) harmony.Patch(roundEnd, postfix: new HarmonyMethod(typeof(BattleSpeedPlugin), nameof(OnRoundEnd)));

        Log.LogInfo("BattleSpeed 2.0: Loaded!");
    }

    static void OnCoinStart(int duelCount)
    {
        if (IsClashing) return;
        float speed = 1f + (duelCount * 0.3f);
        Time.timeScale = speed;
        Logger.LogInfo($"[COIN] duelCount={duelCount} → {speed}x");
    }

    static void OnCoinEnd()
    {
        if (IsClashing) return;
        Time.timeScale = 1f;
        Logger.LogInfo("[COIN END] → 1x");
    }

    static void OnClashStep()
    {
        if (Time.frameCount == LastClashFrame) return;
        LastClashFrame = Time.frameCount;
        IsClashing = true;
        ClashCount++;
        float speed = 1f + (ClashCount * 0.3f);
        Time.timeScale = speed;
        Logger.LogInfo($"[CLASH #{ClashCount}] → {speed}x");
    }

    static void OnClashEnd()
    {
        IsClashing = false;
        ClashCount = 0;
        Time.timeScale = 1f;
        Logger.LogInfo("[CLASH END] → 1x");
    }

    static void OnRoundEnd()
    {
        ClashCount = 0;
        IsClashing = false;
        Time.timeScale = 1f;
        Logger.LogInfo("[ROUND END] 리셋 → 1x");
    }
}