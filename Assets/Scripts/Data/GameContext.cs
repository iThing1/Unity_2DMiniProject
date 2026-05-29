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

        public bool UpdateAchievementProgress(string achievementId, int addValue)
        {
            if (IsAchievementCompleted(achievementId)) return false;

            AchievementData data = GameDataManager.Instance.Get<AchievementData>(achievementId);
            if (data == null) return false;

            if (data.AchievementType == AchievementType.Stacked && (data.TargetValue == null || data.TargetValue <= 0))
            {
                return false;
            }

            if (!AchievementProgress.ContainsKey(achievementId))
                AchievementProgress[achievementId] = 0;

            AchievementProgress[achievementId] += addValue;

            if (AchievementProgress[achievementId] >= (data.TargetValue ?? 0))
            {
                AchievementStatus[achievementId] = true;
                AchievementScore += data.Score;
                return true;
            }

            return false;
        }

        public bool CompleteAchievement(string achievementId)
        {
            if (IsAchievementCompleted(achievementId)) return false;

            AchievementStatus[achievementId] = true;
            AchievementData data = GameDataManager.Instance.Get<AchievementData>(achievementId);
            if (data != null)
                AchievementScore += data.Score;

            return true;
        }

        public string SavedAt;
        public int LastLoadedSlot = 0;
    }
}