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

        public bool HasSeenTutorial = false;
        public string LastSelectedStageId;
        public List<string> UnlockedStageIds = new List<string>();

        public Dictionary<string, bool> StageClearStatus = new Dictionary<string, bool>();
        public Dictionary<string, int> UpgradeLevels = new Dictionary<string, int>();

        public Dictionary<string, List<StageClearRecord>> ClearRecords 
            = new Dictionary<string, List<StageClearRecord>>();

        public int AchievementScore;

        public Dictionary<string, bool> AchievementStatus = new Dictionary<string, bool>();
        public Dictionary<string, int> AchievementProgress = new Dictionary<string, int>();

        public bool IsAchievementCompleted(string achievementId)
        {
            return AchievementStatus.TryGetValue(achievementId, out bool completed) && completed;
        }

        public int GetAchievementProgress(string achievementId)
        {
            return AchievementProgress.TryGetValue(achievementId, out int value) ? value : 0;
        }

        public string SavedAt;
        public int LastLoadedSlot = 0;
    }
}