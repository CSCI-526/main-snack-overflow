using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a contextual hint along the vision mask circumference describing changes to vision.
/// </summary>
public class TutorialVisionHint : MonoBehaviour
{
    public static TutorialVisionHint Instance { get; private set; }

    [Header("UI References")]
    public RectTransform root;
    public RectTransform arrowGroup;
    public RectTransform arrowShaft;
    public RectTransform arrowHead;
    public RectTransform messageRect;
    public TextMeshProUGUI messageText;
    public Button continueButton;

    [Header("Timing")]
    public float displayDuration = 2f;
    [Header("Layout")]
    public float circlePadding = 0f;
    [Range(0.2f, 1f)]
    public float arrowDistanceScale = 1f;

    float hideAt;
    bool visible;
    bool waitForContinue;
    Vector2 shaftBaseSize;
    float targetArrowLength;
    RectTransform trackedMask;
    float trackedRadius;
    Action onContinue;
    float headTipDepth;
    bool persistentDisplay;
    Vector2 rootDefaultAnchoredPosition;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        shaftBaseSize = arrowShaft ? arrowShaft.sizeDelta : Vector2.zero;
        targetArrowLength = shaftBaseSize.x;
        headTipDepth = CalculateHeadTipDepth();
        trackedMask = null;
        trackedRadius = 0f;
        persistentDisplay = false;
        rootDefaultAnchoredPosition = root ? root.anchoredPosition : Vector2.zero;

        if (continueButton)
            continueButton.onClick.AddListener(HandleContinue);

        Hide();
    }

    public void Configure(RectTransform rootRect, TextMeshProUGUI text, RectTransform groupRect,
        RectTransform shaftRect, RectTransform headRect, RectTransform labelRect, Button button)
    {
        root = rootRect;
        messageText = text;
        arrowGroup = groupRect;
        arrowShaft = shaftRect;
        arrowHead = headRect;
        messageRect = labelRect;
        continueButton = button;

        shaftBaseSize = arrowShaft ? arrowShaft.sizeDelta : Vector2.zero;
        targetArrowLength = shaftBaseSize.x;
        headTipDepth = CalculateHeadTipDepth();
        persistentDisplay = false;
        rootDefaultAnchoredPosition = root ? root.anchoredPosition : Vector2.zero;

        if (continueButton)
        {
            continueButton.onClick.RemoveListener(HandleContinue);
            continueButton.onClick.AddListener(HandleContinue);
        }

        Hide();
    }

    public void Show(bool increase, RectTransform maskRect, float radius, string customMessage = null,
        bool requireContinue = false, Action continueCallback = null, bool persistent = false)
    {
        if (!root || !messageText || maskRect == null)
            return;

        messageText.text = string.IsNullOrEmpty(customMessage)
            ? (increase ? "Vision expands across the landscape." : "Vision shrinks across the landscape.")
            : customMessage;
        messageText.color = Color.white;

        waitForContinue = requireContinue;
        onContinue = continueCallback;
        persistentDisplay = persistent || requireContinue;
        hideAt = persistentDisplay ? float.PositiveInfinity : Time.unscaledTime + displayDuration;
        trackedMask = maskRect;
        trackedRadius = radius;

        if (continueButton)
        {
            continueButton.gameObject.SetActive(requireContinue);
            continueButton.interactable = requireContinue;
        }

        Position(maskRect, radius);

        root.gameObject.SetActive(true);
        visible = true;
    }

    public void Hide()
    {
        visible = false;
        waitForContinue = false;
        onContinue = null;
        trackedMask = null;
        trackedRadius = 0f;
        persistentDisplay = false;
        if (root)
        {
            root.anchoredPosition = rootDefaultAnchoredPosition;
            root.gameObject.SetActive(false);
        }
        if (continueButton)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.interactable = false;
        }
    }

    void Update()
    {
        if (!visible || root == null)
            return;

        if (trackedMask)
            Position(trackedMask, trackedRadius);

        if (!waitForContinue && !persistentDisplay && Time.unscaledTime >= hideAt)
            Hide();
    }

    void HandleContinue()
    {
        if (!waitForContinue)
            return;
        var callback = onContinue;
        Hide();
        callback?.Invoke();
    }

    void Position(RectTransform maskRect, float radius)
    {
        if (!root || !arrowGroup)
            return;

        var parentRect = root.parent as RectTransform;
        if (!parentRect)
            return;

        var canvas = parentRect.GetComponentInParent<Canvas>();
        Camera cam = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        Vector3 worldCenter = maskRect.TransformPoint(maskRect.rect.center);
        if (!TryGetLocalPoint(parentRect, worldCenter, cam, out Vector2 localCenter))
            return;

        Vector3 baseWorld = arrowGroup ? arrowGroup.TransformPoint(Vector3.zero) : Vector3.zero;
        if (!TryGetLocalPoint(parentRect, baseWorld, cam, out Vector2 baseParent))
            return;

        Vector2 dir = baseParent - localCenter;
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.right;
        dir.Normalize();

        float reference = Mathf.Min(maskRect.rect.width, maskRect.rect.height) * 0.5f;
        float pixelRadius = reference * Mathf.Clamp01(radius);
        float perimeterRadius = pixelRadius + circlePadding;

        Vector2 tipParent = localCenter + dir * perimeterRadius;
        Vector3 tipWorld = parentRect.TransformPoint(new Vector3(tipParent.x, tipParent.y, 0f));

        ApplyArrowDistanceScaling(baseParent, tipParent);

        UpdateArrow(cam, tipWorld);
    }

    void ApplyArrowDistanceScaling(Vector2 baseParent, Vector2 tipParent)
    {
        if (!root)
            return;

        float scale = Mathf.Clamp(arrowDistanceScale, 0.2f, 1f);
        if (Mathf.Approximately(scale, 1f))
            return;

        Vector2 rootShift = root.anchoredPosition - rootDefaultAnchoredPosition;
        Vector2 baseDefault = baseParent - rootShift;
        Vector2 toTipDefault = tipParent - baseDefault;

        Vector2 desiredBase = tipParent - toTipDefault * scale;
        Vector2 delta = desiredBase - baseParent;
        if (delta.sqrMagnitude < 0.25f)
            return;

        root.anchoredPosition += delta;
    }

    void UpdateArrow(Camera cam, Vector3 tipWorld)
    {
        if (!arrowGroup)
            return;

        if (!TryGetLocalPoint(root, tipWorld, cam, out Vector2 tipLocal))
            return;

        Vector3 originWorld = arrowGroup.TransformPoint(Vector3.zero);
        if (!TryGetLocalPoint(root, originWorld, cam, out Vector2 originLocal))
            return;

        Vector2 toTip = tipLocal - originLocal;
        float distance = Mathf.Max(1f, toTip.magnitude);

        float angle = Mathf.Atan2(toTip.y, toTip.x) * Mathf.Rad2Deg;
        arrowGroup.localEulerAngles = new Vector3(0f, 0f, angle);

        float tipDepth = headTipDepth > 0f ? headTipDepth : 18f;
        float shaftLength = Mathf.Max(12f, distance - tipDepth);
        targetArrowLength = shaftLength;
        ApplyArrowLength(targetArrowLength);
    }

    float CalculateHeadTipDepth()
    {
        if (arrowHead == null)
            return 0f;
        float width = arrowHead.rect.width;
        return width * 0.5f * Mathf.Sqrt(2f);
    }

    void ApplyArrowLength(float length)
    {
        if (!arrowShaft)
            return;

        float clamped = Mathf.Max(16f, length);
        arrowShaft.sizeDelta = new Vector2(clamped, shaftBaseSize.y);

        if (arrowHead)
            arrowHead.anchoredPosition = new Vector2(clamped, 0f);
    }

    static bool TryGetLocalPoint(RectTransform rect, Vector3 worldPoint, Camera cam, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (!rect)
            return false;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPoint);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, cam, out localPoint);
    }

}
