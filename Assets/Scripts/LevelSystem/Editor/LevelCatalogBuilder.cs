#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class LevelCatalogBuilder
{
    private const string PrefabsFolder = "Assets/Prefabs/Levels";
    private const string CatalogPath = "Assets/Resources/LevelCatalog.asset";

    [InitializeOnLoadMethod]
    private static void AutoBuildOnLoad()
    {
        EditorApplication.delayCall += () => EnsureBuilt(silent: true);
    }

    [MenuItem("BLAST/Build Level Catalog (Required For Play)")]
    public static void BuildFromMenu()
    {
        EnsureBuilt(silent: false);
    }

    public static void EnsureBuilt(bool silent)
    {
        List<GameObject> levels = LoadLevelPrefabsInOrder();
        if (levels.Count == 0)
        {
            if (!silent)
                Debug.LogError($"No level prefabs in {PrefabsFolder}");
            return;
        }

        LevelCatalog catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath)!);
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        SerializedObject so = new SerializedObject(catalog);
        SerializedProperty list = so.FindProperty("levelRoots");
        list.ClearArray();
        for (int i = 0; i < levels.Count; i++)
        {
            list.InsertArrayElementAtIndex(i);
            list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        AssignSceneLevelManager(catalog, levels);

        if (levels.Count < 20)
            Debug.LogWarning($"Only {levels.Count} level prefabs found. Expected 20 in {PrefabsFolder}.");

        if (!silent)
            Debug.Log($"LevelCatalog built with {levels.Count} levels at {CatalogPath}");
    }

    private static void AssignSceneLevelManager(LevelCatalog catalog, List<GameObject> levels)
    {
        LevelManager manager = Object.FindFirstObjectByType<LevelManager>();
        if (manager == null)
            return;

        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("levelCatalog").objectReferenceValue = catalog;

        SerializedProperty list = so.FindProperty("levelPrefabs");
        list.ClearArray();
        for (int i = 0; i < levels.Count; i++)
        {
            Level levelComponent = levels[i].GetComponent<Level>();
            if (levelComponent == null)
                continue;

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = levelComponent;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    private static List<GameObject> LoadLevelPrefabsInOrder()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsFolder });
        var numbered = new List<(int number, GameObject prefab)>();
        Regex pattern = new Regex(@"^Level \((\d+)\)$");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            Match match = pattern.Match(name);
            if (!match.Success)
                continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.GetComponent<Level>() != null)
                numbered.Add((int.Parse(match.Groups[1].Value), prefab));
        }

        numbered.Sort((a, b) => a.number.CompareTo(b.number));
        List<GameObject> result = new List<GameObject>();
        foreach ((int _, GameObject prefab) in numbered)
            result.Add(prefab);

        return result;
    }
}
#endif
