using UnityEngine;
using UnityEngine.UI;

public static class LevelLoadingPanelLayout
{
    // Midpoint between screen center (0.5) and bottom (0) — matches face mockup bar slot.
    private const float LoadingBarAnchorY = 0.25f;

    public static LevelLoadingPanel Build(Transform canvasParent, RectTransform progressAreaTemplate, Sprite faceSprite)
    {
        GameObject panel = new GameObject(
            "LevelLoadingPanel",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(LevelLoadingPanel));

        panel.layer = progressAreaTemplate.gameObject.layer;
        panel.transform.SetParent(canvasParent, false);
        panel.transform.SetAsLastSibling();

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        StretchFull(panelRt);

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        CreateFullScreenFace(panel.transform, faceSprite);

        Slider loadingSlider = CloneProgressArea(progressAreaTemplate, panelRt);

        LevelLoadingPanel loading = panel.GetComponent<LevelLoadingPanel>();
        loading.Configure(cg, loadingSlider);
        panel.SetActive(false);
        return loading;
    }

    private static void CreateFullScreenFace(Transform parent, Sprite faceSprite)
    {
        GameObject face = new GameObject("FaceBG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        face.transform.SetParent(parent, false);
        face.transform.SetAsFirstSibling();

        RectTransform faceRt = face.GetComponent<RectTransform>();
        StretchFull(faceRt);

        Image img = face.GetComponent<Image>();
        img.sprite = faceSprite;
        img.color = Color.white;
        img.raycastTarget = true;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;

        if (faceSprite != null)
        {
            AspectRatioFitter fitter = face.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = faceSprite.rect.width / faceSprite.rect.height;
        }
    }

    private static Slider CloneProgressArea(RectTransform template, RectTransform panelRt)
    {
        GameObject copy = Object.Instantiate(template.gameObject, panelRt, false);
        copy.name = "Loading Progress Area";
        copy.SetActive(true);
        copy.transform.SetAsLastSibling();

        RectTransform copyRt = copy.GetComponent<RectTransform>();
        PlaceLoadingProgressBar(copyRt, template);

        Transform levelLabel = copy.transform.Find("Level");
        if (levelLabel != null)
            levelLabel.gameObject.SetActive(false);

        Slider slider = copy.GetComponentInChildren<Slider>(true);
        if (slider != null)
        {
            slider.value = 0f;
            slider.interactable = false;
            slider.gameObject.SetActive(true);
        }

        return slider;
    }

    public static void PlaceLoadingProgressBar(RectTransform target, RectTransform template)
    {
        target.localScale = Vector3.one;
        target.localRotation = Quaternion.identity;
        target.anchorMin = new Vector2(0.5f, LoadingBarAnchorY);
        target.anchorMax = new Vector2(0.5f, LoadingBarAnchorY);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.sizeDelta = template.sizeDelta;
        target.anchoredPosition = Vector2.zero;
    }

    public static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }
}
