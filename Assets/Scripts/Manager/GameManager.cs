using GameData;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Spawner")]
    [SerializeField] private StationSpawner _stationSpawner;
    [SerializeField] private ShipSpawner _shipSpawner;
    [SerializeField] private PlanetSpawner _planetSpawner;
    
    public GameState CurrentState { get; private set; } = GameState.Loading;
    public GameContext Context { get; private set; } = new GameContext();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEventBus.Subscribe<string>(GameEventType.AchievementCompleted, HandleAchievementCompleted);
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<string>(GameEventType.AchievementCompleted, HandleAchievementCompleted);
    }

    private async void Start()
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await GameDataManager.Instance.RegisterAllTables();
        GameConfig.Instance.Initialize();
        await UIManager.Instance.LoadUIPrefabsAsync();
        await SoundManager.Instance.SetUp();

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

    // =========================================================================
    // 상태 전환
    // =========================================================================
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        string prevBindState = GameUtility.StateToString(CurrentState);
        UIManager.Instance.SetUIByBindState(prevBindState, false);

        GameState prev = CurrentState;
        CurrentState = newState;

        string nextBindState = GameUtility.StateToString(CurrentState);
        UIManager.Instance.SetUIByBindState(nextBindState, true);

        if (prev == GameState.GamePlay)
        {
            if (_stationSpawner != null) _stationSpawner.OnExitGamePlay();
            if (_shipSpawner != null) _shipSpawner.OnExitGamePlay();
            if (_planetSpawner != null) _planetSpawner.OnExitGamePlay();
            SaveCurrentGame();
        }

        if (newState == GameState.GamePlay)
        {
            if (_stationSpawner != null) _stationSpawner.OnEnterGamePlay();
            StageManager.Instance.OnEnterGamePlay();
        }

        GameEventBus.Publish(GameEventType.GameStateChanged, prev, newState);
    }

    public void StartGamePlay() => ChangeState(GameState.GamePlay);
    public void ReturnToLobby() => ChangeState(GameState.Lobby);

    // =========================================================================
    // 저장 / 로드
    // =========================================================================
    public void SaveCurrentGame()
    {
        SaveLoadController.SaveSlot(Context.LastLoadedSlot, Context);
    }

    public bool LoadGame(int slotIndex)
    {
        GameContext context = SaveLoadController.LoadSlot(slotIndex);
        if (context == null) return false;

        context.LastLoadedSlot = slotIndex;
        LoadContext(context);
        return true;
    }

    public void ResetContext(int slotIndex)
    {
        Context = new GameContext();
        Context.LastLoadedSlot = slotIndex;
        GameEventBus.Publish(GameEventType.GoldChanged, Context.CurrentGold);
        GameEventBus.Publish(GameEventType.IngotChanged, Context.CurrentIngot);
    }

    public void LoadContext(GameContext context)
    {
        Context = context;
        GameEventBus.Publish(GameEventType.GoldChanged, Context.CurrentGold);
        GameEventBus.Publish(GameEventType.IngotChanged, Context.CurrentIngot);
    }

    // =========================================================================
    // 이벤트 핸들러
    // =========================================================================
    private void HandleAchievementCompleted(string achievementId)
    {
        SaveCurrentGame();
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

        int goldCost = data.BaseGoldCost + data.CostIncrease * currentLevel;
        int ingotCost = data.BaseIngotCost + data.IngotIncrease * currentLevel;

        if (Context.CurrentGold < goldCost) return false;
        if (Context.CurrentIngot < ingotCost) return false;

        CurrencyManager.Instance.TrySpendGold(goldCost);
        CurrencyManager.Instance.TrySpendIngot(ingotCost);

        Context.UpgradeLevels[upgradeId] = currentLevel + 1;

        GameEventBus.Publish(GameEventType.UpgradeCompleted, upgradeId);
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
            case Upgrade.StatFuelRegen:
            case Upgrade.StatDockingSpeed:
            case Upgrade.StatLoaderSpeed:
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
            FuelRegenRate = GetUpgradeStat(Upgrade.StatFuelRegen),
            DockingSpeedThreshold = GetUpgradeStat(Upgrade.StatDockingSpeed),
            LoaderSpeed = GetUpgradeStat(Upgrade.StatLoaderSpeed),
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
}