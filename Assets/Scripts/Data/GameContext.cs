using System;
using System.Collections.Generic;

namespace GameData
{
    [Serializable]
    public class StageClearRecord
    {
        public float ClearTime;
        public int GoldEarned;
        public int IngotEarned;
        public int Score;
        public string ClearedAt;
    }

    [Serializable]
    public class GameContext
    {
        public int CurrentGold;
        public int CurrentIngot;

        public string LastSelectedStageId;
        public List<string> UnlockedStageIds = new List<string>();

        public Dictionary<string, bool> StageClearStatus = new Dictionary<string, bool>();
        public Dictionary<string, int> UpgradeLevels = new Dictionary<string, int>();

        public SortedDictionary<string, List<StageClearRecord>> ClearRecords
            = new SortedDictionary<string, List<StageClearRecord>>();

        public string SavedAt;
        public int LastLoadedSlot = 0;
    }
}