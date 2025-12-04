//using UnityEngine;
//using UnityEngine.UI;
//using Image = UnityEngine.UI.Image;  // avoid clash with System.Net.Mime.Image

///// <summary>
///// After the player goes idleSecondsBeforeHint without killing an impostor,
///// shows an arrow inside the vision circle that points toward a single chosen
///// impostor and keeps pointing to it until you reach / kill it.
///// </summary>
//[DefaultExecutionOrder(250)]
//public class ImpostorHintArrow : MonoBehaviour
//{
//    public static ImpostorHintArrow Instance { get; private set; }

//    [Header("Timing")]
//    [Tooltip("Seconds without killing an impostor before a hint arrow appears.")]
//    public float idleSecondsBeforeHint = 15f;

//    [Tooltip("World-space distance at which the player is considered to have 'reached' the impostor.")]
//    public float arrivalDistance = 1.2f;

//    [Header("Arrow visuals")]
//    [Tooltip("Root rect of the arrow. If null, one will be created under the vision mask rect.")]
//    public RectTransform arrowRoot;
//    [Tooltip("Rect of the arrow shaft.")]
//    public RectTransform arrowShaft;
//    [Tooltip("Rect of the arrow head.")]
//    public RectTransform arrowHead;

//    [Tooltip("Fraction of the vision circle radius to use for arrow length.")]
//    [Range(0.2f, 1f)] public float arrowDistanceScale = 0.8f;

//    [Header("Blinking")]
//    public bool enableBlink = true;
//    [Tooltip("Time for one full on→off→on cycle (seconds).")]
//    public float blinkPeriod = 0.6f;
//    [Range(0f, 1f)] public float blinkMinAlpha = 0.2f;
//    [Range(0f, 1f)] public float blinkMaxAlpha = 1f;

//    [Header("References")]
//    [Tooltip("If left null, Camera.main will be used.")]
//    public Camera worldCamera;

//    [Tooltip("Override for the vision mask rect. If null, uses VisionMaskController.MaskRect.")]
//    public RectTransform maskRectOverride;

//    [Tooltip("Optional explicit reference to the player mover. If null, uses PlayerMover.Active.")]
//    public PlayerMover playerMover;

//    [Tooltip("Optional explicit reference to the vision mask controller. If null, uses VisionMaskController.Instance.")]
//    public VisionMaskController visionMask;

//    // Internal
//    RectTransform maskRect;
//    Canvas canvas;
//    float idleTimer;
//    NPCIdentity currentTarget;
//    bool hintActive;
//    Vector2 shaftBaseSize;

//    CanvasGroup arrowCanvasGroup;
//    float blinkTimer;

//    void Awake()
//    {
//        if (Instance != null && Instance != this)
//        {
//            Destroy(gameObject);
//            return;
//        }

//        Instance = this;
//    }

//    void Start()
//    {
//        canvas = GetComponentInParent<Canvas>();

//        if (!worldCamera)
//            worldCamera = Camera.main;

//        if (!visionMask)
//            visionMask = VisionMaskController.Instance;

//        maskRect = maskRectOverride
//                   ? maskRectOverride
//                   : (visionMask != null ? visionMask.MaskRect : null);

//        if (!playerMover)
//            playerMover = PlayerMover.Active;

//        // If arrow not assigned in inspector, create one programmatically.
//        if (!arrowRoot)
//            CreateArrowUI();

//        if (arrowRoot)
//        {
//            arrowCanvasGroup = arrowRoot.GetComponent<CanvasGroup>();
//            if (!arrowCanvasGroup)
//                arrowCanvasGroup = arrowRoot.gameObject.AddComponent<CanvasGroup>();

//            arrowRoot.gameObject.SetActive(false);
//            arrowCanvasGroup.alpha = 0f;

//            if (arrowShaft)
//                shaftBaseSize = arrowShaft.sizeDelta;
//        }
//    }

//    void Update()
//    {
//        if (!playerMover)
//            playerMover = PlayerMover.Active;

//        if (!playerMover || !playerMover.isActiveAndEnabled)
//        {
//            HideArrowAndReset();
//            return;
//        }

//        if (!worldCamera)
//            worldCamera = Camera.main;

//        if (!visionMask)
//            visionMask = VisionMaskController.Instance;

//        if (!maskRect)
//            maskRect = maskRectOverride
//                       ? maskRectOverride
//                       : (visionMask != null ? visionMask.MaskRect : null);

//        // If our target despawned or got disabled, stop tracking it.
//        if (hintActive && !IsValidTarget(currentTarget))
//        {
//            HideArrowAndReset();
//        }

//        if (!hintActive)
//        {
//            idleTimer += Time.deltaTime;

//            if (idleTimer >= idleSecondsBeforeHint)
//            {
//                currentTarget = FindClosestImpostor();
//                if (IsValidTarget(currentTarget))
//                {
//                    hintActive = true;
//                    EnsureArrowVisible(true);
//                    idleTimer = 0f;
//                }
//                else
//                {
//                    // No impostors? Just keep waiting quietly.
//                    idleTimer = 0f;
//                }
//            }
//        }
//        else
//        {
//            UpdateArrowToTarget();
//            UpdateBlink();
//            CheckArrival();
//        }
//    }

//    bool IsValidTarget(NPCIdentity id)
//    {
//        return id != null &&
//               id.isImpostor &&
//               id.gameObject.activeInHierarchy;
//    }

//    NPCIdentity FindClosestImpostor()
//    {
//        var all = FindObjectsOfType<NPCIdentity>();
//        if (all == null || all.Length == 0)
//            return null;

//        Vector3 origin = playerMover.transform.position;
//        float bestDistSqr = float.MaxValue;
//        NPCIdentity best = null;

//        foreach (var npc in all)
//        {
//            if (!npc || !npc.isImpostor || !npc.gameObject.activeInHierarchy)
//                continue;

//            Vector3 diff = npc.transform.position - origin;
//            diff.y = 0f;
//            float d = diff.sqrMagnitude;
//            if (d < bestDistSqr)
//            {
//                bestDistSqr = d;
//                best = npc;
//            }
//        }

//        return best;
//    }

//    /// <summary>
//    /// Positions and rotates the arrow so its tail is at the player
//    /// and its tip is near the edge of the current vision circle,
//    /// pointing toward the tracked impostor.
//    /// </summary>
//    void UpdateArrowToTarget()
//    {
//        if (!arrowRoot || !IsValidTarget(currentTarget) || !playerMover)
//            return;

//        if (!maskRect)
//            maskRect = maskRectOverride
//                       ? maskRectOverride
//                       : (visionMask != null ? visionMask.MaskRect : null);

//        if (!maskRect)
//            return;

//        var camForScreen = GetCanvasCamera();

//        // World -> Screen
//        Vector3 playerWorld = playerMover.transform.position;
//        Vector3 targetWorld = currentTarget.transform.position;

//        Vector3 playerScreen = worldCamera.WorldToScreenPoint(playerWorld);
//        Vector3 targetScreen = worldCamera.WorldToScreenPoint(targetWorld);

//        // Screen -> local (UI)
//        Vector2 playerLocal;
//        Vector2 targetLocal;
//        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(maskRect, playerScreen, camForScreen, out playerLocal))
//            return;
//        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(maskRect, targetScreen, camForScreen, out targetLocal))
//            return;

//        Vector2 dirLocal = targetLocal - playerLocal;
//        if (dirLocal.sqrMagnitude < 0.0001f)
//            return;

//        dirLocal.Normalize();

//        // Compute actual radius based on VisionMaskController.currentRadius (0-1)
//        float minDim = Mathf.Min(maskRect.rect.width, maskRect.rect.height);
//        float radiusFromMask = minDim * (visionMask != null ? visionMask.currentRadius : 0.26f);

//        // Keep the arrow slightly inside the circle
//        float radiusPx = radiusFromMask * arrowDistanceScale;

//        // Distance from player to impostor in UI space
//        float distanceToTargetPx = (targetLocal - playerLocal).magnitude;

//        // We want the arrow to reach the impostor, but never go outside the circle
//        float desiredLength = Mathf.Min(distanceToTargetPx, radiusPx);

//        // Tail of the arrow at player position
//        arrowRoot.anchoredPosition = playerLocal;

//        // Rotate arrow in direction of impostor
//        float angle = Mathf.Atan2(dirLocal.y, dirLocal.x) * Mathf.Rad2Deg;
//        arrowRoot.localEulerAngles = new Vector3(0f, 0f, angle);

//        // Stretch shaft so head stops near the impostor (or circle edge if farther)
//        if (arrowShaft)
//        {
//            float shaftLength = desiredLength;

//            if (arrowHead)
//                shaftLength -= arrowHead.rect.width * 0.5f; // room for head

//            // Optional: small minimum so it doesn't disappear when very close
//            shaftLength = Mathf.Max(shaftLength, 10f);

//            var size = arrowShaft.sizeDelta;
//            size.x = shaftLength;
//            arrowShaft.sizeDelta = size;

//            if (arrowHead)
//                arrowHead.anchoredPosition = new Vector2(shaftLength, 0f);
//        }

//    }

//    Camera GetCanvasCamera()
//    {
//        if (!canvas) canvas = GetComponentInParent<Canvas>();
//        if (!canvas) return null;

//        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
//            return null;

//        return canvas.worldCamera ? canvas.worldCamera : worldCamera;
//    }

//    void CheckArrival()
//    {
//        if (!playerMover || !IsValidTarget(currentTarget))
//            return;

//        Vector3 a = playerMover.transform.position;
//        Vector3 b = currentTarget.transform.position;
//        a.y = b.y = 0f;

//        if ((a - b).sqrMagnitude <= arrivalDistance * arrivalDistance)
//        {
//            // Considered "found" – hide arrow, keep target cleared.
//            HideArrowAndReset();
//        }
//    }

//    void HideArrowAndReset()
//    {
//        hintActive = false;
//        currentTarget = null;
//        idleTimer = 0f;
//        blinkTimer = 0f;
//        EnsureArrowVisible(false);
//    }

//    void EnsureArrowVisible(bool visible)
//    {
//        if (arrowRoot)
//            arrowRoot.gameObject.SetActive(visible);

//        if (arrowCanvasGroup)
//            arrowCanvasGroup.alpha = visible ? blinkMaxAlpha : 0f;
//    }

//    void UpdateBlink()
//    {
//        if (!enableBlink || arrowCanvasGroup == null)
//            return;

//        if (blinkPeriod <= 0.0001f)
//        {
//            arrowCanvasGroup.alpha = blinkMaxAlpha;
//            return;
//        }

//        blinkTimer += Time.deltaTime;
//        float phase = (blinkTimer / blinkPeriod) * Mathf.PI * 2f; // 0→2π
//        float s = (Mathf.Sin(phase) + 1f) * 0.5f;                  // 0→1
//        arrowCanvasGroup.alpha = Mathf.Lerp(blinkMinAlpha, blinkMaxAlpha, s);
//    }

//    /// <summary>
//    /// Called from ClickToSmite when an impostor is correctly killed.
//    /// </summary>
//    public void NotifyImpostorKilled(NPCIdentity killed)
//    {
//        // If it was our tracked impostor, definitely stop hinting.
//        if (killed != null && killed == currentTarget)
//        {
//            HideArrowAndReset();
//        }
//        else
//        {
//            // Even if it was a different impostor, reset the idle timer
//            // so hints don't instantly re-trigger.
//            idleTimer = 0f;
//            if (!AnyImpostorsLeft())
//                HideArrowAndReset();
//        }
//    }

//    bool AnyImpostorsLeft()
//    {
//        var all = FindObjectsOfType<NPCIdentity>();
//        foreach (var npc in all)
//        {
//            if (npc != null && npc.isImpostor && npc.gameObject.activeInHierarchy)
//                return true;
//        }
//        return false;
//    }

//    // --------------------------------------------------------------------
//    // UI Arrow creation (shaft + head) under the vision mask.
//    // --------------------------------------------------------------------
//    void CreateArrowUI()
//    {
//        if (!maskRect)
//        {
//            if (!visionMask)
//                visionMask = VisionMaskController.Instance;
//            if (visionMask != null)
//                maskRect = visionMask.MaskRect;
//        }

//        if (!maskRect)
//            return;

//        var arrowGO = new GameObject("ImpostorHintArrow", typeof(RectTransform));
//        arrowRoot = arrowGO.GetComponent<RectTransform>();
//        arrowRoot.SetParent(maskRect, false);
//        arrowRoot.anchorMin = new Vector2(0.5f, 0.5f);
//        arrowRoot.anchorMax = new Vector2(0.5f, 0.5f);
//        arrowRoot.pivot = new Vector2(0f, 0.5f); // tail at center
//        arrowRoot.sizeDelta = new Vector2(200f, 40f);

//        // Shaft
//        var shaftGO = new GameObject("Shaft", typeof(RectTransform), typeof(Image));
//        arrowShaft = shaftGO.GetComponent<RectTransform>();
//        arrowShaft.SetParent(arrowRoot, false);
//        arrowShaft.anchorMin = new Vector2(0f, 0.5f);
//        arrowShaft.anchorMax = new Vector2(0f, 0.5f);
//        arrowShaft.pivot = new Vector2(0f, 0.5f);
//        arrowShaft.anchoredPosition = Vector2.zero;
//        arrowShaft.sizeDelta = new Vector2(120f, 12f);
//        var shaftImage = shaftGO.GetComponent<Image>();
//        shaftImage.sprite = GetSolidSprite();
//        shaftImage.color = Color.white;
//        shaftImage.raycastTarget = false;

//        // Head
//        var headGO = new GameObject("Head", typeof(RectTransform), typeof(Image));
//        arrowHead = headGO.GetComponent<RectTransform>();
//        arrowHead.SetParent(arrowRoot, false);
//        arrowHead.anchorMin = new Vector2(0f, 0.5f);
//        arrowHead.anchorMax = new Vector2(0f, 0.5f);
//        arrowHead.pivot = new Vector2(0.5f, 0.5f);
//        arrowHead.anchoredPosition = new Vector2(26f * 0.5f + arrowShaft.sizeDelta.x, 0f);
//        arrowHead.sizeDelta = new Vector2(26f, 26f);
//        arrowHead.localRotation = Quaternion.Euler(0f, 0f, 45f);
//        var headImage = headGO.GetComponent<Image>();
//        headImage.sprite = GetSolidSprite();
//        headImage.color = Color.white;
//        headImage.raycastTarget = false;

//        shaftBaseSize = arrowShaft.sizeDelta;
//    }

//    Sprite GetSolidSprite()
//    {
//        var tex = new Texture2D(2, 2);
//        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
//        tex.Apply();
//        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;  // avoid clash with System.Net.Mime.Image

/// <summary>
/// After the player goes idleSecondsBeforeHint without killing an impostor,
/// shows an arrow inside the vision circle that points toward a single chosen
/// impostor and keeps pointing to it until you reach / kill it.
/// </summary>
[DefaultExecutionOrder(250)]
public class ImpostorHintArrow : MonoBehaviour
{
    public static ImpostorHintArrow Instance { get; private set; }

    [Header("Timing")]
    [Tooltip("Seconds without killing an impostor before a hint arrow appears.")]
    public float idleSecondsBeforeHint = 15f;

    [Tooltip("World-space distance at which the player is considered to have 'reached' the impostor.")]
    public float arrivalDistance = 1.2f;

    [Header("Arrow visuals")]
    [Tooltip("Root rect of the arrow. If null, one will be created under the vision mask rect.")]
    public RectTransform arrowRoot;
    [Tooltip("Rect of the arrow shaft.")]
    public RectTransform arrowShaft;
    [Tooltip("Rect of the arrow head.")]
    public RectTransform arrowHead;

    [Tooltip("Fraction of the vision circle radius to use for arrow length.")]
    [Range(0.2f, 1f)] public float arrowDistanceScale = 0.8f;

    [Header("Offsets")]
    [Tooltip("How far the arrow starts away from the player (UI pixels).")]
    public float startOffset = 20f;

    [Tooltip("How far the arrow stops before reaching the impostor (UI pixels).")]
    public float endOffset = 25f;

    [Header("Blinking")]
    public bool enableBlink = true;
    [Tooltip("Time for one full on→off→on cycle (seconds).")]
    public float blinkPeriod = 0.6f;
    [Range(0f, 1f)] public float blinkMinAlpha = 0.2f;
    [Range(0f, 1f)] public float blinkMaxAlpha = 1f;

    [Header("References")]
    [Tooltip("If left null, Camera.main will be used.")]
    public Camera worldCamera;

    [Tooltip("Override for the vision mask rect. If null, uses VisionMaskController.MaskRect.")]
    public RectTransform maskRectOverride;

    [Tooltip("Optional explicit reference to the player mover. If null, uses PlayerMover.Active.")]
    public PlayerMover playerMover;

    [Tooltip("Optional explicit reference to the vision mask controller. If null, uses VisionMaskController.Instance.")]
    public VisionMaskController visionMask;

    // Internal
    RectTransform maskRect;
    Canvas canvas;
    float idleTimer;
    NPCIdentity currentTarget;
    bool hintActive;
    Vector2 shaftBaseSize;

    CanvasGroup arrowCanvasGroup;
    float blinkTimer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();

        if (!worldCamera)
            worldCamera = Camera.main;

        if (!visionMask)
            visionMask = VisionMaskController.Instance;

        maskRect = maskRectOverride
                   ? maskRectOverride
                   : (visionMask != null ? visionMask.MaskRect : null);

        if (!playerMover)
            playerMover = PlayerMover.Active;

        // If arrow not assigned in inspector, create one programmatically.
        if (!arrowRoot)
            CreateArrowUI();

        if (arrowRoot)
        {
            arrowCanvasGroup = arrowRoot.GetComponent<CanvasGroup>();
            if (!arrowCanvasGroup)
                arrowCanvasGroup = arrowRoot.gameObject.AddComponent<CanvasGroup>();

            arrowRoot.gameObject.SetActive(false);
            arrowCanvasGroup.alpha = 0f;

            if (arrowShaft)
                shaftBaseSize = arrowShaft.sizeDelta;
        }
    }

    void Update()
    {
        if (!playerMover)
            playerMover = PlayerMover.Active;

        if (!playerMover || !playerMover.isActiveAndEnabled)
        {
            HideArrowAndReset();
            return;
        }

        if (!worldCamera)
            worldCamera = Camera.main;

        if (!visionMask)
            visionMask = VisionMaskController.Instance;

        if (!maskRect)
            maskRect = maskRectOverride
                       ? maskRectOverride
                       : (visionMask != null ? visionMask.MaskRect : null);

        // If our target despawned or got disabled, stop tracking it.
        if (hintActive && !IsValidTarget(currentTarget))
        {
            HideArrowAndReset();
        }

        if (!hintActive)
        {
            idleTimer += Time.deltaTime;

            if (idleTimer >= idleSecondsBeforeHint)
            {
                currentTarget = FindClosestImpostor();
                if (IsValidTarget(currentTarget))
                {
                    hintActive = true;
                    EnsureArrowVisible(true);
                    idleTimer = 0f;
                }
                else
                {
                    // No impostors? Just keep waiting quietly.
                    idleTimer = 0f;
                }
            }
        }
        else
        {
            UpdateArrowToTarget();
            UpdateBlink();
            CheckArrival();
        }
    }

    bool IsValidTarget(NPCIdentity id)
    {
        return id != null &&
               id.isImpostor &&
               id.gameObject.activeInHierarchy;
    }

    NPCIdentity FindClosestImpostor()
    {
        var all = FindObjectsOfType<NPCIdentity>();
        if (all == null || all.Length == 0)
            return null;

        Vector3 origin = playerMover.transform.position;
        float bestDistSqr = float.MaxValue;
        NPCIdentity best = null;

        foreach (var npc in all)
        {
            if (!npc || !npc.isImpostor || !npc.gameObject.activeInHierarchy)
                continue;

            Vector3 diff = npc.transform.position - origin;
            diff.y = 0f;
            float d = diff.sqrMagnitude;
            if (d < bestDistSqr)
            {
                bestDistSqr = d;
                best = npc;
            }
        }

        return best;
    }

    /// <summary>
    /// Positions and rotates the arrow so its tail is offset from the player
    /// and its tip stops before the impostor, staying inside the vision circle.
    /// </summary>
    void UpdateArrowToTarget()
    {
        if (!arrowRoot || !IsValidTarget(currentTarget) || !playerMover)
            return;

        if (!maskRect)
            maskRect = maskRectOverride
                       ? maskRectOverride
                       : (visionMask != null ? visionMask.MaskRect : null);

        if (!maskRect)
            return;

        var camForScreen = GetCanvasCamera();

        // World -> Screen
        Vector3 playerWorld = playerMover.transform.position;
        Vector3 targetWorld = currentTarget.transform.position;

        Vector3 playerScreen = worldCamera.WorldToScreenPoint(playerWorld);
        Vector3 targetScreen = worldCamera.WorldToScreenPoint(targetWorld);

        // Screen -> local (UI)
        Vector2 playerLocal;
        Vector2 targetLocal;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(maskRect, playerScreen, camForScreen, out playerLocal))
            return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(maskRect, targetScreen, camForScreen, out targetLocal))
            return;

        Vector2 dirLocal = targetLocal - playerLocal;
        if (dirLocal.sqrMagnitude < 0.0001f)
            return;

        dirLocal.Normalize();

        // Compute actual radius based on VisionMaskController.currentRadius (0-1)
        float minDim = Mathf.Min(maskRect.rect.width, maskRect.rect.height);
        float radiusFromMask = minDim * (visionMask != null ? visionMask.currentRadius : 0.26f);

        // Max length allowed inside the circle, after pushing startOffset forward
        float maxLengthInsideCircle = Mathf.Max(radiusFromMask * arrowDistanceScale - startOffset, 10f);

        // Distance from player to impostor in UI space
        float fullDistance = (targetLocal - playerLocal).magnitude;

        // Subtract offsets at both ends so arrow doesn't touch player or impostor
        float usableDistance = Mathf.Max(fullDistance - startOffset - endOffset, 10f);

        // Final desired length
        float desiredLength = Mathf.Min(usableDistance, maxLengthInsideCircle);

        // Tail of the arrow is offset away from the player
        arrowRoot.anchoredPosition = playerLocal + dirLocal * startOffset;

        // Rotate arrow in direction of impostor
        float angle = Mathf.Atan2(dirLocal.y, dirLocal.x) * Mathf.Rad2Deg;
        arrowRoot.localEulerAngles = new Vector3(0f, 0f, angle);

        // Stretch shaft so head stops before the impostor / circle edge
        if (arrowShaft)
        {
            float shaftLength = desiredLength;

            if (arrowHead)
                shaftLength -= arrowHead.rect.width * 0.5f; // room for head

            shaftLength = Mathf.Max(shaftLength, 10f);

            var size = arrowShaft.sizeDelta;
            size.x = shaftLength;
            arrowShaft.sizeDelta = size;

            if (arrowHead)
                arrowHead.anchoredPosition = new Vector2(shaftLength, 0f);
        }
    }

    Camera GetCanvasCamera()
    {
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (!canvas) return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera ? canvas.worldCamera : worldCamera;
    }

    void CheckArrival()
    {
        if (!playerMover || !IsValidTarget(currentTarget))
            return;

        Vector3 a = playerMover.transform.position;
        Vector3 b = currentTarget.transform.position;
        a.y = b.y = 0f;

        if ((a - b).sqrMagnitude <= arrivalDistance * arrivalDistance)
        {
            // Considered "found" – hide arrow, keep target cleared.
            HideArrowAndReset();
        }
    }

    void HideArrowAndReset()
    {
        hintActive = false;
        currentTarget = null;
        idleTimer = 0f;
        blinkTimer = 0f;
        EnsureArrowVisible(false);
    }

    void EnsureArrowVisible(bool visible)
    {
        if (arrowRoot)
            arrowRoot.gameObject.SetActive(visible);

        if (arrowCanvasGroup)
            arrowCanvasGroup.alpha = visible ? blinkMaxAlpha : 0f;
    }

    void UpdateBlink()
    {
        if (!enableBlink || arrowCanvasGroup == null)
            return;

        if (blinkPeriod <= 0.0001f)
        {
            arrowCanvasGroup.alpha = blinkMaxAlpha;
            return;
        }

        blinkTimer += Time.deltaTime;
        float phase = (blinkTimer / blinkPeriod) * Mathf.PI * 2f; // 0→2π
        float s = (Mathf.Sin(phase) + 1f) * 0.5f;                  // 0→1
        arrowCanvasGroup.alpha = Mathf.Lerp(blinkMinAlpha, blinkMaxAlpha, s);
    }

    /// <summary>
    /// Called from ClickToSmite when an impostor is correctly killed.
    /// </summary>
    public void NotifyImpostorKilled(NPCIdentity killed)
    {
        // If it was our tracked impostor, definitely stop hinting.
        if (killed != null && killed == currentTarget)
        {
            HideArrowAndReset();
        }
        else
        {
            // Even if it was a different impostor, reset the idle timer
            // so hints don't instantly re-trigger.
            idleTimer = 0f;
            if (!AnyImpostorsLeft())
                HideArrowAndReset();
        }
    }

    bool AnyImpostorsLeft()
    {
        var all = FindObjectsOfType<NPCIdentity>();
        foreach (var npc in all)
        {
            if (npc != null && npc.isImpostor && npc.gameObject.activeInHierarchy)
                return true;
        }
        return false;
    }

    // --------------------------------------------------------------------
    // UI Arrow creation (shaft + head) under the vision mask.
    // --------------------------------------------------------------------
    void CreateArrowUI()
    {
        if (!maskRect)
        {
            if (!visionMask)
                visionMask = VisionMaskController.Instance;
            if (visionMask != null)
                maskRect = visionMask.MaskRect;
        }

        if (!maskRect)
            return;

        var arrowGO = new GameObject("ImpostorHintArrow", typeof(RectTransform));
        arrowRoot = arrowGO.GetComponent<RectTransform>();
        arrowRoot.SetParent(maskRect, false);
        arrowRoot.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRoot.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRoot.pivot = new Vector2(0f, 0.5f); // tail at center
        arrowRoot.sizeDelta = new Vector2(200f, 40f);

        // Shaft
        var shaftGO = new GameObject("Shaft", typeof(RectTransform), typeof(Image));
        arrowShaft = shaftGO.GetComponent<RectTransform>();
        arrowShaft.SetParent(arrowRoot, false);
        arrowShaft.anchorMin = new Vector2(0f, 0.5f);
        arrowShaft.anchorMax = new Vector2(0f, 0.5f);
        arrowShaft.pivot = new Vector2(0f, 0.5f);
        arrowShaft.anchoredPosition = Vector2.zero;
        arrowShaft.sizeDelta = new Vector2(120f, 12f);
        var shaftImage = shaftGO.GetComponent<Image>();
        shaftImage.sprite = GetSolidSprite();
        shaftImage.color = Color.white;
        shaftImage.raycastTarget = false;

        // Head
        var headGO = new GameObject("Head", typeof(RectTransform), typeof(Image));
        arrowHead = headGO.GetComponent<RectTransform>();
        arrowHead.SetParent(arrowRoot, false);
        arrowHead.anchorMin = new Vector2(0f, 0.5f);
        arrowHead.anchorMax = new Vector2(0f, 0.5f);
        arrowHead.pivot = new Vector2(0.5f, 0.5f);
        arrowHead.anchoredPosition = new Vector2(26f * 0.5f + arrowShaft.sizeDelta.x, 0f);
        arrowHead.sizeDelta = new Vector2(26f, 26f);
        arrowHead.localRotation = Quaternion.Euler(0f, 0f, 45f);
        var headImage = headGO.GetComponent<Image>();
        headImage.sprite = GetSolidSprite();
        headImage.color = Color.white;
        headImage.raycastTarget = false;

        shaftBaseSize = arrowShaft.sizeDelta;
    }

    Sprite GetSolidSprite()
    {
        var tex = new Texture2D(2, 2);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
    }
}
