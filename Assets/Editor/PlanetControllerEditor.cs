using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlanetController))]
public class PlanetControllerEditor : Editor
{
    // 인스펙터 자동 갱신 주기
    private const float REPAINT_INTERVAL = 0.1f;
    private double _lastRepaintTime;

    public override void OnInspectorGUI()
    {
        var planet = (PlanetController)target;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("플레이 모드에서 실시간 상태를 확인할 수 있습니다.", MessageType.Info);
            return;
        }
        if (string.IsNullOrEmpty(planet.InstanceId))
        {
            EditorGUILayout.HelpBox("Initialize() 호출 전입니다.\n아래 버튼으로 테스트 초기화를 실행하세요.", MessageType.Warning);
            DrawInitButton(planet);
            return;
        }

        // =========================================================================
        // 실시간 상태 표시
        // =========================================================================
        EditorGUILayout.LabelField("[ 행성 정보 ]", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("이름", planet.PlanetName);
        EditorGUILayout.LabelField("크기", planet.Size.ToString());
        EditorGUILayout.LabelField("Instance ID", planet.InstanceId);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("[ 런타임 상태 ]", EditorStyles.boldLabel);

        // 번영도 ? 단계 색상 표시
        Color prevColor = GUI.color;
        GUI.color = GetProsperityColor(planet.State);
        EditorGUILayout.LabelField("번영도 단계", planet.State.ToString());
        GUI.color = prevColor;

        // 번영도 Progress Bar
        Rect rect = EditorGUILayout.GetControlRect(false, 18f);
        float prosperity01 = Mathf.Clamp01(planet.Prosperity / 100f);
        EditorGUI.ProgressBar(rect, prosperity01, $"번영도: {planet.Prosperity:F1} / 100");

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("인구", $"{planet.Population:F0} 명");
        EditorGUILayout.LabelField("보유 식량", $"{planet.StoredFood:F1} 개");
        EditorGUILayout.LabelField("보유 광석", $"{planet.StoredOre:F1} 개");

        // 게임오버 경고 표시
        EditorGUILayout.Space(6);
        if (planet.IsGameOverWarning)
        {
            GUI.color = Color.red;
            EditorGUILayout.LabelField("? 멸망 위기! 유예 타이머 진행 중", EditorStyles.boldLabel);
            GUI.color = prevColor;
        }
        else
        {
            GUI.color = Color.green;
            EditorGUILayout.LabelField("? 정상 운영 중", EditorStyles.boldLabel);
            GUI.color = prevColor;
        }

        // =========================================================================
        // 테스트 버튼
        // =========================================================================
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("[ 테스트 ]", EditorStyles.boldLabel);

        // 식량 공급
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("식량 +10")) GameEvents.RaiseFoodDelivered(planet.InstanceId, 10);
        if (GUILayout.Button("식량 +50")) GameEvents.RaiseFoodDelivered(planet.InstanceId, 50);
        if (GUILayout.Button("식량 +100")) GameEvents.RaiseFoodDelivered(planet.InstanceId, 100);
        EditorGUILayout.EndHorizontal();

        // 광석 수거
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("광석 수거 -10")) GameEvents.RaiseOreCollected(planet.InstanceId, 10);
        if (GUILayout.Button("광석 수거 -50")) GameEvents.RaiseOreCollected(planet.InstanceId, 50);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // 번영도 강제 조작
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("번영도 0 강제\n(게임오버 테스트)"))
        {
            // ContextMenu 디버그 메서드와 동일한 흐름
            var method = typeof(PlanetController)
                .GetMethod("Debug_ForceDeath",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(planet, null);
        }
        if (GUILayout.Button("상태 로그 출력"))
        {
            var method = typeof(PlanetController)
                .GetMethod("Debug_PrintState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(planet, null);
        }
        EditorGUILayout.EndHorizontal();

        // =========================================================================
        // 자동 갱신 (0.1초마다 Repaint)
        // =========================================================================
        if (EditorApplication.timeSinceStartup - _lastRepaintTime > REPAINT_INTERVAL)
        {
            _lastRepaintTime = EditorApplication.timeSinceStartup;
            Repaint();
        }
    }

    // =========================================================================
    // 초기화 버튼 (Initialize 전 상태용)
    // =========================================================================
    private void DrawInitButton(PlanetController planet)
    {
        EditorGUILayout.Space(6);
        if (GUILayout.Button("테스트 초기화 (Planet_small_01)"))
        {
            // GameDataManager가 준비된 경우 실제 데이터로 초기화
            if (GameDataManager.Instance != null && GameDataManager.Instance.IsInitialized)
            {
                var data = GameDataManager.Instance.Get<GameData.PlanetData>("Planet_small_01");
                if (data != null)
                    planet.Initialize(data, "test_instance_01");
                else
                    Debug.LogWarning("[Editor] Planet_small_01 데이터를 찾을 수 없습니다.");
            }
            else
            {
                Debug.LogWarning("[Editor] GameManager가 씬에 있는지 확인하세요.");
            }
        }
    }

    // =========================================================================
    // 번영도 단계별 색상
    // =========================================================================
    private Color GetProsperityColor(PlanetState state)
    {
        switch (state)
        {
            case PlanetState.VeryProsperous: return Color.green;
            case PlanetState.Prosperous: return new Color(0.5f, 1f, 0.5f);
            case PlanetState.Neutral: return Color.white;
            case PlanetState.Poor: return new Color(1f, 0.6f, 0.2f);
            case PlanetState.Critical: return Color.red;
            case PlanetState.Destroyed: return Color.gray;
            default: return Color.white;
        }
    }
}