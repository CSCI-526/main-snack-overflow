using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(205)]
public class Level3TutorialController : MonoBehaviour
{
    static readonly Vector2 MessageSize = new Vector2(600f, 330f);
    static readonly Vector2 MessageAnchor = new Vector2(0.5f, 0.7f);
    static readonly Vector2 MessageOffset = new Vector2(0f, -40f);
    static readonly Vector2 ArrowTailOffset = new Vector2(-140f, 55f);
    const float MessageArrowSpacing = 5f;
    const float MessageArrowInset = 0f;
    const float ArrowHeadSize = 26f;
    const float ArrowThickness = 12f;
    const float ArrowTargetHeight = 1.4f;
    const string LevelThreeSceneName = "LvL3";
    const float BrightnessArrowGroundOffset = 0.08f;
    const float BrightnessArrowMinRadius = 2.4f;
    const float BrightnessArrowMaxRadius = 5.8f;
    const float BrightnessArrowBasePerpOffset = 95f;
    const float BrightnessArrowTipRotationDegrees = 60f;

    static Level3TutorialController instance;
    static bool tutorialCompleted;
    static string lastSceneName;
    static bool sceneLoadedHooked;

    InstructionsManager instructions;
    TimerController timer;
    PlayerMover playerMover;
    VisionMaskController visionMask;

    Vector3 savedPlayerPosition;
    Quaternion savedPlayerRotation;
    bool playerMoverWasEnabled;
    bool playerStateCaptured;

    Canvas canvas;
    RectTransform canvasRect;
    CanvasGroup overlayGroup;
    RectTransform overlayRoot;
    RectTransform messagePanel;
    TextMeshProUGUI messageText;
    Button continueButton;
    Button skipButton;
    RectTransform arrowRoot;
    RectTransform arrowShaft;
    RectTransform arrowHead;
    Transform arrowTarget;
    Transform brightnessArrowTarget;
    Transform brightnessArrowAnchor;
    Camera mainCam;
    bool arrowPointingAtBrightness;
    float? arrowSpacingOverride;

    bool tutorialActive;
    bool lightDecayPaused;
    enum TutorialPhase
    {
        None,
        IntroPrompt,
        AwaitingCollection,
        PostCollectionMessage,
        Completed
    }

    TutorialPhase phase = TutorialPhase.None;
    bool awaitingLightOrb;
    LightCollectible activeTutorialOrb;
    readonly List<GameObject> suppressedOrbs = new List<GameObject>();
    readonly List<LightOrbSpawner> suppressedSpawners = new List<LightOrbSpawner>();
    [SerializeField] float tutorialLightBonus = 0.5f;

    public static bool TryBeginTutorial(InstructionsManager mgr)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool sceneChanged = sceneName != lastSceneName;
        if (sceneName == LevelThreeSceneName && sceneChanged)
            tutorialCompleted = false;
        lastSceneName = sceneName;

        EnsureInstance();
        if (tutorialCompleted || !IsLevelThreeScene())
            return false;

        return instance != null && instance.InternalBegin(mgr);
    }

    void Awake()
    {
        instance = this;
        EnsureSceneLoadedHooked();
        lastSceneName = SceneManager.GetActiveScene().name;
    }

    static void EnsureInstance()
    {
        if (instance != null)
            return;

        var go = new GameObject("Level3TutorialCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(Level3TutorialController));

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3500;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        go.GetComponent<GraphicRaycaster>().ignoreReversedGraphics = true;

        instance = go.GetComponent<Level3TutorialController>();
    }

    static void EnsureSceneLoadedHooked()
    {
        if (sceneLoadedHooked)
            return;

        SceneManager.sceneLoaded += HandleSceneLoaded;
        sceneLoadedHooked = true;
    }

    bool InternalBegin(InstructionsManager mgr)
    {
        if (tutorialActive)
            return false;

        instructions = mgr;
        timer = mgr ? mgr.timerController : FindObjectOfType<TimerController>(true);
        visionMask = VisionMaskController.Instance;

        EnsurePlayerPresent();

        CapturePlayerState();
        EnsureUI();

        Time.timeScale = 1f;
        instructions?.SetVisionMaskActive(true);
        visionMask?.UpdateRadius(visionMask.initialRadius);

        PrepareTutorialOrbs();
        PauseLightDecay(true);
        ShowIntroPrompt();

        tutorialActive = true;
        return true;
    }

    void EnsurePlayerPresent()
    {
        if (playerMover && PlayerMover.IsActivePlayer(playerMover))
            return;

        playerMover = FindObjectOfType<PlayerMover>();
        if (playerMover && PlayerMover.IsActivePlayer(playerMover))
            return;

        var spawner = FindObjectOfType<SpawnManager>();
        if (spawner)
            playerMover = spawner.EnsurePlayerForTutorial();

        if (!playerMover)
            playerMover = FindObjectOfType<PlayerMover>(true);

        if (!playerMover)
            Debug.LogWarning("[Level3Tutorial] Player not found; tutorial hints may not orient correctly.");
    }

    void PauseLightDecay(bool pause)
    {
        var lightController = PlayerLightController.Instance;
        if (!lightController)
            return;
        lightController.SetPassiveDecayPaused(pause);
        lightDecayPaused = pause;
    }

    void ShowIntroPrompt()
    {
        phase = TutorialPhase.IntroPrompt;
        ShowOverlay(true);
        FocusOnLightOrb();
        var orb = GetTutorialOrbTransform();
        if (orb)
            PointArrowAt(orb);
        else
            HideArrow(true);

        string message =
            "<b>Move and collect the <color=#FFD94A>Yellow Light Orb</color></b>";
        SetMessage(message);
        SetArrowSpacingOverride(0f);

        continueButton.onClick.RemoveAllListeners();
        continueButton.gameObject.SetActive(false);
        continueButton.interactable = false;

        if (skipButton)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(FinishTutorial);
            skipButton.gameObject.SetActive(true);
            skipButton.interactable = true;
        }
    }

    void BeginCollectionPhase()
    {
        if (phase != TutorialPhase.IntroPrompt)
            return;

        SetArrowSpacingOverride(null);
        continueButton.onClick.RemoveAllListeners();
        continueButton.gameObject.SetActive(false);
        // Keep the overlay/message visible so players continue to see instructions but let gameplay UI receive input.
        ShowOverlay(true, blockInput: false);
        RestorePlayerState();
        FocusOnLightOrb();
        var orb = GetTutorialOrbTransform();
        if (orb)
            PointArrowAt(orb);
        else
            HideArrow(true);
        awaitingLightOrb = true;
        phase = TutorialPhase.AwaitingCollection;
        LightCollectible.AnyCollected += HandleLightOrbCollected;
    }

    void HandleLightOrbCollected(LightCollectible orb, PlayerMover mover)
    {
        if (!awaitingLightOrb || !PlayerMover.IsActivePlayer(mover))
            return;
        if (activeTutorialOrb && orb != activeTutorialOrb)
            return;

        activeTutorialOrb = orb;
        awaitingLightOrb = false;
        StopListeningForOrbs();
        ApplyTutorialLightBonus();
        CapturePlayerState();
        HideArrow(true);
        ShowPostCollectionMessage();
    }

    void StopListeningForOrbs()
    {
        LightCollectible.AnyCollected -= HandleLightOrbCollected;
        awaitingLightOrb = false;
    }

    void ShowPostCollectionMessage()
    {
        phase = TutorialPhase.PostCollectionMessage;
        ShowOverlay(true);

        if (skipButton)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.gameObject.SetActive(false);
            skipButton.interactable = false;
        }

        continueButton.gameObject.SetActive(true);
        continueButton.interactable = true;
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(FinishTutorial);

        SetMessage("Collecting light orbs brightens your nearby area.\n\nPress Continue to start.");

        PointArrowAt(GetBrightnessArrowTarget(), highlightBrightness: true);
    }

    void CapturePlayerState()
    {
        if (playerMover == null || playerStateCaptured)
            return;

        savedPlayerPosition = playerMover.transform.position;
        savedPlayerRotation = playerMover.transform.rotation;
        playerMoverWasEnabled = playerMover.enabled;
        if (playerMoverWasEnabled)
            playerMover.ForceStopMovement();
        playerMover.enabled = false;
        playerStateCaptured = true;
    }

    void RestorePlayerState()
    {
        if (!playerStateCaptured || playerMover == null)
            return;

        playerMover.transform.position = savedPlayerPosition;
        playerMover.transform.rotation = savedPlayerRotation;
        if (playerMover.TryGetComponent<Rigidbody>(out var rb))
            rb.velocity = Vector3.zero;
        playerMover.enabled = playerMoverWasEnabled;
        if (playerMoverWasEnabled)
            playerMover.ForceStopMovement();
        playerStateCaptured = false;
    }

    void FocusOnLightOrb()
    {
        if (!playerMover)
            return;

        Transform target = GetTutorialOrbTransform();
        if (!target)
            return;

        Vector3 origin = playerStateCaptured ? savedPlayerPosition : playerMover.transform.position;
        Vector3 lookTarget = target.position;
        lookTarget.y = origin.y;

        Vector3 forward = lookTarget - origin;
        if (forward.sqrMagnitude > 0.001f)
            playerMover.transform.rotation = Quaternion.LookRotation(forward.normalized);

        if (playerMover.TryGetComponent<Rigidbody>(out var rb))
            rb.velocity = Vector3.zero;
    }

    Transform GetTutorialOrbTransform()
    {
        if (activeTutorialOrb && activeTutorialOrb.gameObject)
            return activeTutorialOrb.transform;

        var fallback = SelectTutorialOrb(FindObjectsOfType<LightCollectible>());
        if (fallback)
        {
            activeTutorialOrb = fallback;
            return fallback.transform;
        }
        return null;
    }

    void PrepareTutorialOrbs()
    {
        suppressedOrbs.Clear();
        suppressedSpawners.Clear();

        var spawners = FindObjectsOfType<LightOrbSpawner>();
        foreach (var spawner in spawners)
        {
            if (!spawner) continue;
            if (spawner.enabled)
            {
                spawner.enabled = false;
                suppressedSpawners.Add(spawner);
            }
        }

        var allOrbs = FindObjectsOfType<LightCollectible>();
        activeTutorialOrb = SelectTutorialOrb(allOrbs);

        foreach (var orb in allOrbs)
        {
            if (!orb || orb == activeTutorialOrb)
                continue;
            var go = orb.gameObject;
            if (go.activeSelf)
            {
                go.SetActive(false);
                suppressedOrbs.Add(go);
            }
        }

        if (!activeTutorialOrb)
            activeTutorialOrb = SpawnFallbackOrb();
    }

    LightCollectible SelectTutorialOrb(LightCollectible[] orbs)
    {
        if (orbs == null || orbs.Length == 0)
            return null;

        Vector3 origin = playerStateCaptured ? savedPlayerPosition :
            playerMover ? playerMover.transform.position : Vector3.zero;

        float bestDist = float.MaxValue;
        LightCollectible best = null;
        foreach (var orb in orbs)
        {
            if (!orb || !orb.gameObject.activeInHierarchy)
                continue;
            float dist = Vector3.SqrMagnitude(orb.transform.position - origin);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = orb;
            }
        }
        return best ?? orbs[0];
    }

    LightCollectible SpawnFallbackOrb()
    {
        Vector3 origin = playerMover ? playerMover.transform.position : Vector3.zero;
        Vector3 offset = playerMover ? playerMover.transform.forward : Vector3.forward;
        Vector3 spawnPos = origin + offset.normalized * 2.5f;
        spawnPos.y += 1.2f;

        var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "Tutorial Light Orb";
        orb.transform.position = spawnPos;
        orb.transform.localScale = Vector3.one * 0.9f;

        var col = orb.GetComponent<Collider>();
        if (col) col.isTrigger = true;

        var renderer = orb.GetComponent<MeshRenderer>();
        if (renderer)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        var light = orb.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 2.2f;
        light.intensity = 2.1f;
        light.color = new Color(1f, 0.88f, 0.45f);
        light.shadows = LightShadows.None;

        var collectible = orb.AddComponent<LightCollectible>();
        collectible.lightReward = 1f;
        orb.AddComponent<LightOrbAnimator>();
        return collectible;
    }

    void RestoreSuppressedOrbsAndSpawners()
    {
        HideArrow(true);
        foreach (var go in suppressedOrbs)
        {
            if (go)
                go.SetActive(true);
        }
        suppressedOrbs.Clear();

        foreach (var spawner in suppressedSpawners)
        {
            if (spawner)
                spawner.enabled = true;
        }
        suppressedSpawners.Clear();
    }

    void FinishTutorial()
    {
        if (!tutorialActive)
            return;

        StopListeningForOrbs();
        RestoreSuppressedOrbsAndSpawners();
        tutorialActive = false;
        tutorialCompleted = true;
        phase = TutorialPhase.Completed;

        ShowOverlay(false);
        if (skipButton)
            skipButton.gameObject.SetActive(false);
        RestorePlayerState();
        if (lightDecayPaused)
            PauseLightDecay(false);

        if (instructions != null)
        {
            instructions.SetVisionMaskActive(true);
            instructions.TriggerSpawnIfNeeded();
            instructions.EnableGameplayUI(true);
        }

        if (timer == null)
            timer = FindObjectOfType<TimerController>(true);
        if (timer != null)
            timer.StartTimer(90f);
    }

    void EnsureUI()
    {
        if (overlayRoot)
            return;

        canvas = GetComponent<Canvas>();
        if (!canvas)
            canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3500;
        canvasRect = canvas.GetComponent<RectTransform>();

        overlayRoot = new GameObject("Level3TutorialOverlay", typeof(RectTransform)).GetComponent<RectTransform>();
        overlayRoot.SetParent(transform, false);
        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;

        overlayGroup = overlayRoot.gameObject.AddComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;

        messagePanel = new GameObject("MessagePanel", typeof(RectTransform)).GetComponent<RectTransform>();
        messagePanel.SetParent(overlayRoot, false);
        messagePanel.anchorMin = MessageAnchor;
        messagePanel.anchorMax = MessageAnchor;
        messagePanel.pivot = new Vector2(0.5f, 0.5f);
        messagePanel.sizeDelta = MessageSize;
        messagePanel.anchoredPosition = MessageOffset;

        var panelImage = messagePanel.gameObject.AddComponent<Image>();
        panelImage.sprite = GetSolidSprite();
        panelImage.color = new Color(0.06f, 0.07f, 0.12f, 0.92f);

        var textGO = new GameObject("Message", typeof(RectTransform));
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.SetParent(messagePanel, false);
        textRect.anchorMin = new Vector2(0.06f, 0.34f);
        textRect.anchorMax = new Vector2(0.94f, 0.94f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        messageText = textGO.AddComponent<TextMeshProUGUI>();
        messageText.font = GetFontAsset();
        messageText.fontSize = 34f;
        messageText.color = Color.white;
        messageText.alignment = TextAlignmentOptions.Midline;
        messageText.enableWordWrapping = true;

        continueButton = CreateButton("ContinueButton", new Vector2(0.5f, 0.24f), "Continue", new Color(0.9f, 0.25f, 0.28f, 0.95f));
        skipButton = CreateButton("SkipButton", new Vector2(0.5f, 0.18f), "Skip Tutorial", new Color(0.3f, 0.3f, 0.36f, 0.95f));
        var skipCanvasGroup = skipButton.gameObject.AddComponent<CanvasGroup>();
        skipCanvasGroup.ignoreParentGroups = true;
        skipCanvasGroup.interactable = true;
        skipCanvasGroup.blocksRaycasts = true;
        skipButton.gameObject.SetActive(false);

        EnsureArrow();
        mainCam = Camera.main;
    }

    Button CreateButton(string name, Vector2 anchor, string label, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(messagePanel, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(240f, 64f);
        rect.anchoredPosition = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.sprite = GetSolidSprite();
        image.color = color;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        var labelGO = new GameObject("Label", typeof(RectTransform));
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.SetParent(go.transform, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var text = labelGO.AddComponent<TextMeshProUGUI>();
        text.font = GetFontAsset();
        text.text = label;
        text.fontSize = 28f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return button;
    }

    void EnsureArrow()
    {
        if (arrowRoot || !overlayRoot)
            return;

        arrowRoot = new GameObject("TutorialArrow", typeof(RectTransform)).GetComponent<RectTransform>();
        arrowRoot.SetParent(overlayRoot, false);
        arrowRoot.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRoot.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRoot.pivot = new Vector2(0f, 0.5f);
        arrowRoot.sizeDelta = new Vector2(200f, 40f);
        arrowRoot.gameObject.SetActive(false);

        arrowShaft = new GameObject("ArrowShaft", typeof(RectTransform)).GetComponent<RectTransform>();
        arrowShaft.SetParent(arrowRoot, false);
        arrowShaft.anchorMin = new Vector2(0f, 0.5f);
        arrowShaft.anchorMax = new Vector2(0f, 0.5f);
        arrowShaft.pivot = new Vector2(0f, 0.5f);
        arrowShaft.anchoredPosition = Vector2.zero;
        arrowShaft.sizeDelta = new Vector2(120f, ArrowThickness);
        var shaftImage = arrowShaft.gameObject.AddComponent<Image>();
        shaftImage.sprite = GetSolidSprite();
        shaftImage.color = Color.white;

        arrowHead = new GameObject("ArrowHead", typeof(RectTransform)).GetComponent<RectTransform>();
        arrowHead.SetParent(arrowRoot, false);
        arrowHead.anchorMin = new Vector2(0f, 0.5f);
        arrowHead.anchorMax = new Vector2(0f, 0.5f);
        arrowHead.pivot = new Vector2(0.5f, 0.5f);
        arrowHead.anchoredPosition = new Vector2(ArrowHeadSize * 0.5f, 0f);
        arrowHead.sizeDelta = new Vector2(ArrowHeadSize, ArrowHeadSize);
        arrowHead.localRotation = Quaternion.Euler(0f, 0f, 45f);

        var headImage = arrowHead.gameObject.AddComponent<Image>();
        headImage.sprite = GetSolidSprite();
        headImage.color = Color.white;
        headImage.raycastTarget = false;
    }

    void SetMessage(string text)
    {
        if (!messagePanel || !messageText)
            return;

        messagePanel.gameObject.SetActive(true);
        messageText.text = text;
    }

    void ShowOverlay(bool show, bool blockInput = true)
    {
        if (!overlayGroup)
            return;

        overlayGroup.alpha = show ? 1f : 0f;
        bool block = show && blockInput;
        overlayGroup.blocksRaycasts = block;
        overlayGroup.interactable = block;
    }

    void Update()
    {
        if (!tutorialActive)
            return;
        if (phase == TutorialPhase.IntroPrompt && HasMovementInput())
            BeginCollectionPhase();
        if (arrowPointingAtBrightness)
            UpdateBrightnessArrowAnchor();
        UpdateTutorialArrow();
    }

    void OnDisable()
    {
        if (tutorialActive)
            FinishTutorial();
        else if (lightDecayPaused)
            PauseLightDecay(false);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
        if (brightnessArrowAnchor)
            Destroy(brightnessArrowAnchor.gameObject);
    }

    static bool IsLevelThreeScene()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.IsValid() && scene.name == LevelThreeSceneName;
    }

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        lastSceneName = scene.name;
        if (scene.name == LevelThreeSceneName)
            tutorialCompleted = false;
    }

    static Sprite solidSprite;
    static TMP_FontAsset cachedFont;

    static Sprite GetSolidSprite()
    {
        if (solidSprite != null)
            return solidSprite;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "Level3TutorialSolidTex",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat,
            hideFlags = HideFlags.HideAndDontSave
        };
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);

        solidSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        solidSprite.hideFlags = HideFlags.HideAndDontSave;
        return solidSprite;
    }

    static TMP_FontAsset GetFontAsset()
    {
        if (cachedFont != null)
            return cachedFont;

        cachedFont = TMP_Settings.defaultFontAsset;
        if (cachedFont == null)
            cachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        return cachedFont;
    }
    void HideArrow(bool clearTarget = false)
    {
        if (clearTarget)
        {
            arrowTarget = null;
            brightnessArrowTarget = null;
            arrowPointingAtBrightness = false;
        }
        if (arrowRoot)
            arrowRoot.gameObject.SetActive(false);
    }

    void SetArrowSpacingOverride(float? spacing)
    {
        arrowSpacingOverride = spacing;
        UpdateTutorialArrow();
    }

    void PointArrowAt(Transform target, bool highlightBrightness = false)
    {
        if (target == null)
        {
            arrowPointingAtBrightness = false;
            brightnessArrowTarget = null;
            HideArrow(true);
            return;
        }

        arrowPointingAtBrightness = highlightBrightness;
        brightnessArrowTarget = highlightBrightness ? target : null;
        EnsureArrow();
        arrowTarget = target;
        if (arrowRoot)
            arrowRoot.gameObject.SetActive(true);
        UpdateTutorialArrow();
    }

    void UpdateTutorialArrow()
    {
        if (!arrowRoot || !arrowTarget || !overlayRoot)
            return;

        Vector3 tipWorld = GetArrowTipWorld();
        if (tipWorld == Vector3.zero)
        {
            HideArrow();
            return;
        }

        if (!TryConvertWorldToCanvasLocal(tipWorld, out var tipLocal))
        {
            HideArrow();
            return;
        }

        Vector2 basePos = GetMessageArrowBaseTowards(arrowTarget) ?? (tipLocal + ArrowTailOffset);
        arrowRoot.anchoredPosition = basePos;

        Vector2 delta = tipLocal - basePos;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        arrowRoot.localEulerAngles = new Vector3(0f, 0f, angle);

        float shaftLength = Mathf.Max(10f, delta.magnitude - ArrowHeadSize * 0.5f);
        if (arrowShaft)
        {
            arrowShaft.sizeDelta = new Vector2(shaftLength, ArrowThickness);
            arrowShaft.anchoredPosition = Vector2.zero;
        }
        if (arrowHead)
            arrowHead.anchoredPosition = new Vector2(shaftLength, 0f);
    }

    Vector2? GetMessageArrowBase()
    {
        if (!messagePanel)
            return null;

        float yOffset = -messagePanel.rect.height * 0.5f - MessageArrowSpacing;
        var panelPos = messagePanel.localPosition;
        return new Vector2(panelPos.x, panelPos.y + yOffset);
    }

    Vector2? GetMessageArrowBaseTowards(Transform target)
    {
        var basePos = GetMessageArrowBase();
        if (!basePos.HasValue || !target)
            return basePos;
        if (!TryConvertWorldToCanvasLocal(target.position, out var targetLocal))
            return basePos;

        Vector2 dir = targetLocal - basePos.Value;
        if (dir.sqrMagnitude < 0.01f)
            return basePos;
        dir.Normalize();
        Vector2 adjustedBase = basePos.Value + dir * MessageArrowInset;
        if (arrowPointingAtBrightness)
        {
            Vector2 perp = new Vector2(-dir.y, dir.x);
            adjustedBase += perp * BrightnessArrowBasePerpOffset;
        }
        return adjustedBase;
    }

    bool TryConvertWorldToCanvasLocal(Vector3 world, out Vector2 local)
    {
        local = default;
        if (!overlayRoot)
            return false;
        if (!canvas)
            canvas = GetComponent<Canvas>();
        if (!canvas)
            return false;
        if (!mainCam)
            mainCam = Camera.main;

        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(mainCam, world);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRoot,
            screenPt,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out local);
    }

    Vector3 GetArrowTipWorld()
    {
        if (!arrowTarget)
            return Vector3.zero;

        if (arrowPointingAtBrightness && arrowTarget == brightnessArrowTarget)
            return GetBrightnessArrowTipWorld();

        var renderer = arrowTarget.GetComponentInChildren<Renderer>();
        if (renderer)
            return renderer.bounds.center + Vector3.up * renderer.bounds.extents.y;

        return arrowTarget.position + Vector3.up * ArrowTargetHeight;
    }

    Vector3 GetBrightnessArrowTipWorld()
    {
        var focus = ResolveBrightnessFocus();
        if (!focus)
            return Vector3.zero;
        return CalculateBrightnessArrowPosition(focus);
    }

    Vector3 GetBrightnessArrowPlanarDirection(Transform focus)
    {
        if (mainCam)
        {
            Vector3 toCamera = Vector3.ProjectOnPlane(mainCam.transform.position - focus.position, Vector3.up);
            if (toCamera.sqrMagnitude > 0.001f)
                return (-toCamera).normalized;

            Vector3 forward = Vector3.ProjectOnPlane(-mainCam.transform.forward, Vector3.up);
            if (forward.sqrMagnitude > 0.001f)
                return forward.normalized;
        }

        Vector3 fwd = Vector3.ProjectOnPlane(focus.forward, Vector3.up);
        if (fwd.sqrMagnitude > 0.001f)
            return fwd.normalized;
        return Vector3.forward;
    }

    float GetBrightnessArrowRadius()
    {
        var lightController = PlayerLightController.Instance;
        if (lightController && lightController.targetLight)
        {
            float range = lightController.targetLight.range;
            float minRange = Mathf.Max(0.1f, lightController.minRange);
            float maxRange = Mathf.Max(minRange + 0.1f, lightController.maxRange);
            float lerp = Mathf.InverseLerp(minRange, maxRange, range);
            return Mathf.Lerp(BrightnessArrowMinRadius, BrightnessArrowMaxRadius, lerp);
        }
        return BrightnessArrowMinRadius;
    }

    bool UpdateBrightnessArrowAnchor(bool force = false)
    {
        if (!brightnessArrowAnchor)
            return false;
        if (!force && !arrowPointingAtBrightness)
            return false;

        var focus = ResolveBrightnessFocus();
        if (!focus)
            return false;

        Vector3 pos = CalculateBrightnessArrowPosition(focus);
        brightnessArrowAnchor.position = pos;
        return true;
    }

    void EnsureBrightnessArrowAnchor()
    {
        if (brightnessArrowAnchor)
            return;

        var go = new GameObject("Level3TutorialBrightnessAnchor");
        go.hideFlags = HideFlags.HideAndDontSave;
        brightnessArrowAnchor = go.transform;
    }

    Transform ResolveBrightnessFocus()
    {
        if (playerMover)
            return playerMover.transform;

        var lightController = PlayerLightController.Instance;
        if (lightController && lightController.targetLight)
            return lightController.targetLight.transform;

        return arrowTarget;
    }

    Vector3 CalculateBrightnessArrowPosition(Transform focus)
    {
        if (!focus)
            return Vector3.zero;

        float ground = playerMover ? playerMover.groundY : focus.position.y;
        Vector3 center = focus.position;
        center.y = ground + BrightnessArrowGroundOffset;

        Vector3 planarDir;
        if (!TryGetMessageBasePlanarDirection(center, out planarDir))
            planarDir = GetBrightnessArrowPlanarDirection(focus);
        float radius = GetBrightnessArrowRadius();
        if (planarDir.sqrMagnitude < 0.001f)
            planarDir = Vector3.forward;

        Vector3 adjustedDir = planarDir.normalized;
        if (Mathf.Abs(BrightnessArrowTipRotationDegrees) > 0.01f)
            adjustedDir = Quaternion.AngleAxis(BrightnessArrowTipRotationDegrees, Vector3.up) * adjustedDir;

        Vector3 target = center + adjustedDir * radius;
        target.y = center.y;
        return target;
    }

    bool TryGetMessageBasePlanarDirection(Vector3 center, out Vector3 direction)
    {
        direction = Vector3.zero;
        if (!overlayRoot)
            return false;
        if (!mainCam)
            mainCam = Camera.main;
        if (!mainCam)
            return false;
        var basePos = GetMessageArrowBase();
        if (!basePos.HasValue)
            return false;
        if (!canvas)
            canvas = GetComponent<Canvas>();
        if (!canvas)
            return false;

        Vector3 local = new Vector3(basePos.Value.x, basePos.Value.y, 0f);
        Vector3 overlayWorld = overlayRoot.TransformPoint(local);
        Vector2 baseScreen = RectTransformUtility.WorldToScreenPoint(
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            overlayWorld);

        Ray ray = mainCam.ScreenPointToRay(baseScreen);
        float denom = ray.direction.y;
        if (Mathf.Abs(denom) < 0.0001f)
            return false;
        float distance = (center.y - ray.origin.y) / denom;
        if (distance <= 0.05f)
            return false;

        Vector3 hit = ray.origin + ray.direction * distance;
        Vector3 planar = hit - center;
        planar.y = 0f;
        if (planar.sqrMagnitude < 0.001f)
            return false;

        direction = planar.normalized;
        return true;
    }

    void ApplyTutorialLightBonus()
    {
        if (tutorialLightBonus <= 0f)
            return;
        var lightController = PlayerLightController.Instance;
        if (lightController)
            lightController.AddLightEnergy(tutorialLightBonus);
    }

    Transform GetBrightnessArrowTarget()
    {
        EnsureBrightnessArrowAnchor();
        return UpdateBrightnessArrowAnchor(true) ? brightnessArrowAnchor : null;
    }

    bool HasMovementInput()
    {
        return Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.UpArrow) ||
            Input.GetKey(KeyCode.DownArrow) ||
            Input.GetKey(KeyCode.LeftArrow) ||
            Input.GetKey(KeyCode.RightArrow) ||
            Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
            Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f ||
            Input.touchCount > 0;
    }
}
