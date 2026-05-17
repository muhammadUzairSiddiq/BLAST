#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class LevelPrefabAssigner
{
    private const string LevelsFolder = "Assets/Prefabs/Levels";

    [MenuItem("BLAST/Assign All Level Prefabs (1-20)")]
    public static void AssignAllLevelPrefabs()
    {
        LevelCatalogBuilder.BuildFromMenu();
    }

    [MenuItem("BLAST/Assign All Level Prefabs (Legacy List Only)")]
    public static void AssignLegacyListOnly()
    {
        LevelManager manager = Object.FindFirstObjectByType<LevelManager>();
        if (manager == null)
        {
            Debug.LogError("LevelManager not found in the open scene.");
            return;
        }

        List<Level> levels = LoadLevelsInOrder();
        if (levels.Count == 0)
        {
            Debug.LogError($"No level prefabs found in {LevelsFolder}");
            return;
        }

        SerializedObject so = new SerializedObject(manager);
        SerializedProperty list = so.FindProperty("levelPrefabs");
        list.ClearArray();
        for (int i = 0; i < levels.Count; i++)
        {
            list.InsertArrayElementAtIndex(i);
            list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"Assigned {levels.Count} level prefabs to LevelManager.");
    }

    private static List<Level> LoadLevelsInOrder()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { LevelsFolder });
        var numbered = new List<(int number, Level level)>();

        Regex namePattern = new Regex(@"^Level \((\d+)\)$");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            Match match = namePattern.Match(fileName);
            if (!match.Success)
                continue;

            Level level = AssetDatabase.LoadAssetAtPath<Level>(path);
            if (level == null)
                continue;

            numbered.Add((int.Parse(match.Groups[1].Value), level));
        }

        numbered.Sort((a, b) => a.number.CompareTo(b.number));
        List<Level> result = new List<Level>();
        foreach ((int _, Level level) in numbered)
            result.Add(level);

        return result;
    }
}
#endif
