using System;
using System.Collections.Generic;

public enum GameEventType
{
    None = 0,

    // ==========================================
    // [시스템 & 상태] 100번대
    // ==========================================
    DataInitialized = 100,
    GameStateChanged = 101,
    NewGameRequested = 102,
    ContinueRequested = 103,
    AchievementCompleted = 104,
    // ==========================================
    // [사운드] 200번대
    // ==========================================
    SFXPlayRequested = 200,

    // ==========================================
    // [자원 & 화물] 300번대
    // ==========================================
    GoldChanged = 300,
    IngotChanged = 301,
    CargoChanged = 302,
    FoodDelivered = 303,
    IngotRefined = 304,

    // ==========================================
    // [성장 & 스탯] 400번대
    // ==========================================
    UpgradeCompleted = 400,
    ShipStatsChanged = 401,
    StationStatsChanged = 402,

    // ==========================================
    // [스테이지] 500번대
    // ==========================================
    StageSelected = 500,
    StageStartRequested = 501,
    StageClear = 502,
    StageFailed = 503,
    StageClearCondition = 504,
    // ==========================================
    // [행성] 600번대
    // ==========================================
    PlanetWarning = 600,
    PlanetDestroyed = 601,
    PlanetHovered = 602,

    // ==========================================
    // [우주 정거장] 700번대
    // ==========================================
    StationInteractionChanged = 700,

    // ==========================================
    // [스폰 관련] 800번대
    // ==========================================
    StationSpawned = 800,
    ShipSpawned = 801,
    PlanetSpawned = 802
}

public static class GameEventBus
{
    private static readonly Dictionary<GameEventType, Delegate> _events = new Dictionary<GameEventType, Delegate>();

    // ==========================================
    // 파라미터 0개짜리
    // ==========================================
    public static void Subscribe(GameEventType eventType, Action listener)
    {
        if (_events.TryGetValue(eventType, out var existing))
            _events[eventType] = Delegate.Combine(existing, listener);
        else
            _events[eventType] = listener;
    }

    public static void Unsubscribe(GameEventType eventType, Action listener)
    {
        if (_events.TryGetValue(eventType, out var existing))
        {
            var current = Delegate.Remove(existing, listener);
            if (current == null) _events.Remove(eventType);
            else _events[eventType] = current;
        }
    }

    public static void Publish(GameEventType eventType)
    {
        if (_events.TryGetValue(eventType, out var existing))
            (existing as Action)?.Invoke();
    }

    // ==========================================
    // 파라미터 1개짜리
    // ==========================================
    public static void Subscribe<T>(GameEventType eventType, Action<T> listener)
    {
        if (_events.TryGetValue(eventType, out var existing))
        {
            _events[eventType] = Delegate.Combine(existing, listener);
        }
        else
        {
            _events[eventType] = listener;
        }
    }

    public static void Unsubscribe<T>(GameEventType eventType, Action<T> listener)
    {
        if (_events.TryGetValue(eventType, out var existing))
        {
            var current = Delegate.Remove(existing, listener);
            if (current == null)
                _events.Remove(eventType);
            else
                _events[eventType] = current;
        }
    }

    public static void Publish<T>(GameEventType eventType, T eventData)
    {
        if (_events.TryGetValue(eventType, out var existing))
        {
            (existing as Action<T>)?.Invoke(eventData);
        }
    }

    // ==========================================
    // 파라미터 2개짜리
    // ==========================================
    public static void Subscribe<T1, T2>(GameEventType eventType, Action<T1, T2> listener)
    {
        if (_events.TryGetValue(eventType, out var existing))
            _events[eventType] = Delegate.Combine(existing, listener);
        else
            _events[eventType] = listener;
    }

    public static void Unsubscribe<T1, T2>(GameEventType eventType, Action<T1, T2> listener)
    {
        if (_events.TryGetValue(eventType, out var existing))
        {
            var current = Delegate.Remove(existing, listener);
            if (current == null) _events.Remove(eventType);
            else _events[eventType] = current;
        }
    }

    public static void Publish<T1, T2>(GameEventType eventType, T1 arg1, T2 arg2)
    {
        if (_events.TryGetValue(eventType, out var existing))
            (existing as Action<T1, T2>)?.Invoke(arg1, arg2);
    }
}