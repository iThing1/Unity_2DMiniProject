using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;

public class FontReplacer : EditorWindow
{
    private TMP_FontAsset _targetFont;
    private int _replacedCount = 0;

    [MenuItem("Tools/Font Replacer")]
    public static void ShowWindow()
    {
        GetWindow<FontReplacer>("Font Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("TMP 폰트 일괄 교체", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "교체할 폰트", _targetFont, typeof(TMP_FontAsset), false);

        EditorGUILayout.Space();

        if (_targetFont == null)
        {
            EditorGUILayout.HelpBox("교체할 Font Asset을 선택해주세요.", MessageType.Warning);
            return;
        }

        if (GUILayout.Button("씬 내 텍스트 교체"))
            ReplaceInScene();

        if (GUILayout.Button("프리팹 텍스트 교체 (Assets 전체)"))
            ReplaceInPrefabs();

        if (GUILayout.Button("씬 + 프리팹 전체 교체"))
        {
            ReplaceInScene();
            ReplaceInPrefabs();
        }
    }

    // =========================================================================
    // 씬 내 교체
    // =========================================================================
    private void ReplaceInScene()
    {
        _replacedCount = 0;

        TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        foreach (TextMeshProUGUI tmp in allTexts)
        {
            Undo.RecordObject(tmp, "Replace Font");
            tmp.font = _targetFont;
            EditorUtility.SetDirty(tmp);
            _replacedCount++;
        }

        Debug.Log($"[FontReplacer] 씬 내 텍스트 {_replacedCount}개 교체 완료");
        EditorUtility.DisplayDialog("완료", $"씬 내 텍스트 {_replacedCount}개 교체 완료", "확인");
    }

    // =========================================================================
    // 프리팹 전체 교체
    // =========================================================================
    private void ReplaceInPrefabs()
    {
        _replacedCount = 0;

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);

            TextMeshProUGUI[] texts = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
            bool changed = false;

            foreach (TextMeshProUGUI tmp in texts)
            {
                tmp.font = _targetFont;
                changed = true;
                _replacedCount++;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                Debug.Log($"[FontReplacer] 프리팹 교체: {path}");
            }

            PrefabUtility.UnloadPrefabContents(prefab);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[FontReplacer] 프리팹 텍스트 {_replacedCount}개 교체 완료");
        EditorUtility.DisplayDialog("완료", $"프리팹 텍스트 {_replacedCount}개 교체 완료", "확인");
    }
}
