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
        // 재화
        public int CurrentGold;
        public int CurrentIngot;

        // 스테이지
        public bool HasSeenTutorial = false;
        public string LastSelectedStageId;
        public List<string> UnlockedStageIds = new List<string>();
        public Dictionary<string, bool> StageClearStatus = new Dictionary<string, bool>();

        // 업그레이드
        public Dictionary<string, int> UpgradeLevels = new Dictionary<string, int>();

        // 클리어 기록
        public Dictionary<string, List<StageClearRecord>> ClearRecords 
            = new Dictionary<string, List<StageClearRecord>>();

        // 업적
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

        // 세이브/로드
        public string SavedAt;
        public int LastLoadedSlot = 0;
    }
}