using GameData;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Loading;
    public GameContext Context { get; private set; } = new GameContext();

    private const string UPGRADE_SPEED = "UP_Ship_Speed";
    private const string UPGRADE_ACCEL = "UP_Ship_Accel";
    private const string UPGRADE_MAX_FUEL = "UP_Ship_MaxFuel";
    private const string UPGRADE_CARGO = "UP_Ship_Cargo";
    private const string UPGRADE_FARM = "UP_Stat_Farm";
    private const string UPGRADE_REFINE = "UP_Stat_Refine";

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
        GameEvents.OnContinueRequested += HandleContinueRequested;
    }

    private void OnDisable()
    {
        GameEvents.OnStageClear -= HandleStageClear;
        GameEvents.OnStageFailed -= HandleStageFailed;
        GameEvents.OnStageSelected -= HandleStageSelected;
        GameEvents.OnContinueRequested -= HandleContinueRequested;
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
        Context.StageClearStatus[stageId] = true;

        if (!Context.UnlockedStageIds.Contains(stageId))
            Context.UnlockedStageIds.Add(stageId);
    }

    private void HandleStageFailed(string stageId)
    {
        Context.StageClearStatus[stageId] = false;
    }

    private void HandleContinueRequested()
    {
        SaveLoadManager.Instance.Load();
    }

    public void LoadContext(GameContext context)
    {
        Context = context;
        GameEvents.RaiseGoldChanged(Context.CurrentGold);
        GameEvents.RaiseIngotChanged(Context.CurrentIngot);
        Debug.Log("[GameManager] Context 로드 완료");
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

        if (Context.CurrentGold < goldCost) return false;
        if (Context.CurrentIngot < ingotCost) return false;

        TrySpendGold(goldCost);
        TrySpendIngot(ingotCost);

        int newLevel = currentLevel + 1;
        Context.UpgradeLevels[upgradeId] = newLevel;

        GameEvents.RaiseUpgradeCompleted(upgradeId, newLevel);

        RaiseStatsIfNeeded(upgradeId);

        return true;
    }

    private void RaiseStatsIfNeeded(string upgradeId)
    {
        switch (upgradeId)
        {
            case UPGRADE_SPEED:
            case UPGRADE_ACCEL:
            case UPGRADE_MAX_FUEL:
            case UPGRADE_CARGO:
                GameEvents.RaiseShipStatsChanged(SetShipStats());
                break;
            case UPGRADE_FARM:
            case UPGRADE_REFINE:
                GameEvents.RaiseStationStatsChanged(SetStationStats());
                break;
        }
    }

    public ShipStats SetShipStats()
    {
        return new ShipStats
        {
            BaseSpeed = GetUpgradeStat(UPGRADE_SPEED),
            BoostAcceleration = GetUpgradeStat(UPGRADE_ACCEL),
            MaxFuel = GetUpgradeStat(UPGRADE_MAX_FUEL),
            Capacity = Mathf.RoundToInt(GetUpgradeStat(UPGRADE_CARGO)),
        };
    }

    public StationStats SetStationStats()
    {
        return new StationStats
        {
            FarmRate = GetUpgradeStat(UPGRADE_FARM),
            RefineRate = GetUpgradeStat(UPGRADE_REFINE),
        };
    }


    public float GetUpgradeStat(string upgradeId)
    {
        var data = GameDataManager.Instance.Get<UpgradeData>(upgradeId);
        return data?.GetStat(GetUpgradeLevel(upgradeId)) ?? 0f;
    }

    public string GetUpgradeDesc(string upgradeId)
    {
        var desc = GameDataManager.Instance.Get<UpgradeData>(upgradeId);
        return desc?.Description ?? string.Empty;
    }

    public void RequestSFX(string soundId)
    {
        var data = GameDataManager.Instance.Get<SoundData>(soundId);
        if (data != null)
            GameEvents.RaiseSFXPlayRequested(data.SoundPath);
    }
}