using System;

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
            public const string Confirm = "UI_Popup_01";
            public const string ContinueInfo = "UI_Popup_02";
            public const string StageClear = "UI_Popup_03";
            public const string StageFailed = "UI_Popup_04";
            public const string StageStart = "UI_Popup_05";
            public const string PlanetInfo = "UI_Popup_06";
            public const string StationUpgrade = "UI_Popup_07";
            public const string StationMarket = "UI_Popup_08";

            public const string DebugMenu = "UI_Debug_01";
            
        }

        public struct VeryFront
        {
            public const string Loading = "UI_Loading_01";
        }
    }
}