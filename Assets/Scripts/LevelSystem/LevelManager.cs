using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("Script References")] 
    [SerializeField] private UIManager uiManager;
    
    [Header("Level Setup")]
    [SerializeField] private LevelCatalog levelCatalog;
    [SerializeField] private List<Level> levelPrefabs = new List<Level>();
    [SerializeField] private Transform levelRoot;
    [SerializeField] private LevelLoadingPanel loadingPanel;

    [SerializeField] private List<TargetRenderers> levelThemesRenderer;
    [SerializeField] private List<TargetSpriteRenderers> levelThemesSprites;
    [SerializeField] private List<TargetImages> levelThemesImages;

    [Header("UI References")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Slider progressBar;

    [Header("Settings")]
    [SerializeField] private string levelPrefKey = "CurrentLevel";
    [SerializeField] private int maxLevels = 20;
    [SerializeField] private bool loopAfterMaxLevel = false;

    [SerializeField] private bool test;
    [SerializeField] private int testLevel;
    
    private int _currentLevelIndex;
    private Level _currentLevelInstance;
    
    private int _totalCubes;
    private int _destroyedCubes;

    public static LevelManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(this);
            return;
        }

        if (levelCatalog == null)
            levelCatalog = Resources.Load<LevelCatalog>("LevelCatalog");

        EnsureLoadingPanel();

        if (levelCatalog != null && levelCatalog.Count < maxLevels)
            Debug.LogWarning(
                $"LevelCatalog has {levelCatalog.Count} levels but maxLevels is {maxLevels}. " +
                "Run BLAST > Build Level Catalog (Required For Play).");
    }

    private int TotalLevelCount => maxLevels;

    private void EnsureLoadingPanel()
    {
        if (loadingPanel != null)
            return;

        loadingPanel = FindFirstObjectByType<LevelLoadingPanel>(FindObjectsInactive.Include);
        if (loadingPanel != null)
            return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        RectTransform progressTemplate = FindProgressAreaTemplate();
        Sprite face = Resources.Load<Sprite>("UI/face bg");

        if (canvas == null || progressTemplate == null || face == null)
        {
            Debug.LogWarning(
                "LevelLoadingPanel missing. Run BLAST > Create Level Loading Panel In Scene.");
            return;
        }

        loadingPanel = LevelLoadingPanelFactory.Create(canvas.transform, progressTemplate, face);
    }

    private static RectTransform FindProgressAreaTemplate()
    {
        foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name != "Progress Area")
                continue;

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
                return rt;
        }

        return null;
    }

    private Level GetLevelPrefab(int index)
    {
        if (index < 0 || index >= maxLevels)
            return null;

        if (levelCatalog != null)
        {
            Level fromCatalog = levelCatalog.GetLevel(index);
            if (fromCatalog != null)
                return fromCatalog;
        }

        if (index < levelPrefabs.Count && levelPrefabs[index] != null)
            return levelPrefabs[index];

        return null;
    }

    private void Start()
    {
        InitDefaultColor();
        LoadNextLevel();
    }
    
    private void InitDefaultColor()
    {
        foreach (TargetSpriteRenderers renderer1 in levelThemesSprites)
        {
            renderer1.defaultColor = renderer1.renderer.color;
        }

        foreach (TargetRenderers renderer1 in levelThemesRenderer)
        {
            renderer1.defaultColor = renderer1.renderer.material.color;
        }
    }

    private void UpdateLevel()
    {
        if (_currentLevelInstance.IsHard)
        {
            SoundManager.Instance.PlayGameHardSound();
        }
        else
        {
            SoundManager.Instance.PlayGameDefaultSound();
        }
        
        if (_currentLevelInstance.UseCustomTheme)
        {
            foreach (TargetSpriteRenderers renderer1 in levelThemesSprites)
            {
                ColorUtils.ApplyThemeTint(renderer1.renderer, _currentLevelInstance.CustomColor, 0.3f);
            }
        
            foreach (TargetSpriteRenderers renderer1 in _currentLevelInstance.LevelThemesSprites)
            {
                ColorUtils.ApplyThemeTint(renderer1.renderer, _currentLevelInstance.CustomColor, 0.85f);
            }
        
            foreach (TargetRenderers renderer1 in levelThemesRenderer)
            {
                ColorUtils.ApplyThemeTint(renderer1.renderer.material, _currentLevelInstance.CustomColor, 1f);
            }
        }
        else
        {
            foreach (TargetSpriteRenderers renderer1 in levelThemesSprites)
            {
                ColorUtils.ResetToOriginal(renderer1.renderer, renderer1.defaultColor);
            }
            
            foreach (TargetRenderers renderer1 in levelThemesRenderer)
            {
                ColorUtils.ResetToOriginal(renderer1.renderer.material, renderer1.defaultColor);
            }
        }
    }

    private void LoadLevelIndex()
    {
        if (test)
        {
            _currentLevelIndex = testLevel;
            return;
        }
        
        _currentLevelIndex = PlayerPrefs.GetInt(levelPrefKey, 0);
        _currentLevelIndex = Mathf.Clamp(_currentLevelIndex, 0, TotalLevelCount - 1);
    }

    [ContextMenu("SpawnCurrentLevel")]
    private void SpawnCurrentLevel()
    {
        if (_currentLevelInstance != null)
            DestroyImmediate(_currentLevelInstance);

        Level prefab = GetLevelPrefab(_currentLevelIndex);
        if (prefab == null)
        {
            Debug.LogError(
                $"Level {_currentLevelIndex + 1} is missing. In Unity menu run: BLAST > Build Level Catalog (Required For Play)");
            return;
        }

        _currentLevelInstance = Instantiate(prefab, levelRoot);
        UpdateLevel();
    }

    private void UpdateLevelText()
    {
        if (levelText != null)
            levelText.text = $"Level <size=40>{_currentLevelIndex + 1}</size>";
    }

    private void ResetProgressBar()
    {
        if (progressBar != null)
            progressBar.value = 0f;
    }

    public void SetTotalCubes(int total)
    {
        _totalCubes = Mathf.Max(1, total);
        _destroyedCubes = 0;
        if (progressBar != null)
            progressBar.value = 0f;
    }

    public void UpdateProgress()
    {
        _destroyedCubes = Mathf.Clamp(_destroyedCubes + 1, 0, _totalCubes);

        float normalizedValue = (float)_destroyedCubes / _totalCubes;

        if (progressBar != null)
            progressBar.value = normalizedValue;
        
        if (_destroyedCubes >= _totalCubes)
        {
            OnLevelComplete();
        }
    }
    
    private void OnLevelComplete()
    {
        uiManager.ShowLevelCompleteUI(_currentLevelIndex, _currentLevelInstance.LevelBonus);
        
        _currentLevelIndex++;
        int lastIndex = TotalLevelCount - 1;
        if (_currentLevelIndex > lastIndex)
            _currentLevelIndex = loopAfterMaxLevel ? 0 : lastIndex;

        PlayerPrefs.SetInt(levelPrefKey, _currentLevelIndex);
        PlayerPrefs.Save();
        
        Invoke(nameof(ClearCurrentLevel), 1.0f);
    }

    private void ClearCurrentLevel()
    {
        if (_currentLevelInstance != null)
            Destroy(_currentLevelInstance.gameObject);
    }
    
    public void ContinueToNextLevel()
    {
        uiManager.PrepareForLevelTransition();

        if (loadingPanel != null)
            loadingPanel.Show(LoadNextLevel);
        else
            LoadNextLevel();
    }

    public void LoadNextLevel()
    {
        uiManager.ShowGameUi();
        
        LoadLevelIndex();
        SpawnCurrentLevel();
        UpdateLevelText();
        ResetProgressBar();
    }
}

[Serializable]
public class TargetRenderers
{
    public Renderer renderer;
    public Color32 defaultColor;
}

[Serializable]
public class TargetSpriteRenderers
{
    public SpriteRenderer renderer;
    public Color32 defaultColor;
}

[Serializable]
public class TargetImages
{
    public Image renderer;
    public Color32 defaultColor;
}
