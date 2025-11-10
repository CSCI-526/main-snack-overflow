using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ImpostorColorIndicator : MonoBehaviour
{
    public static ImpostorColorIndicator Instance { get; private set; }

    [Header("UI")]
    [Tooltip("Label that displays the current impostor color.")]
    public TMP_Text label;

    [Tooltip("Optional swatch image that will be tinted to match the impostor color.")]
    public Image colorSwatch;

    [Tooltip("Optional renderers (e.g., cylinder mesh) that should be tinted to match the impostor color.")]
    public Renderer[] tintRenderers;

    [Header("Auto Cylinder Display")]
    public bool autoCreateSwatch = true;
    public Vector2 swatchSize = new Vector2(70f, 140f);
    public Vector2 swatchOffset = new Vector2(140f, 0f);

    [Header("Text Formatting")]
    public string prefix = "Current Impostor Color:";
    public string suffix = "";

    MaterialPropertyBlock _mpb;
    static Sprite _generatedCylinderSprite;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!label)
            label = GetComponent<TMP_Text>();

        EnsureSwatch();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetColor(NPCColorPalette palette, int colorId)
    {
        Color color = palette ? palette.GetForRole(colorId, true) : Color.white;
        string colorName = palette ? palette.GetName(colorId) : "Unknown";
        SetColor(colorName, color);
    }

    public void SetColor(string colorName, Color color)
    {
        if (label)
        {
            if (!string.IsNullOrEmpty(prefix) && !string.IsNullOrEmpty(suffix))
                label.text = $"{prefix} {suffix}";
            else if (!string.IsNullOrEmpty(prefix))
                label.text = prefix;
            else
                label.text = suffix;
        }

        var swatch = EnsureSwatch();
        if (swatch)
        {
            color.a = 1f;
            swatch.color = color;
        }

        if (tintRenderers != null && tintRenderers.Length > 0)
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();

            foreach (var r in tintRenderers)
            {
                if (!r) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor("_Color", color);
                r.SetPropertyBlock(_mpb);
            }
        }
    }

    Image EnsureSwatch()
    {
        if (colorSwatch)
            return colorSwatch;

        if (!autoCreateSwatch)
            return null;

        var labelRect = label ? label.rectTransform : (transform as RectTransform);
        if (!labelRect)
            return null;

        var parent = labelRect.parent as RectTransform;
        if (!parent)
            return null;

        var swatchGO = new GameObject("ImpostorCylinder", typeof(RectTransform), typeof(Image));
        var rect = swatchGO.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = labelRect.anchorMin;
        rect.anchorMax = labelRect.anchorMax;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = swatchSize;
        rect.anchoredPosition = labelRect.anchoredPosition + swatchOffset;

        colorSwatch = swatchGO.GetComponent<Image>();
        colorSwatch.raycastTarget = false;
        colorSwatch.sprite = GetGeneratedCylinderSprite();
        colorSwatch.type = Image.Type.Simple;
        colorSwatch.preserveAspect = true;
        colorSwatch.color = Color.white;
        return colorSwatch;
    }

    Sprite GetGeneratedCylinderSprite()
    {
        if (_generatedCylinderSprite)
            return _generatedCylinderSprite;

        const int width = 96;
        const int height = 192;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "ImpostorCylinderTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color32[width * height];
        float halfWidth = 0.5f;
        float bodyHalfHeight = 0.35f;
        float capHeight = 0.5f - bodyHalfHeight;
        float edgeFeather = 0.08f;

        for (int y = 0; y < height; y++)
        {
            float v = (y + 0.5f) / height;
            float centeredY = (v - 0.5f);
            float absY = Mathf.Abs(centeredY);
            float horizontalLimit;

            if (absY <= bodyHalfHeight)
            {
                horizontalLimit = halfWidth;
            }
            else
            {
                float capT = (absY - bodyHalfHeight) / capHeight;
                if (capT > 1f)
                    continue;
                horizontalLimit = Mathf.Sqrt(Mathf.Clamp01(1f - capT * capT)) * halfWidth;
            }

            for (int x = 0; x < width; x++)
            {
                float u = (x + 0.5f) / width;
                float centeredX = Mathf.Abs(u - 0.5f);

                float norm = centeredX / halfWidth;
                if (norm > 1f)
                    continue;

                float edgeDistance = (horizontalLimit / halfWidth) - norm;
                float alpha = Mathf.Clamp01(edgeDistance / edgeFeather);
                if (alpha <= 0f)
                    continue;

                float shading = 0.55f + 0.45f * (1f - Mathf.Pow(centeredX / horizontalLimit, 2f));
                byte shade = (byte)Mathf.Clamp(Mathf.RoundToInt(shading * 255f), 0, 255);
                var c = new Color32(shade, shade, shade, (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
                pixels[y * width + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        _generatedCylinderSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), width);
        _generatedCylinderSprite.name = "ImpostorCylinderSprite";
        _generatedCylinderSprite.hideFlags = HideFlags.HideAndDontSave;

        return _generatedCylinderSprite;
    }
}
