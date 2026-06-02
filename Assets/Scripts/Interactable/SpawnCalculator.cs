using System.Collections.Generic;
using UnityEngine;

// 스폰 위치 계산 전담 클래스.
public static class SpawnCalculator
{
    public static Vector3 FindSpawnPosition(
        int grade, int maxGrade,
        float colliderRadius,
        float spawnPadding,
        float stationExclusionRange,
        int maxAttempts,
        CameraController cameraController,
        Collider2D stationCollider,
        IList<PlanetController> spawnedPlanets)
    {
        float maxDistance = GetSpawnRange(colliderRadius, spawnPadding, cameraController);
        float minDistance = colliderRadius + stationExclusionRange;
        float gradeWeight = (float)grade / maxGrade;

        float bestDist = -1f;
        (float x, float y) bestCandidate = MathUtility.FallbackPointOnRing(
            minDistance, maxDistance, gradeWeight, Random.Range(0f, 360f)
        );

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var (cx, cy) = MathUtility.RandomPointOnRing(
                minDistance, maxDistance, gradeWeight,
                Random.value, Random.Range(0f, 360f)
            );

            if (!IsOverlapping(cx, cy, colliderRadius, spawnPadding, stationExclusionRange, cameraController, stationCollider, spawnedPlanets))
                return new Vector3(cx, cy, 0f);

            // 겹침이더라도 정거장으로부터 거리 기록 (fallback 후보)
            float distFromStation = GetDistanceFromStation(cx, cy, stationCollider);

            if (distFromStation > bestDist)
            {
                bestDist = distFromStation;
                bestCandidate = (cx, cy);
            }
        }

        Debug.LogWarning("[SpawnCalculator] fallback: 최대 시도 초과, 정거장과 가장 먼 후보 위치로 소환");
        return new Vector3(bestCandidate.x, bestCandidate.y, 0f);
    }

    // 프리팹 Collider2D 반지름 추출 (로컬 스케일 기준).
    public static float GetPrefabColliderRadius(GameObject prefab)
    {
        Collider2D col = prefab.GetComponent<Collider2D>();
        if (col == null) return 1f;

        float scale = prefab.transform.localScale.x;
        if (col is CircleCollider2D circle)
            return circle.radius * scale;
        if (col is BoxCollider2D box)
            return Mathf.Max(box.size.x, box.size.y) * 0.5f * scale;
        return 1f * scale;
    }

    // =========================================================================
    // 내부 계산
    // =========================================================================
    private static float GetSpawnRange(float colliderRadius, float spawnPadding, CameraController cameraController)
    {
        if (cameraController != null)
            return cameraController.ViewHalfWidth - colliderRadius - spawnPadding;

        float defaultSize = GameConfig.Instance.Settings.CameraHeightDefault;
        return defaultSize - colliderRadius - spawnPadding;
    }

    private static bool IsOverlapping(
        float cx, float cy, float colliderRadius,
        float spawnPadding, float stationExclusionRange,
        CameraController cameraController,
        Collider2D stationCollider,
        IList<PlanetController> spawnedPlanets)
    {
        // 카메라 시야 밖 체크
        if (cameraController != null)
        {
            float halfW = cameraController.ViewHalfWidth - colliderRadius - spawnPadding;
            float halfH = cameraController.ViewHalfHeight - colliderRadius - spawnPadding;
            if (!MathUtility.IsPointInRect(cx, cy, halfW, halfH))
                return true;
        }

        // 기존 행성과 겹침 체크
        foreach (PlanetController planet in spawnedPlanets)
        {
            if (planet == null) continue;

            Collider2D col = planet.GetComponent<Collider2D>();
            if (col == null) continue;

            float otherRadius = GetColliderRadius(col);
            Vector3 pos = planet.transform.position;

            if (MathUtility.IsCircleOverlapping(cx, cy, colliderRadius, pos.x, pos.y, otherRadius))
                return true;
        }

        // 정거장 제외 범위 체크
        if (stationCollider != null)
        {
            float checkRadius = colliderRadius + stationExclusionRange;
            Vector2 closestPoint = stationCollider.ClosestPoint(new Vector2(cx, cy));
            float dx = cx - closestPoint.x;
            float dy = cy - closestPoint.y;
            if (dx * dx + dy * dy < checkRadius * checkRadius)
                return true;
        }

        return false;
    }

    private static bool IsCameraInside(float cx, float cy, float colliderRadius, float spawnPadding, CameraController cameraController)
    {
        if (cameraController == null) return true;

        float halfW = cameraController.ViewHalfWidth - colliderRadius - spawnPadding;
        float halfH = cameraController.ViewHalfHeight - colliderRadius - spawnPadding;
        return MathUtility.IsPointInRect(cx, cy, halfW, halfH);
    }

    private static float GetColliderRadius(Collider2D col)
    {
        float scale = col.transform.lossyScale.x;
        if (col is CircleCollider2D circle)
            return circle.radius * scale;
        if (col is CapsuleCollider2D capsule)
            return Mathf.Min(capsule.size.x, capsule.size.y) * 0.5f * scale;
        if (col is BoxCollider2D box)
            return Mathf.Max(box.size.x, box.size.y) * 0.5f * scale;
        return col.bounds.extents.magnitude;
    }

    private static float GetDistanceFromStation(float cx, float cy, Collider2D stationCollider)
    {
        if (stationCollider == null)
            return Mathf.Sqrt(cx * cx + cy * cy);

        Vector2 closestPoint = stationCollider.ClosestPoint(new Vector2(cx, cy));
        float dx = cx - closestPoint.x;
        float dy = cy - closestPoint.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}