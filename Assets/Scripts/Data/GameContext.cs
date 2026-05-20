using System;
using System.Collections.Generic;

namespace GameData
{
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
}