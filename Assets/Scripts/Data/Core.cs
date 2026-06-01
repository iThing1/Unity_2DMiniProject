using System;
using System.Runtime.CompilerServices;

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

    public enum AchievementType
    {
        Stacked,
        Acomplished
    }

    public enum TriggerType
    {
        None,
        FoodDelivered,
        IngotRefined,
        StageClear,
    }

    public static class GameUtility
    {
        public static string StateToString(GameState state)
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

    public struct UIId
    {
        public struct Panel
        {
            public const string MainMenu = "UI_Panel_01";
            public const string GamePlay = "UI_Panel_02";
            public const string GameLobby = "UI_Panel_03";
            public const string Upgrade = "UI_Panel_04";
            public const string Achievement = "UI_Panel_05";
            public const string Currency = "UI_Panel_06";
            public const string Setting = "UI_Panel_07";
        }

        public struct Popup
        {
            public const string Confirm = "UI_Popup_01";
            public const string ContinueInfo = "UI_Popup_02";
            public const string StageClear = "UI_Popup_03";
            public const string StageFailed = "UI_Popup_04";
            public const string StageStart = "UI_Popup_05";
            public const string PlanetInfo = "UI_Popup_06";
            public const string StationUpgrade = "UI_Popup_07";
            public const string StationMarket = "UI_Popup_08";
            public const string Scoreboard = "UI_Popup_09";
            public const string Option = "UI_Popup_10";

            public const string HowToMove = "UI_Tutorial_02";
            public const string HowToLoop = "UI_Tutorial_03";

            public const string DebugMenu = "UI_Debug_01";
            
        }

        public struct VeryFront
        {
            public const string Loading = "UI_Loading_01";
            public const string Intro = "UI_Intro_01";
            public const string TutorialOverlay = "UI_Tutorial_01";
        }
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

    [Serializable]
    public class AchievementData : IGameData
    {
        public string Id = null!;
        public string Name = null!;
        public string Description = null!;
        public AchievementType AchievementType;
        public TriggerType TriggerType;
        public int? TargetValue;
        public int Score;
        public string StageId;
        public string NextId;
        public string SpritePath;

        string IGameData.Id => Id;
    }

    [Serializable]
    public class IntroData : IGameData
    {
        public string Id = null!;
        public string Message = null!;
        public string BackgroundPath = null!;

        string IGameData.Id => Id;
    }
}