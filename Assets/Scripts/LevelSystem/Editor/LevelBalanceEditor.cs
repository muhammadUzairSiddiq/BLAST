#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ensures total shooter ammo per color equals cube count per color (1 shot = 1 cube).
/// </summary>
public static class LevelBalanceEditor
{
    private const string LevelsFolder = "Assets/Prefabs/Levels";
    private static readonly int[] PlayableColors = { 0, 1, 2, 3, 4 };

    [MenuItem("BLAST/Balance Levels 10-20 (Exact Ammo)")]
    public static void BalanceLevels10To20()
    {
        BalanceRange(10, 20);
    }

    [MenuItem("BLAST/Sync Levels 11-20 Board From Level 10")]
    public static void SyncBoardFromLevel10()
    {
        string masterPath = $"{LevelsFolder}/Level (10).prefab";
        GameObject master = AssetDatabase.LoadAssetAtPath<GameObject>(masterPath);
        if (master == null)
        {
            Debug.LogError("Level (10) not found.");
            return;
        }

        ColorCube[] masterCubes = GetSortedCubes(master);
        for (int n = 11; n <= 20; n++)
        {
            string path = $"{LevelsFolder}/Level ({n}).prefab";
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) continue;

            ColorCube[] cubes = GetSortedCubes(root);
            int count = Mathf.Min(masterCubes.Length, cubes.Length);
            for (int i = 0; i < count; i++)
            {
                SetCubeColor(cubes[i], masterCubes[i].Color);
                cubes[i].transform.localPosition = masterCubes[i].transform.localPosition;
                cubes[i].transform.localRotation = masterCubes[i].transform.localRotation;
            }

            EditorUtility.SetDirty(root);
            PrefabUtility.SavePrefabAsset(root);
            Debug.Log($"L{n}: synced {count} cubes with Level 10.");
        }

        AssetDatabase.SaveAssets();
    }

    private static ColorCube[] GetSortedCubes(GameObject root)
    {
        return root.GetComponentsInChildren<ColorCube>(true)
            .OrderBy(c => c.transform.position.z)
            .ThenBy(c => c.transform.position.x)
            .ThenBy(c => c.transform.position.y)
            .ToArray();
    }

    private static void SetCubeColor(ColorCube cube, CubeColors color)
    {
        SerializedObject so = new SerializedObject(cube);
        so.FindProperty("cubeColors").enumValueIndex = (int)color;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("BLAST/Validate Level Balance (All 1-20)")]
    public static void ValidateAllLevels()
    {
        for (int n = 1; n <= 20; n++)
            ValidateLevel(n, logOnly: true);
    }

    public static void BalanceRange(int from, int to)
    {
        int fixed_count = 0;
        for (int n = from; n <= to; n++)
        {
            if (BalanceLevel(n))
                fixed_count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Balanced {fixed_count} level(s) from {from} to {to}.");
    }

    private static bool BalanceLevel(int levelNumber)
    {
        string path = $"{LevelsFolder}/Level ({levelNumber}).prefab";
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null)
        {
            Debug.LogWarning($"Missing: {path}");
            return false;
        }

        Dictionary<int, int> cubeCounts = CountCubes(root);
        Player[] shooters = root.GetComponentsInChildren<Player>(true);
        if (shooters.Length == 0 || cubeCounts.Values.Sum() == 0)
        {
            Debug.LogWarning($"L{levelNumber}: no cubes or shooters.");
            return false;
        }

        int[] assignment = AssignShooterColors(shooters.Length, cubeCounts);
        var groups = new Dictionary<int, List<Player>>();

        for (int i = 0; i < shooters.Length; i++)
        {
            int color = assignment[i];
            SetPlayerColor(shooters[i], color);
            if (!groups.ContainsKey(color))
                groups[color] = new List<Player>();
            groups[color].Add(shooters[i]);
        }

        foreach (var pair in groups)
        {
            int color = pair.Key;
            List<Player> group = pair.Value;
            int[] ammoSplit = Distribute(cubeCounts.GetValueOrDefault(color, 0), group.Count);
            for (int i = 0; i < group.Count; i++)
                SetPlayerAmmo(group[i], ammoSplit[i]);
        }

        EditorUtility.SetDirty(root);
        PrefabUtility.SavePrefabAsset(root);

        string resPath = $"Assets/Resources/Levels/Level ({levelNumber}).prefab";
        GameObject res = AssetDatabase.LoadAssetAtPath<GameObject>(resPath);
        if (res != null)
        {
            // Sync Resources copy if it exists
            Object.DestroyImmediate(res, true);
            AssetDatabase.CopyAsset(path, resPath);
        }

        return ValidateLevel(levelNumber, logOnly: false);
    }

    private static bool ValidateLevel(int levelNumber, bool logOnly)
    {
        string path = $"{LevelsFolder}/Level ({levelNumber}).prefab";
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null) return false;

        Dictionary<int, int> cubes = CountCubes(root);
        Dictionary<int, int> ammo = CountAmmo(root);
        bool ok = true;

        foreach (int c in PlayableColors)
        {
            int cubeN = cubes.GetValueOrDefault(c, 0);
            int ammoN = ammo.GetValueOrDefault(c, 0);
            if (cubeN == 0 && ammoN == 0) continue;
            if (cubeN != ammoN)
            {
                ok = false;
                if (logOnly)
                    Debug.LogWarning($"L{levelNumber} {(CubeColors)c}: cubes={cubeN} ammo={ammoN}");
            }
        }

        if (logOnly && ok)
            Debug.Log($"L{levelNumber} OK — cubes/ammo match per color.");
        else if (!logOnly && ok)
            Debug.Log($"L{levelNumber} balanced OK.");

        return ok;
    }

    private static Dictionary<int, int> CountCubes(GameObject root)
    {
        var counts = new Dictionary<int, int>();
        foreach (ColorCube cube in root.GetComponentsInChildren<ColorCube>(true))
        {
            int c = (int)cube.Color;
            if (c == (int)CubeColors.Surprise) continue;
            counts[c] = counts.GetValueOrDefault(c, 0) + 1;
        }
        return counts;
    }

    private static Dictionary<int, int> CountAmmo(GameObject root)
    {
        var counts = new Dictionary<int, int>();
        foreach (Player p in root.GetComponentsInChildren<Player>(true))
        {
            int c = (int)p.Color;
            counts[c] = counts.GetValueOrDefault(c, 0) + p.AmmoCount;
        }
        return counts;
    }

    private static int[] AssignShooterColors(int shooterCount, Dictionary<int, int> cubeCounts)
    {
        var active = PlayableColors
            .Where(c => cubeCounts.GetValueOrDefault(c, 0) > 0)
            .Select(c => (c, cubeCounts[c]))
            .OrderByDescending(x => x.Item2)
            .ToList();

        var assignment = new List<int>();
        foreach (var (c, _) in active)
            assignment.Add(c);

        while (assignment.Count < shooterCount)
        {
            var assigned = assignment.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
            int pick = active.OrderByDescending(x => (float)x.Item2 / assigned[x.c]).First().c;
            assignment.Add(pick);
        }

        return assignment.Take(shooterCount).ToArray();
    }

    private static int[] Distribute(int total, int count)
    {
        if (count <= 0) return System.Array.Empty<int>();
        int baseVal = total / count;
        int rem = total % count;
        var result = new int[count];
        for (int i = 0; i < count; i++)
            result[i] = Mathf.Max(1, baseVal + (i < rem ? 1 : 0));
        return result;
    }

    private static void SetPlayerColor(Player player, int colorIndex)
    {
        SerializedObject so = new SerializedObject(player);
        so.FindProperty("cubeColors").enumValueIndex = colorIndex;
        so.FindProperty("isSurpriseShooter").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPlayerAmmo(Player player, int ammo)
    {
        SerializedObject so = new SerializedObject(player);
        so.FindProperty("ammoCount").intValue = ammo;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
