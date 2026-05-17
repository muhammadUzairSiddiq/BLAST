using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LevelLoadingPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private float loadDuration = 1.6f;
    [SerializeField] private float fadeDuration = 0.25f;

    private Tween _loadTween;

    public void Configure(CanvasGroup group, Slider slider)
    {
        canvasGroup = group;
        loadingSlider = slider;
    }

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        HideInstant();
    }

    public void Show(Action onComplete)
    {
        gameObject.SetActive(true);
        KillTweens();
        EnsureProgressBarLayout();

        if (loadingSlider != null)
            loadingSlider.value = 0f;

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        Sequence seq = DOTween.Sequence();
        seq.Append(canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad));

        if (loadingSlider != null)
        {
            seq.Append(
                loadingSlider
                    .DOValue(1f, loadDuration)
                    .SetEase(Ease.OutCubic));
        }
        else
        {
            seq.AppendInterval(loadDuration);
        }

        seq.Append(canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            HideInstant();
            onComplete?.Invoke();
        });
    }

    private void EnsureProgressBarLayout()
    {
        if (loadingSlider == null)
            return;

        RectTransform progressRoot = loadingSlider.transform.parent as RectTransform;
        if (progressRoot == null)
            return;

        RectTransform template = FindGameplayProgressArea();
        if (template == null)
            return;

        progressRoot.gameObject.SetActive(true);
        progressRoot.SetAsLastSibling();
        LevelLoadingPanelLayout.PlaceLoadingProgressBar(progressRoot, template);
    }

    private static RectTransform FindGameplayProgressArea()
    {
        foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name != "Progress Area")
                continue;

            if (go.transform.parent != null && go.transform.parent.name == "LevelLoadingPanel")
                continue;

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
                return rt;
        }

        return null;
    }

    private void KillTweens()
    {
        _loadTween?.Kill();
        canvasGroup.DOKill();
        if (loadingSlider != null)
            loadingSlider.DOKill();
    }

    private void HideInstant()
    {
        KillTweens();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        KillTweens();
    }
}
