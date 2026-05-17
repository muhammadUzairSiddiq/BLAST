#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LevelLoadingPanelCreator
{
    private const string FaceBgPath = "Assets/Resources/UI/face bg.png";

    [MenuItem("BLAST/Create Level Loading Panel In Scene")]
    public static void CreateInScene()
    {
        EnsureFaceBgIsSprite();

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in scene.");
            return;
        }

        Transform existing = canvas.transform.Find("LevelLoadingPanel");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        RectTransform progressTemplate = FindProgressAreaRect();
        if (progressTemplate == null)
        {
            Debug.LogError("Could not find Progress Area in scene.");
            return;
        }

        Sprite face = AssetDatabase.LoadAssetAtPath<Sprite>(FaceBgPath);
        LevelLoadingPanel loading = LevelLoadingPanelLayout.Build(canvas.transform, progressTemplate, face);

        LevelManager manager = Object.FindFirstObjectByType<LevelManager>();
        if (manager != null)
        {
            SerializedObject mgrSo = new SerializedObject(manager);
            mgrSo.FindProperty("loadingPanel").objectReferenceValue = loading;
            mgrSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }

        WireContinueButton(manager);
        EditorSceneManager.MarkSceneDirty(loading.gameObject.scene);
        Debug.Log("Level Loading Panel created: full-screen face + exact Progress Area clone.");
    }

    private static RectTransform FindProgressAreaRect()
    {
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name != "Progress Area")
                continue;

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
                return rt;
        }

        return null;
    }

    private static void WireContinueButton(LevelManager manager)
    {
        if (manager == null)
            return;

        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.gameObject.name != "Continue")
                continue;

            SerializedObject so = new SerializedObject(button);
            SerializedProperty onClick = so.FindProperty("m_OnClick");
            onClick.FindPropertyRelative("m_PersistentCalls.m_Calls").ClearArray();
            onClick.FindPropertyRelative("m_PersistentCalls.m_Calls").InsertArrayElementAtIndex(0);
            SerializedProperty call = onClick.FindPropertyRelative("m_PersistentCalls.m_Calls").GetArrayElementAtIndex(0);
            call.FindPropertyRelative("m_Target").objectReferenceValue = manager;
            call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = typeof(LevelManager).AssemblyQualifiedName;
            call.FindPropertyRelative("m_MethodName").stringValue = nameof(LevelManager.ContinueToNextLevel);
            call.FindPropertyRelative("m_Mode").enumValueIndex = 1;
            call.FindPropertyRelative("m_CallState").enumValueIndex = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(button);
            return;
        }
    }

    private static void EnsureFaceBgIsSprite()
    {
        TextureImporter importer = AssetImporter.GetAtPath(FaceBgPath) as TextureImporter;
        if (importer == null)
            return;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
#endif
