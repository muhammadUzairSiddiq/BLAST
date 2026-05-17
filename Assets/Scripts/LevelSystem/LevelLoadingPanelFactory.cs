using UnityEngine;

public static class LevelLoadingPanelFactory
{
    public static LevelLoadingPanel Create(
        Transform canvasParent,
        RectTransform progressAreaTemplate,
        Sprite faceSprite)
    {
        return LevelLoadingPanelLayout.Build(canvasParent, progressAreaTemplate, faceSprite);
    }
}
