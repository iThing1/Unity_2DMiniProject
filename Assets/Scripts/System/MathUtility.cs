using System;
using UnityEngine;

public static class MathUtility
{
    // =========================================================================
    // 숫자 표기
    // =========================================================================
    private static readonly string[] Suffixes = { "", "K", "M", "B", "T" };

    public static string ToAbbreviatedString(this float value, int digits = 1)
    {
        if (value <= 0) return "0";

        int suffixIndex = 0;
        double num = value;

        while (num >= 1000d && suffixIndex < Suffixes.Length - 1)
        {
            num /= 1000d;
            suffixIndex++;
        }

        return $"{num.ToString($"f{digits}")}{Suffixes[suffixIndex]}";
    }

    // =========================================================================
    // 공간 / 기하학
    // =========================================================================

    // 원형 궤도 위 랜덤 좌표 생성.
    public static (float x, float y) RandomPointOnRing(float minDist, float maxDist, float gradeWeight, float randomValue, float randomAngle)
    {
        float t = (float)Math.Pow(randomValue, gradeWeight > 0f ? gradeWeight : 0.5f);
        float distance = Lerp(minDist, maxDist, t);
        float angleRad = randomAngle * (float)(Math.PI / 180.0);

        return ((float)Math.Cos(angleRad) * distance, (float)Math.Sin(angleRad) * distance );
    }

    // 원형 궤도 위 폴백 좌표 생성.
    // 최대 시도 초과 시 고정 비율로 거리를 계산해 반환.
    public static (float x, float y) FallbackPointOnRing(
        float minDist, float maxDist, float gradeWeight,
        float randomAngle)
    {
        float distance = Lerp(minDist, maxDist, gradeWeight);
        float angleRad = randomAngle * (float)(Math.PI / 180.0);
        return (
            (float)Math.Cos(angleRad) * distance,
            (float)Math.Sin(angleRad) * distance
        );
    }


    // 두 원이 겹치는지 판별.
    public static bool IsCircleOverlapping(
        float px, float py, float radius,
        float ox, float oy, float otherRadius)
    {
        float dx = px - ox;
        float dy = py - oy;
        float minDist = radius + otherRadius;
        return dx * dx + dy * dy < minDist * minDist;
    }

    public static bool IsPointInRect(float x, float y, float halfWidth, float halfHeight)
    {
        return Math.Abs(x) <= halfWidth && Math.Abs(y) <= halfHeight;
    }

    // 선형 보간.
    public static float Lerp(float a, float b, float t)
    {
        t = Math.Max(0f, Math.Min(1f, t));
        return a + (b - a) * t;
    }

    // 초를 00:00로 변환하여 표기
    public static string FormatTime(float seconds)
    {
        int min = Mathf.FloorToInt(seconds / 60f);
        int sec = Mathf.FloorToInt(seconds % 60f);
        return $"{min:00}:{sec:00}";
    }
}