using System;
using System.Collections.Generic;

namespace GameData
{
    public interface IGameData
    {
        string Id { get; }
    }

    public enum PlanetSize { None, Small, Medium, Large, Super }
    public enum CalcType { Sum, Multi, }
    public enum GameState { Loading, MainMenu, Lobby, GamePlay }
    public enum UIType { Popup, Main, VeryFront }
    public enum SoundType { BGM, SFX }

    public struct UIId
    {
        public struct Panel
        {
            public const string MainMenu = "UI_Panel_01";
            public const string GamePlay = "UI_Panel_02";
            public const string GameLobby = "UI_Panel_03";
            public const string Upgrade = "UI_Panel_04";
        }

        public struct Popup
        {
            public const string StageStart = "UI_Popup_05";
            public const string StageClear = "UI_Popup_03";
            public const string StageFailed = "UI_Popup_04";
            public const string PlanetInfo = "UI_Popup_06";
            public const string RefinerUpgrade = "UI_Popup_07";
            public const string FarmUpgrade = "UI_Popup_08";
        }

        public struct VeryFront
        {
            public const string Loading = "UI_Loading_01";
        }
    }

    [Serializable]
    public class GameContext
    {
        public float CurrentGold;
        public float CurrentIngot;

        public string LastSelectedStageId;
        public List<string> UnlockedStageIds = new List<string>();

        public Dictionary<string, bool> StageClearStatus = new Dictionary<string, bool>();
        public Dictionary<string, int> UpgradeLevels = new Dictionary<string, int>();
    }

    [Serializable]
    public class GameSettingData : IGameData
    {
        public string Id;
        public float DefaultValue;
        public string Desc;

        string IGameData.Id => Id;
    }

    [Serializable]
    public class GameConstantData : IGameData
    {
        public string Id;
        public float Value;
        public string Desc;

        string IGameData.Id => Id;
    }

    [Serializable]
    public class PlanetData : IGameData
    {
        public string Id = null!;
        public PlanetSize Size;
        public string Name = null!;
        public int BasePop;
        public int BaseFood;
        public int BaseOre;
        public int Property;
        public int Grade;
        public string PlanetSprite = null!;

        string IGameData.Id => Id;
    }

    [Serializable]
    public class StageData : IGameData
    {
        public string Stage_ID = null!;
        public string Name = null!;
        public int MaxPlanet;
        public float SpawnInteval;
        public float ExpansionSpeed;
        public int ReqGold;
        public bool isClear;
        public string PlanetList = null!;

        public string Id => Stage_ID;

        public string[] GetPlanetList()
        {
            if (string.IsNullOrEmpty(PlanetList)) return Array.Empty<string>();
            return PlanetList.Split(',');
        }
    }

    [Serializable]
    public class UpgradeData : IGameData
    {
        public string Id = null!;
        public string Target = null!;
        public string UpgradeType = null!;
        public string Name = null!;
        public int MaxLevel;
        public int BaseGoldCost;
        public int CostIncrease;
        public int BaseIngotCost;
        public int IngotIncrease;
        public float BaseStats;
        public float UpgradeValue;
        public CalcType CalcType;
        public string Description = null!;

        string IGameData.Id => Id;

        public float GetStat(int level)
        {
            switch (CalcType)
            {
                case CalcType.Sum: return BaseStats + UpgradeValue * level;
                case CalcType.Multi: return BaseStats * MathF.Pow(1f + UpgradeValue / 100f, level);
                default: return BaseStats;
            }
        }
    }

    [Serializable]
    public class SoundData : IGameData
    {
        public string Id = null!;
        public string Name = null!;
        public SoundType Type;
        public string BindState = null!;
        public string SoundPath = null!; 

        string IGameData.Id => Id;
    }

    [Serializable]
    public class UIData : IGameData
    {
        public string Id = null!;
        public string Name = null!;
        public UIType Type;
        public string BindState = null!;
        public bool AutoSpawn;

        string IGameData.Id => Id;
    }
}