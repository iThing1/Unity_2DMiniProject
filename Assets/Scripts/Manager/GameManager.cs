using GameData;
using System.Threading.Tasks;
using UnityEngine;

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
        GameEventBus.Subscribe<string>(GameEventType.StageClear, HandleStageClear);
        GameEventBus.Subscribe<string>(GameEventType.StageFailed, HandleStageFailed);
        GameEventBus.Subscribe<string>(GameEventType.StageSelected, HandleStageSelected);
        GameEventBus.Subscribe(GameEventType.ContinueRequested, HandleContinueRequested);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<string>(GameEventType.StageClear, HandleStageClear);
        GameEventBus.Unsubscribe<string>(GameEventType.StageFailed, HandleStageFailed);
        GameEventBus.Unsubscribe<string>(GameEventType.StageSelected, HandleStageSelected);
        GameEventBus.Unsubscribe(GameEventType.ContinueRequested, HandleContinueRequested);
    }

    private async void Start()
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await GameDataManager.Instance.RegisterAllTables();
        await UIManager.Instance.LoadUIPrefabsAsync();

        GameEventBus.Publish(GameEventType.DataInitialized);
        ChangeState(GameState.MainMenu);
        PreloadPlanetSprites();
    }

    private void PreloadPlanetSprites()
    {
        ResourceManager.Instance.LoadSpriteFromSheet("Planet", "Planet_0", OnPlanetSpritesLoaded);
    }

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

        GameEventBus.Publish(GameEventType.GameStateChanged, prev, newState);
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
        SaveLoadController.LoadCurrentGame();
    }

    public void LoadContext(GameContext context)
    {
        Context = context;
        GameEventBus.Publish(GameEventType.GoldChanged, Context.CurrentGold);
        GameEventBus.Publish(GameEventType.IngotChanged, Context.CurrentIngot);
        Debug.Log("[GameManager] Context 로드 완료");
    }

    // =========================================================================
    // 재화 관련
    // =========================================================================

    public void AddGold(float amount)
    {
        Context.CurrentGold += amount;
        GameEventBus.Publish(GameEventType.GoldChanged, Context.CurrentGold);
    }

    public bool TrySpendGold(float amount)
    {
        if (Context.CurrentGold < amount) return false;
        Context.CurrentGold -= amount;
        GameEventBus.Publish(GameEventType.GoldChanged, Context.CurrentGold);
        return true;
    }

    public void AddIngot(float amount)
    {
        Context.CurrentIngot += amount;
        GameEventBus.Publish(GameEventType.IngotChanged, Context.CurrentIngot);
    }

    public bool TrySpendIngot(float amount)
    {
        if (Context.CurrentIngot < amount) return false;
        Context.CurrentIngot -= amount;
        GameEventBus.Publish(GameEventType.IngotChanged, Context.CurrentIngot);
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

        GameEventBus.Publish(GameEventType.UpgradeCompleted, upgradeId, newLevel);
        RaiseStatsIfNeeded(upgradeId);

        return true;
    }

    private void RaiseStatsIfNeeded(string upgradeId)
    {
        switch (upgradeId)
        {
            case Upgrade.StatSpeed:
            case Upgrade.StatAccel:
            case Upgrade.StatMaxFuel:
            case Upgrade.StatCargo:
                GameEventBus.Publish(GameEventType.ShipStatsChanged, SetShipStats());
                break;

            case Upgrade.StatFarm:
            case Upgrade.StatRefine:
                GameEventBus.Publish(GameEventType.StationStatsChanged, SetStationStats());
                break;
        }
    }

    public ShipStats SetShipStats()
    {
        return new ShipStats
        {
            BaseSpeed = GetUpgradeStat(Upgrade.StatSpeed),
            BoostAcceleration = GetUpgradeStat(Upgrade.StatAccel),
            MaxFuel = GetUpgradeStat(Upgrade.StatMaxFuel),
            Capacity = Mathf.RoundToInt(GetUpgradeStat(Upgrade.StatCargo)),
        };
    }

    public StationStats SetStationStats()
    {
        return new StationStats
        {
            FarmRate = GetUpgradeStat(Upgrade.StatFarm),
            RefineRate = GetUpgradeStat(Upgrade.StatRefine),
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
            GameEventBus.Publish(GameEventType.SFXPlayRequested, data.SoundPath);
    }
}