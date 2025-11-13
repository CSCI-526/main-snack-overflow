using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(205)]
public class Level3TutorialController : MonoBehaviour
{
    static readonly Vector2 MessageSize = new Vector2(660f, 370f);
    static readonly Vector2 MessageAnchor = new Vector2(0.5f, 0.7f);
    static readonly Vector2 MessageOffset = new Vector2(0f, -40f);
    const string LevelThreeSceneName = "LvL3";

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

    CanvasGroup overlayGroup;
    RectTransform overlayRoot;
    RectTransform messagePanel;
    TextMeshProUGUI messageText;
    Button continueButton;

    bool tutorialActive;

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
        playerMover = FindObjectOfType<PlayerMover>(true);
        visionMask = VisionMaskController.Instance;

        CapturePlayerState();
        EnsureUI();

        Time.timeScale = 1f;
        instructions?.SetVisionMaskActive(true);
        visionMask?.UpdateRadius(visionMask.initialRadius);

        ShowOverlay(true);

        string message =
            "<b>Collect <color=#FFD94A>Yellow Light Orbs</color></b>\n" +
            "Every orb you gather boosts your lantern's light intensity.\n" +
            "Stronger light makes it easier to spot the imposters.";
        SetMessage(message);

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(FinishTutorial);
        continueButton.gameObject.SetActive(true);
        continueButton.interactable = true;

        FocusOnLightOrb();

        tutorialActive = true;
        return true;
    }

    void CapturePlayerState()
    {
        if (playerMover == null || playerStateCaptured)
            return;

        savedPlayerPosition = playerMover.transform.position;
        savedPlayerRotation = playerMover.transform.rotation;
        playerMoverWasEnabled = playerMover.enabled;
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
        playerStateCaptured = false;
    }

    void FocusOnLightOrb()
    {
        if (!playerMover)
            return;

        var orbs = FindObjectsOfType<LightCollectible>(true);
        if (orbs == null || orbs.Length == 0)
            return;

        Transform target = null;
        Vector3 origin = playerStateCaptured ? savedPlayerPosition : playerMover.transform.position;
        float bestDist = float.MaxValue;

        foreach (var orb in orbs)
        {
            if (!orb) continue;
            float dist = Vector3.SqrMagnitude(orb.transform.position - origin);
            if (dist < bestDist)
            {
                bestDist = dist;
                target = orb.transform;
            }
        }

        if (!target)
            return;

        Vector3 lookTarget = target.position;
        lookTarget.y = origin.y;

        Vector3 forward = lookTarget - origin;
        if (forward.sqrMagnitude > 0.001f)
            playerMover.transform.rotation = Quaternion.LookRotation(forward.normalized);

        if (playerMover.TryGetComponent<Rigidbody>(out var rb))
            rb.velocity = Vector3.zero;
    }

    void FinishTutorial()
    {
        if (!tutorialActive)
            return;

        tutorialActive = false;
        tutorialCompleted = true;

        ShowOverlay(false);
        RestorePlayerState();

        if (instructions != null)
        {
            instructions.SetVisionMaskActive(true);
            instructions.TriggerSpawnIfNeeded();
            instructions.EnableGameplayUI(true);
        }

        if (timer == null)
            timer = FindObjectOfType<TimerController>(true);
        if (timer != null)
            timer.StartTimer(60f);
    }

    void EnsureUI()
    {
        if (overlayRoot)
            return;

        var canvas = GetComponent<Canvas>();
        if (!canvas)
            canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3500;

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

        continueButton = CreateButton("ContinueButton", new Vector2(0.5f, 0.16f), "Continue", new Color(0.9f, 0.25f, 0.28f, 0.95f));
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

    void SetMessage(string text)
    {
        if (!messagePanel || !messageText)
            return;

        messagePanel.gameObject.SetActive(true);
        messageText.text = text;
    }

    void ShowOverlay(bool show)
    {
        if (!overlayGroup)
            return;

        overlayGroup.alpha = show ? 1f : 0f;
        overlayGroup.blocksRaycasts = show;
        overlayGroup.interactable = show;
    }

    void OnDisable()
    {
        if (tutorialActive)
            FinishTutorial();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
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
}
