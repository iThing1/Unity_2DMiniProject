using GameData;
using System;
using System.Collections.Generic;
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

    private float _stageStartTime;
    private int _stageEarnedGold;
    private int _stageEarnedIngot;

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
    }

    private void OnDisable()
    {
        GameEventBus.Unsubscribe<string>(GameEventType.StageClear, HandleStageClear);
        GameEventBus.Unsubscribe<string>(GameEventType.StageFailed, HandleStageFailed);
        GameEventBus.Unsubscribe<string>(GameEventType.StageSelected, HandleStageSelected);
    }

    private async void Start()
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await GameDataManager.Instance.RegisterAllTables();
        await UIManager.Instance.LoadUIPrefabsAsync();
        await SoundManager.Instance.SetUp();

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

        string prevBindState = StateToString(CurrentState);
        UIManager.Instance.SetUIByBindState(prevBindState, false);

        GameState prev = CurrentState;
        CurrentState = newState;

        string nextBindState = StateToString(CurrentState);
        UIManager.Instance.SetUIByBindState(nextBindState, true);

        bool showCurrency = (newState == GameState.Lobby || newState == GameState.GamePlay);
        UIManager.Instance.ShowCurrencyUI(showCurrency);

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
            _stageStartTime = Time.realtimeSinceStartup;
            _stageEarnedGold = 0;
            _stageEarnedIngot = 0;

            CheckStageClear();
        }

        GameEventBus.Publish(GameEventType.GameStateChanged, prev, newState);
        UpdateBGMForState(prev, newState);
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

        int goldEarned = _stageEarnedGold;
        int ingotEarned = _stageEarnedIngot;

        StageClearRecord record = new StageClearRecord
        {
            ClearTime = Time.realtimeSinceStartup - _stageStartTime,
            GoldEarned = goldEarned,
            IngotEarned = ingotEarned,
            Score = goldEarned * 1 + ingotEarned * 10,
            ClearedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };

        if (!Context.ClearRecords.ContainsKey(stageId))
            Context.ClearRecords[stageId] = new List<StageClearRecord>();

        Context.ClearRecords[stageId].Add(record);

        Time.timeScale = 0f;
        StageClear stageClear = UIManager.Instance.PrepareUI<StageClear>(UIId.Popup.StageClear);
        if (stageClear != null)
        {
            stageClear.Setup(record);
            UIManager.Instance.OpenUI(UIId.Popup.StageClear);
        }
    }

    private void HandleStageFailed(string stageId)
    {
        Context.StageClearStatus[stageId] = false;

        Time.timeScale = 0f;
        UIManager.Instance.OpenUI(UIId.Popup.StageFailed);
    }

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
    // 재화 관련
    // =========================================================================

    public void AddGold(int amount)
    {
        Context.CurrentGold += amount;
        GameEventBus.Publish(GameEventType.GoldChanged, Context.CurrentGold);
        if (CurrentState == GameState.GamePlay)
        {
            _stageEarnedGold += amount;
            GameEventBus.Publish(GameEventType.GoldChanged, Context.CurrentGold);
            CheckStageClear();
        }   
    }

    public bool TrySpendGold(int amount)
    {
        if (Context.CurrentGold < amount) return false;
        Context.CurrentGold -= amount;
        GameEventBus.Publish(GameEventType.GoldChanged, Context.CurrentGold);
        return true;
    }

    public void AddIngot(int amount)
    {
        Context.CurrentIngot += amount;
        GameEventBus.Publish(GameEventType.IngotChanged, Context.CurrentIngot);
        if (CurrentState == GameState.GamePlay)
        {
            _stageEarnedIngot += amount;
        }
    }

    public bool TrySpendIngot(int amount)
    {
        if (Context.CurrentIngot < amount) return false;
        Context.CurrentIngot -= amount;
        GameEventBus.Publish(GameEventType.IngotChanged, Context.CurrentIngot);
        return true;
    }

    private void CheckStageClear()
    {
        if (CurrentState != GameState.GamePlay) return;

        string stageId = Context.LastSelectedStageId;
        if (string.IsNullOrEmpty(stageId)) return;

        StageData data = GameDataManager.Instance.Get<StageData>(stageId);
        if (data == null) return;

        if (Context.CurrentGold >= data.ReqGold)
            GameEventBus.Publish(GameEventType.StageClearCondition);
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

    private void UpdateBGMForState(GameState prev, GameState next)
    {
        if (prev == GameState.GamePlay)
            SoundManager.Instance.StopBGM();

        string bindState;
        switch (next)
        {
            case GameState.MainMenu:
                bindState = "MainMenu";
                break;
            case GameState.Lobby:
                bindState = "Lobby";
                break;
            case GameState.GamePlay:
                bindState = "GamePlay";
                break;
            default:
                bindState = null;
                break;
        }

        if (bindState == null) return;

        var candidates = new List<string>();
        foreach (var sound in GameDataManager.Instance.GetAll<SoundData>())
        {
            if (sound.Type == SoundType.BGM && sound.BindState == bindState)
                candidates.Add(sound.SoundPath);
        }

        if (candidates.Count > 0)
        {
            string selectedBgm = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            SoundManager.Instance.PlayBGM(selectedBgm);
        }
        else
        {
            Debug.LogWarning($"[GameManager] '{bindState}'에 바인딩된 BGM 없음");
        }
    }

    private string StateToString(GameState state)
    {
        switch (state)
        {
            case GameState.Loading: return "Loading";
            case GameState.MainMenu: return "MainMenu";
            case GameState.Lobby: return "Lobby";
            case GameState.GamePlay: return "GamePlay";
            default: return null;
        }
    }
}