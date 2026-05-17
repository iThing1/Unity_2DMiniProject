using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlanetController))]
public class PlanetControllerEditor : Editor
{
    private void OnEnable()
    {
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Repaint;
    }

    public override void OnInspectorGUI()
    {
        var planet = (PlanetController)target;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play Mode에서 실시간 상태를 확인할 수 있습니다.", MessageType.Info);
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
        EditorGUILayout.LabelField("[ Planet Info ]", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Name", planet.PlanetName);
        EditorGUILayout.LabelField("Size", planet.Size.ToString());
        EditorGUILayout.LabelField("Instance ID", planet.InstanceId);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("[ Runtime State ]", EditorStyles.boldLabel);

        Color prevColor = GUI.color;
        GUI.color = GetProsperityColor(planet.State);
        EditorGUILayout.LabelField("State", planet.State.ToString());
        GUI.color = prevColor;

        Rect rect = EditorGUILayout.GetControlRect(false, 18f);
        float prosperity01 = Mathf.Clamp01(planet.Prosperity / 100f);
        EditorGUI.ProgressBar(rect, prosperity01, $"Prosperity: {planet.Prosperity:F1} / 100");

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Population", $"{planet.Population:F0}");
        EditorGUILayout.LabelField("Food", $"{planet.StoredFood:F1}");
        EditorGUILayout.LabelField("Ore", $"{planet.StoredOre:F1}");

        EditorGUILayout.Space(6);
        if (planet.IsGameOverWarning)
        {
            GUI.color = Color.red;
            EditorGUILayout.LabelField("!! Game Over Warning !!", EditorStyles.boldLabel);
            GUI.color = prevColor;
        }
        else
        {
            GUI.color = Color.green;
            EditorGUILayout.LabelField("Normal", EditorStyles.boldLabel);
            GUI.color = prevColor;
        }

        // =========================================================================
        // 테스트 버튼
        // =========================================================================
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("[ Test ]", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Food +10")) GameEvents.RaiseFoodDelivered(planet.InstanceId, 10);
        if (GUILayout.Button("Food +50")) GameEvents.RaiseFoodDelivered(planet.InstanceId, 50);
        if (GUILayout.Button("Food +100")) GameEvents.RaiseFoodDelivered(planet.InstanceId, 100);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Ore -10")) GameEvents.RaiseOreCollected(planet.InstanceId, 10);
        if (GUILayout.Button("Ore -50")) GameEvents.RaiseOreCollected(planet.InstanceId, 50);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Force Prosperity 0\n(GameOver Test)"))
        {
            var method = typeof(PlanetController).GetMethod("Debug_ForceDeath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(planet, null);
        }
        if (GUILayout.Button("Print State Log"))
        {
            var method = typeof(PlanetController).GetMethod("Debug_PrintState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(planet, null);
        }
        EditorGUILayout.EndHorizontal();
    }

    // =========================================================================
    // 초기화 버튼
    // =========================================================================
    private void DrawInitButton(PlanetController planet)
    {
        EditorGUILayout.Space(6);
        if (GUILayout.Button("Test Initialize (Planet_small_01)"))
        {
            if (GameDataManager.Instance != null && GameDataManager.Instance.IsInitialized)
            {
                var data = GameDataManager.Instance.Get<GameData.PlanetData>("Planet_small_01");
                if (data != null)
                    planet.Initialize(data, "test_instance_01");
                else
                    Debug.LogWarning("[Editor] Planet_small_01 not found.");
            }
            else
            {
                Debug.LogWarning("[Editor] GameDataManager not initialized. Check GameManager is in scene.");
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