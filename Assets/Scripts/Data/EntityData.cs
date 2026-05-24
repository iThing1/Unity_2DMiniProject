using System;

namespace GameData
{
    public static class Upgrade
    {
        public const string StatCargo = "UP_Ship_Cargo";
        public const string StatSpeed = "UP_Ship_Speed";
        public const string StatAccel = "UP_Ship_Accel";
        public const string StatMaxFuel = "UP_Ship_MaxFuel";
        public const string StatFarm = "UP_Stat_Farm";
        public const string StatRefine = "UP_Stat_Refine";
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
        public int Properity;
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
        public float SpawnInterval;
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
        public string SpritePath = null!;
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
        public bool Auto;

        string IGameData.Id => Id;
    }
}