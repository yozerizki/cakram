using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FitImageToViewport : MonoBehaviour
{
    public RectTransform viewport; // drag Viewport ke sini lewat Inspector

    void Start()
    {
        UpdateImageSize();
    }

    public void UpdateImageSize()
    {
        Image img = GetComponent<Image>();
        if (img.sprite == null || viewport == null) return;

        RectTransform imgRect = img.rectTransform;

        // 1. Stretch horizontal anchoring
        imgRect.anchorMin = new Vector2(0, 1);
        imgRect.anchorMax = new Vector2(1, 1);
        imgRect.pivot = new Vector2(0.5f, 1);
        imgRect.anchoredPosition = Vector2.zero;

        // 2. Hitung tinggi dari aspect ratio sprite
        float spriteAspect = img.sprite.rect.height / img.sprite.rect.width;
        float targetWidth = viewport.rect.width;
        float targetHeight = targetWidth * spriteAspect;

        // 3. Set height
        imgRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        // 4. Set tinggi Content biar ScrollRect bisa scroll
        RectTransform content = imgRect.parent.GetComponent<RectTransform>();
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        //imgRect.sizeDelta = new Vector2 (img.sprite.rect.height);
    }
}
