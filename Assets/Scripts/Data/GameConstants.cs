using System;

namespace GameData
{
    public struct GameConstants
    {
        // 우주선
        public float FuelConsumeRate;
        public float FuelRegenRate;
        public float OverheatDuration;
        public float ShipAcceleration;
        public float ShipBoostAccel;
        public float DockingSpeed;

        // 행성
        public float ProsperityChangeRate;
        public float ProsperityIncreaseMax;
        public float PopulationChangeRate;
        public float PopulationIncreaseMax;
        public float PlanetGameoverTime;
        public float PlanetConsumeInterval;
        public float PlanetProsperityMax;

        // 정거장 / 화물
        public float CargoTransferInterval;
        public float OreToIngotRatio;
        public float StationDockingRange;

        // 행성 생산/소비 계수
        public float FoodConsumeBase;
        public float FoodConsumeRate;
        public float OreProdBase;
        public float OreProdRate;
    }

    public struct GameSetting
    {
        public float CameraHeightDefault;
        public float CameraShakePower;
        public float SoundBackgroundVolume;
        public float SoundEffectVolume;
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
    public class GameSettingData : IGameData
    {
        public string Id;
        public float DefaultValue;
        public string Desc;

        string IGameData.Id => Id;
    }  
}