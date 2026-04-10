using BattleUI;
using BattleUI.BattleUnit;
using BattleUI.BattleUnit.SkillInfoUI;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using UI.Utility;
using UnityEngine;

namespace NoMoreUI;

[BepInPlugin("com.samhuelt.nomoreui", "NoMoreUI", "1.0.0")]
public class NoMoreUIPlugin : BasePlugin
{
    internal static bool Hidden = false;

    public override void Load()
    {
        AddComponent<NoMoreUIBehaviour>();
        new Harmony("com.samhuelt.nomoreui").PatchAll(typeof(SkillInfoPatches));
        new Harmony("com.samhuelt.nomoreui").PatchAll(typeof(DamageTypoPatches));
    }
}

public class NoMoreUIBehaviour : MonoBehaviour
{
    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Keypad5))
            return;

        var uiRoot = SingletonBehavior<BattleUIRoot>.Instance;
        if (uiRoot == null)
            return;

        NoMoreUIPlugin.Hidden = !NoMoreUIPlugin.Hidden;
        SetRootUI(uiRoot, !NoMoreUIPlugin.Hidden);
        SetUnitUI(!NoMoreUIPlugin.Hidden);
    }

    private void SetRootUI(BattleUIRoot root, bool active)
    {
        // 메인 배틀 캔버스 (턴/웨이브 카운트, 작전 슬롯 등)
        var battleCanvas = root.battleUICanvas;
        if (battleCanvas != null)
            ((Component)battleCanvas).gameObject.SetActive(active);

        // 원근 UI 캔버스
        var perspectiveCanvas = root._perspectiveUICanvas;
        if (perspectiveCanvas != null)
            ((Component)perspectiveCanvas).gameObject.SetActive(active);

        // 전면 UI (라운드 타이포, 페이드 등)
        var frontUI = root._frontUIController;
        if (frontUI != null)
            ((Component)frontUI).gameObject.SetActive(active);

        // 오브젝트 UI
        var objectUI = root._objectUIController;
        if (objectUI != null)
            ((Component)objectUI).gameObject.SetActive(active);

        // 스킬 뷰 UI (합 볼 때 나오는 연출)
        var skillViewUI = root._battleSkillViewUIController;
        if (skillViewUI != null)
            ((Component)skillViewUI).gameObject.SetActive(active);

        // 인카운터 데미지 타이포
        var encounterDmg = root._battleEncounterDamageController;
        if (encounterDmg != null)
            ((Component)encounterDmg).gameObject.SetActive(active);

        // 이상현상 UI 컨트롤러
        var abUI = root._abUIController;
        if (abUI != null)
            ((Component)abUI).gameObject.SetActive(active);

        // 기본 UI 컨트롤러 (EGO, 단테 능력 등)
        var basicUI = root._basicUIController;
        if (basicUI != null)
            ((Component)basicUI).gameObject.SetActive(active);
    }

    private void SetUnitUI(bool visible)
    {
        var objMgr = SingletonBehavior<BattleObjectManager>.Instance;
        if (objMgr == null) return;

        List<BattleUnitView> views = objMgr.GetViewList();
        if (views == null) return;

        for (int i = 0; i < views.Count; i++)
        {
            BattleUnitView view = views[i];
            if (view == null) continue;

            BattleUnitUIManager uiMgr = view.UIManager;
            if (uiMgr == null) continue;

            // ★ 모든 유닛 UI 한번에 숨기기 (HP바, 스태거, 버프, 액션슬롯 등)
            uiMgr.SetHideAllUI(!visible, true, true, true);

            // 하단 UI 캔버스 직접 끄기 (HP바/스태거바 확실히 제거)
            var canvasUi = uiMgr.canvas_ui;
            if (canvasUi != null)
                ((Component)canvasUi).gameObject.SetActive(visible);

            // 상단 UI 캔버스 (스킬 코인, 다이얼로그 등)
            var canvasUpper = uiMgr.canvas_upper;
            if (canvasUpper != null)
                ((Component)canvasUpper).gameObject.SetActive(visible);

            // 데미지 텍스트 UI
            var dmgTypo = uiMgr._damageTypoUI;
            if (dmgTypo != null)
                ((Component)dmgTypo).gameObject.SetActive(visible);

            // 브레이크 타이포
            var breakTypo = uiMgr._breakTypoUI;
            if (breakTypo != null)
                ((Component)breakTypo).gameObject.SetActive(visible);

            // 스킬 코인 정보 UI
            UnitSkillInfoUI skillInfo = uiMgr._unitSkillInfoUI;
            if (skillInfo != null)
            {
                if (!visible)
                {
                    skillInfo.CloseBothSideUI();
                    CanvasGroup cg = skillInfo.SkillInfoCanvasGroup;
                    if (cg != null) cg.alpha = 0f;
                }
                else
                {
                    CanvasGroup cg = skillInfo.SkillInfoCanvasGroup;
                    if (cg != null) cg.alpha = 1f;
                }
            }
        }
    }
}

[HarmonyPatch]
internal static class SkillInfoPatches
{
    [HarmonyPatch(typeof(UnitSkillInfoUI), nameof(UnitSkillInfoUI.Open),
        new[] { typeof(InfoModels.SkillCoinUIInfo), typeof(BattleUnitView), typeof(DIRECTION) })]
    [HarmonyPrefix]
    static bool BlockOpen1() => !NoMoreUIPlugin.Hidden;

    [HarmonyPatch(typeof(UnitSkillInfoUI), nameof(UnitSkillInfoUI.Open),
        new[] { typeof(InfoModels.SkillCoinUIInfo), typeof(DIRECTION) })]
    [HarmonyPrefix]
    static bool BlockOpen2() => !NoMoreUIPlugin.Hidden;
}

[HarmonyPatch]
internal static class DamageTypoPatches
{
    // 데미지 숫자 생성 차단
    [HarmonyPatch(typeof(BattleUnitUIManager), nameof(BattleUnitUIManager.CreateDamageTypo))]
    [HarmonyPrefix]
    static bool BlockDamageTypo() => !NoMoreUIPlugin.Hidden;

    // 어빌리티 데미지 타이포 차단
    [HarmonyPatch(typeof(BattleUnitUIManager), nameof(BattleUnitUIManager.CreateDamageTypoForAbility))]
    [HarmonyPrefix]
    static bool BlockDamageTypoAbility() => !NoMoreUIPlugin.Hidden;

    // 브레이크 타이포 차단
    [HarmonyPatch(typeof(BattleUnitUIManager), nameof(BattleUnitUIManager.CreateBreakTypo))]
    [HarmonyPrefix]
    static bool BlockBreakTypo() => !NoMoreUIPlugin.Hidden;

    // 가드 타이포 차단
    [HarmonyPatch(typeof(BattleUnitUIManager), nameof(BattleUnitUIManager.CreateGuardTypo))]
    [HarmonyPrefix]
    static bool BlockGuardTypo() => !NoMoreUIPlugin.Hidden;

    // 회복 타이포 차단
    [HarmonyPatch(typeof(BattleUnitUIManager), nameof(BattleUnitUIManager.CreateRecoverHpTypo))]
    [HarmonyPrefix]
    static bool BlockRecoverTypo() => !NoMoreUIPlugin.Hidden;

    // 데미지 텍스트 타이포 차단
    [HarmonyPatch(typeof(BattleUnitUIManager), nameof(BattleUnitUIManager.CreateDamageTypoText))]
    [HarmonyPrefix]
    static bool BlockDamageTypoText() => !NoMoreUIPlugin.Hidden;
}