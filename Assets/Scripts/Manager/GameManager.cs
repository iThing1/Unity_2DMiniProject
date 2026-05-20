using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GameData;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Loading;
    public GameContext Context { get; private set; } = new GameContext();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GameEvents.OnStageClear += HandleStageClear;
        GameEvents.OnStageFailed += HandleStageFailed;
        GameEvents.OnStageSelected += HandleStageSelected;
    }

    private void OnDisable()
    {
        GameEvents.OnStageClear -= HandleStageClear;
        GameEvents.OnStageFailed -= HandleStageFailed;
        GameEvents.OnStageSelected -= HandleStageSelected;
    }

    private async void Start()
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await GameDataManager.Instance.RegisterAllTables();

        ChangeState(GameState.MainMenu);
        PreloadPlanetSprites();

        Context.CurrentGold = 100f; // [DEBUG] 초기 골드
        Context.CurrentIngot = 100f;  // [DEBUG] 초기 주괴
    }

    private void PreloadPlanetSprites()
    {
        ResourceManager.Instance.LoadSpriteFromSheet("Planet", "Planet_0", OnPlanetSpritesLoaded);
    }

    // Debug 로그용 콜백 메서드
    private void OnPlanetSpritesLoaded(Sprite sprite)
    {
        if (sprite == null)
            Debug.LogWarning("[GameManager] Sprite Sheet 프리로드 실패");
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        GameState prev = CurrentState;
        CurrentState = newState;

        GameEvents.RaiseGameStateChanged(prev, newState);
        Debug.Log($"[GameManager] 상태 전환: {prev} → {newState}");
    }

    public void StartGamePlay() => ChangeState(GameState.GamePlay);
    public void ReturnToLobby() => ChangeState(GameState.Lobby);

    private void HandleStageSelected(string stageId)
    {
        Context.LastSelectedStageId = stageId;
    }

    private void HandleStageClear(string stageId)
    {
        Context.StageClearStatus[stageId] = true;   // 클리어 상태 저장. TODO: 다음 스테이지 잠금 해제용

        if (!Context.UnlockedStageIds.Contains(stageId))
            Context.UnlockedStageIds.Add(stageId);
    }

    private void HandleStageFailed(string stageId)
    {
        Context.StageClearStatus[stageId] = false;
    }

    // =========================================================================
    // 재화 관련
    // =========================================================================

    public void AddGold(float amount)
    {
        Context.CurrentGold += amount;
        GameEvents.RaiseGoldChanged(Context.CurrentGold);
    }

    public bool TrySpendGold(float amount)
    {
        if (Context.CurrentGold < amount) return false;
        Context.CurrentGold -= amount;
        GameEvents.RaiseGoldChanged(Context.CurrentGold);
        return true;
    }

    public void AddIngot(float amount)
    {
        Context.CurrentIngot += amount;
        GameEvents.RaiseIngotChanged(Context.CurrentIngot);
    }

    public bool TrySpendIngot(float amount)
    {
        if (Context.CurrentIngot < amount) return false;
        Context.CurrentIngot -= amount;
        GameEvents.RaiseIngotChanged(Context.CurrentIngot);
        return true;
    }

    // =========================================================================
    // 업그레이드 관련
    // =========================================================================

    public int GetUpgradeLevel(string upgradeId)
        => Context.UpgradeLevels.TryGetValue(upgradeId, out int lv) ? lv : 0;

    public bool TryUpgrade(string upgradeId)
    {
        var data = GameDataManager.Instance.Get<UpgradeData>(upgradeId);
        if (data == null) return false;

        int currentLevel = GetUpgradeLevel(upgradeId);
        if (currentLevel >= data.MaxLevel) return false;

        float goldCost = data.BaseGoldCost + data.CostIncrease * currentLevel;
        float ingotCost = data.BaseIngotCost + data.IngotIncrease * currentLevel;

        if (!TrySpendGold(goldCost)) return false;
        if (!TrySpendIngot(ingotCost)) { AddGold(goldCost); return false; }

        int newLevel = currentLevel + 1;
        Context.UpgradeLevels[upgradeId] = newLevel;

        GameEvents.RaiseUpgradeCompleted(upgradeId, newLevel);
        return true;
    }

    public float GetUpgradeStat(string upgradeId)
    {
        var data = GameDataManager.Instance.Get<UpgradeData>(upgradeId);
        return data?.GetStat(GetUpgradeLevel(upgradeId)) ?? 0f;
    }

    public void RequestSFX(string soundId)
    {
        var data = GameDataManager.Instance.Get<SoundData>(soundId);
        if (data != null)
            GameEvents.RaiseSFXPlayRequested(data.SoundPath);
    }
}