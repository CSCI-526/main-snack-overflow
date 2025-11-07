using System.Collections;
using UnityEngine;
using TMPro;

public class KillTextController : MonoBehaviour
{
    public static KillTextController Instance { get; private set; }

    [Header("Refs")]
    public TextMeshProUGUI label;

    [Header("Default Style")]
    public float holdSeconds = 0.9f;      // time fully visible
    public float fadeSeconds = 0.6f;      // fade out duration
    public Color textColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    public Vector3 startScale = new Vector3(0.9f, 0.9f, 1f);
    public Vector3 endScale   = new Vector3(1.05f, 1.05f, 1f);

    [Header("Hazard Style")]
    public float hazardHoldSeconds = 1.6f;
    public float hazardFadeSeconds = 0.9f;
    public float hazardFontSize = 60f;
    public Color hazardTextColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    public Vector3 hazardStartScale = new Vector3(0.85f, 0.85f, 1f);
    public Vector3 hazardEndScale = new Vector3(1f, 1f, 1f);

    Coroutine running;
    RectTransform rect;
    float defaultFontSize;
    Vector2 defaultAnchorMin, defaultAnchorMax, defaultPivot, defaultAnchoredPos;
    TextAlignmentOptions defaultAlignment;

    enum MessageStyle { Default, Hazard }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (label == null) label = GetComponent<TextMeshProUGUI>();
        if (label)
        {
            label.gameObject.SetActive(false);
            rect = label.rectTransform;
            defaultFontSize = label.fontSize;
            if (rect != null)
            {
                defaultAnchorMin = rect.anchorMin;
                defaultAnchorMax = rect.anchorMax;
                defaultPivot = rect.pivot;
                defaultAnchoredPos = rect.anchoredPosition;
            }
            defaultAlignment = label.alignment;
        }
    }


    public void Show(string message) => ShowStyled(message, MessageStyle.Default);
    public void ShowHazard(string message) => ShowStyled(message, MessageStyle.Hazard);

    void ShowStyled(string message, MessageStyle style)
    {
        if (!label) return;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ShowCR(message, style));
    }

    IEnumerator ShowCR(string message, MessageStyle style)
    {
        var config = GetStyle(style);
        ApplyStyle(style);

        label.gameObject.SetActive(true);
        label.text = message;

        // start state
        Color c = config.color; c.a = 0f;
        label.color = c;
        transform.localScale = config.startScale;

        // quick pop-in
        float t = 0f;
        const float popIn = 0.15f;
        while (t < popIn)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / popIn);
            label.color = new Color(config.color.r, config.color.g, config.color.b, k);
            transform.localScale = Vector3.Lerp(config.startScale, Vector3.one, k);
            yield return null;
        }

        // hold
        if (config.hold > 0f)
            yield return new WaitForSecondsRealtime(config.hold);

        // gentle scale + fade out
        t = 0f;
        if (config.fade > 0f)
        {
            while (t < config.fade)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / config.fade);
                float a = 1f - k;
                label.color = new Color(config.color.r, config.color.g, config.color.b, a);
                transform.localScale = Vector3.Lerp(Vector3.one, config.endScale, k);
                yield return null;
            }
        }

        label.gameObject.SetActive(false);
        running = null;
    }

    void ApplyStyle(MessageStyle style)
    {
        if (label == null || rect == null)
            return;

        if (style == MessageStyle.Default)
        {
            rect.anchorMin = defaultAnchorMin;
            rect.anchorMax = defaultAnchorMax;
            rect.pivot = defaultPivot;
            rect.anchoredPosition = defaultAnchoredPos;
            label.fontSize = defaultFontSize;
            label.alignment = defaultAlignment;
        }
        else
        {
            rect.anchorMin = new Vector2(0.5f, defaultAnchorMin.y);
            rect.anchorMax = new Vector2(0.5f, defaultAnchorMax.y);
            rect.pivot = new Vector2(0.5f, defaultPivot.y);
            rect.anchoredPosition = new Vector2(0f, defaultAnchoredPos.y);
            label.fontSize = hazardFontSize > 0f ? hazardFontSize : defaultFontSize;
            label.alignment = TextAlignmentOptions.Center;
        }
    }

    (float hold, float fade, Vector3 startScale, Vector3 endScale, Color color) GetStyle(MessageStyle style)
    {
        if (style == MessageStyle.Default)
            return (holdSeconds, fadeSeconds, startScale, endScale, textColor);

        return (hazardHoldSeconds, hazardFadeSeconds, hazardStartScale, hazardEndScale, hazardTextColor);
    }
}
