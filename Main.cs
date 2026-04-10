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
        var harmony = new Harmony("com.samhuelt.nomoreui");
        harmony.PatchAll(typeof(SkillInfoPatches));
        harmony.PatchAll(typeof(DamageTypoPatches));
        harmony.PatchAll(typeof(ParryingTypoPatches));
    }
}

public class NoMoreUIBehaviour : MonoBehaviour
{
    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Keypad5) && !Input.GetKeyDown(KeyCode.U))
            return;

        var uiRoot = SingletonBehavior<BattleUIRoot>.Instance;
        if (uiRoot == null)
            return;

        NoMoreUIPlugin.Hidden = !NoMoreUIPlugin.Hidden;
        SetRootUI(uiRoot, !NoMoreUIPlugin.Hidden);
        SetUnitUI(!NoMoreUIPlugin.Hidden);
        SetParryingTypoUI(!NoMoreUIPlugin.Hidden);
    }

    // ★ 매 프레임 후반에 잔존하는 패링 UI를 모두 강제 제거 (안전망)
    void LateUpdate()
    {
        if (!NoMoreUIPlugin.Hidden) return;

        // ParryingTypoUI (합 텍스트 타이포)
        var typos = Object.FindObjectsOfType<ParryingTypoUI>();
        if (typos != null)
            for (int i = 0; i < typos.Length; i++)
                if (typos[i] != null && typos[i].gameObject.activeSelf)
                    typos[i].gameObject.SetActive(false);

        // ParryingDiceUI (합 코인/주사위 숫자)
        var dices = Object.FindObjectsOfType<ParryingDiceUI>();
        if (dices != null)
            for (int i = 0; i < dices.Length; i++)
                if (dices[i] != null && dices[i].gameObject.activeSelf)
                    dices[i].gameObject.SetActive(false);

        // ParryingDiceAnimationController (합 코인 굴림 연출)
        var anims = Object.FindObjectsOfType<ParryingDiceAnimationController>();
        if (anims != null)
            for (int i = 0; i < anims.Length; i++)
                if (anims[i] != null && anims[i].gameObject.activeSelf)
                    anims[i].gameObject.SetActive(false);
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

        // ★ 전면 UI (라운드 타이포, 페이드, 웨이브 전환 등)
        // SetActive(false)로 끄면 웨이브 전환 로직이 멈추므로,
        // CanvasGroup.alpha = 0 으로 "보이지만 않게" 처리 → 클릭하면 다음 웨이브로 넘어감
        var frontUI = root._frontUIController;
        if (frontUI != null)
        {
            GameObject frontGO = ((Component)frontUI).gameObject;
            CanvasGroup cg = frontGO.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = frontGO.AddComponent<CanvasGroup>();

            if (!active) // UI 숨기기
            {
                cg.alpha = 0f;
                cg.blocksRaycasts = true;  // 클릭은 통과시켜서 웨이브 전환 가능
                cg.interactable = true;
            }
            else // UI 보이기
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }
        }

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

    // ★ 씬에 존재하는 모든 ParryingTypoUI 오브젝트를 찾아서 끄기/켜기
    private void SetParryingTypoUI(bool visible)
    {
        var allParryingTypos = Object.FindObjectsOfType<ParryingTypoUI>();
        if (allParryingTypos == null) return;

        for (int i = 0; i < allParryingTypos.Length; i++)
        {
            if (allParryingTypos[i] != null)
                allParryingTypos[i].gameObject.SetActive(visible);
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

// ★ 패링(합) 타이포 원천 차단 — 생성 자체를 막음
[HarmonyPatch]
internal static class ParryingTypoPatches
{
    // BattleUnitUIManager.CreateParryingTypo 는 Protected라 nameof() 불가 → 문자열로 지정
    [HarmonyPatch(typeof(BattleUnitUIManager), "CreateParryingTypo")]
    [HarmonyPrefix]
    static bool BlockCreateParryingTypo() => !NoMoreUIPlugin.Hidden;

    // 데이터 세팅도 이중으로 차단 (다른 경로로 호출될 경우 대비)
    [HarmonyPatch(typeof(ParryingTypoUI), nameof(ParryingTypoUI.SetParryingTypoData))]
    [HarmonyPrefix]
    static bool BlockSetParryingTypoData() => !NoMoreUIPlugin.Hidden;
}
