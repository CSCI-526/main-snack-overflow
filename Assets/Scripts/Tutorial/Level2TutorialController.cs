using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(205)]
public class Level2TutorialController : MonoBehaviour
{
    enum Step
    {
        Inactive,
        ShowImpostorColor,
        ShowPothole,
        ShowMud,
        Finished
    }

    static Level2TutorialController instance;
    static bool tutorialCompleted;
    static string lastSceneName;
    static bool sceneLoadedHooked;

    InstructionsManager instructions;
    TimerController timer;
    VisionMaskController visionMask;
    PlayerMover playerMover;

    Vector3 savedPlayerPosition;
    Quaternion savedPlayerRotation;
    bool playerStateCaptured;
    bool playerMoverWasEnabled;

    Transform tutorialPothole;
    Transform tutorialMud;
    Material tutorialPotholeMaterial;
    Material tutorialMudMaterial;
    Vector3 tutorialCenter;


    Canvas canvas;
    RectTransform canvasRect;

    CanvasGroup overlayGroup;
    RectTransform overlayRoot;
    RectTransform messagePanel;
    TextMeshProUGUI messageText;
    Button continueButton;
    Button skipButton;
    float savedVisionRadius = -1f;
    bool visionRadiusCaptured;

    Step currentStep = Step.Inactive;
    Transform arrowTarget;
    Camera mainCam;
    bool tutorialActive;

    static readonly Vector2 MessageSize = new Vector2(660f, 210f);
    static readonly Vector2 MessageAnchor = new Vector2(0.5f, 0.78f);
    static readonly Vector2 MessageOffset = Vector2.zero;

    public static bool TryBeginTutorial(InstructionsManager mgr)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool sceneChanged = sceneName != lastSceneName;
        if (sceneName == "LvL2" && sceneChanged)
            tutorialCompleted = false;
        lastSceneName = sceneName;

        EnsureInstance();
        if (tutorialCompleted || !IsLevelTwoScene())
            return false;

        return instance != null && instance.InternalBegin(mgr);
    }

    void Awake()
    {
        instance = this;
        canvas = GetComponent<Canvas>();
        canvasRect = GetComponent<RectTransform>();
        lastSceneName = SceneManager.GetActiveScene().name;
        tutorialCompleted = false;
        EnsureSceneLoadedHooked();
    }

    static void EnsureInstance()
    {
        if (instance != null)
            return;

        var go = new GameObject("Level2TutorialCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(Level2TutorialController));

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3500;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        go.GetComponent<GraphicRaycaster>().ignoreReversedGraphics = true;

        instance = go.GetComponent<Level2TutorialController>();
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
        playerMover = FindObjectOfType<PlayerMover>(true);
        CaptureVisionRadius();

        CapturePlayerState();
        tutorialCenter = savedPlayerPosition;
        CreateTutorialHazards();

        EnsureUI();
        ShowOverlay(true);

        Time.timeScale = 1f;
        instructions?.SetVisionMaskActive(true);

        mainCam = Camera.main;
        tutorialActive = true;
        currentStep = Step.ShowImpostorColor;
        ShowImpostorColorMessage();
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

    void ShowImpostorColorMessage()
    {
        currentStep = Step.ShowImpostorColor;
        string message =
            "<b>Watch the current " +
            "<color=#FF4D4D>I</color>" +
            "<color=#2ECC71>m</color>" +
            "<color=#3498DB>p</color>" +
            "<color=#FF69B4>o</color>" +
            "<color=#F1C40F>s</color>" +
            "<color=#FF4D4D>t</color>" +
            "<color=#2ECC71>e</color>" +
            "<color=#3498DB>r</color> color</b>\n" +
            "Their identifying color shifts every 15 seconds.\n" +
            "Keep an eye on the change so you do not lose track.";

        SetMessage(message);

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(ShowPotholeMessage);
        continueButton.gameObject.SetActive(true);
        continueButton.interactable = true;
        ConfigureSkipButton(true);
    }

    void ShowPotholeMessage()
    {
        string message =
            "<b>Avoid <color=#5C5C5C>Grey Potholes</color></b>\n" +
            "Stepping on a pothole puts you in a temporary timeout.\n" +
            "Keep your path clear.";

        SetMessage(message);
        FocusOnTag("Pothole");

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(ShowMudMessage);
        continueButton.gameObject.SetActive(true);
        continueButton.interactable = true;
        ConfigureSkipButton(true);
    }

    void ShowMudMessage()
    {
        string message =
            "<b>Watch the <color=#5C2B0F>Brown Mud Patches</color></b>\n" +
            "Mud patches slow your movement speed.\n" +
            "Stay on clean roads to keep pace.\n" +
            "Press Continue to begin the mission.";

        SetMessage(message);
        FocusOnTag("Mud");

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(FinishTutorial);
        ConfigureSkipButton(false);
    }

    void FinishTutorial()
    {
        if (!tutorialActive)
            return;

        tutorialActive = false;
        tutorialCompleted = true;
        currentStep = Step.Finished;

        ShowOverlay(false);
        ConfigureSkipButton(false);
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

        RestoreVisionRadius();
        CleanupTutorialHazards();
    }

    void EnsureUI()
    {
        if (overlayRoot)
            return;

        canvas = GetComponent<Canvas>();
        canvasRect = GetComponent<RectTransform>();

        overlayRoot = new GameObject("Level2TutorialOverlay", typeof(RectTransform)).GetComponent<RectTransform>();
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
        textRect.anchorMin = new Vector2(0.06f, 0.1f);
        textRect.anchorMax = new Vector2(0.94f, 0.9f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        messageText = textGO.AddComponent<TextMeshProUGUI>();
        messageText.font = GetFontAsset();
        messageText.fontSize = 34f;
        messageText.color = Color.white;
        messageText.alignment = TextAlignmentOptions.Midline;
        messageText.enableWordWrapping = true;

        continueButton = CreateButton("ContinueButton", new Vector2(0.5f, 0.27f), "Continue", new Color(0.9f, 0.25f, 0.28f, 0.95f));
        skipButton = CreateButton("SkipTutorialButton", new Vector2(0.5f, 0.18f), "Skip Tutorial", new Color(0.25f, 0.28f, 0.33f, 0.95f));
        skipButton.onClick.AddListener(FinishTutorial);
    }

    Button CreateButton(string name, Vector2 anchor, string label, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(overlayRoot, false);
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

    void FocusOnTag(string tag)
    {
        if (!playerMover)
            playerMover = FindObjectOfType<PlayerMover>(true);

        Transform target = tag == "Pothole" ? tutorialPothole :
            tag == "Mud" ? tutorialMud : null;
        if (!target)
        {
            CreateTutorialHazards();
            target = tag == "Pothole" ? tutorialPothole :
                tag == "Mud" ? tutorialMud : null;
        }
        if (!target)
            target = FindTargetWithTag(tag);
        if (!target)
        {
                        return;
        }

        if (playerMover)
        {
            Vector3 offset = tag == "Mud"
                ? new Vector3(1.5f, 0f, -0.6f)
                : new Vector3(-1.5f, 0f, -0.4f);

            Vector3 newPos = target.position + offset;
            newPos.y = savedPlayerPosition.y;

            playerMover.transform.position = newPos;
            playerMover.transform.LookAt(target.position);
            if (playerMover.TryGetComponent<Rigidbody>(out var rb))
                rb.velocity = Vector3.zero;
        }

        if (playerMover)
        {
            playerMover.transform.position = tutorialCenter;
            playerMover.transform.LookAt(target.position);
            if (playerMover.TryGetComponent<Rigidbody>(out var rb))
                rb.velocity = Vector3.zero;
        }
    }

    Transform FindTargetWithTag(string tag)
    {
        var objs = GameObject.FindGameObjectsWithTag(tag);
        if (objs == null || objs.Length == 0)
            return null;

        Vector3 origin = playerMover ? playerMover.transform.position :
            (playerStateCaptured ? savedPlayerPosition : Vector3.zero);

        GameObject best = null;
        float bestDist = float.MaxValue;
        foreach (var obj in objs)
        {
            if (!obj) continue;
            float d = Vector3.SqrMagnitude(obj.transform.position - origin);
            if (d < bestDist)
            {
                best = obj;
                bestDist = d;
            }
        }

        return best ? best.transform : objs[0].transform;
    }

    void CreateTutorialHazards()
    {
        if (!playerMover)
            playerMover = FindObjectOfType<PlayerMover>(true);

        Vector3 origin = tutorialCenter;

        if (tutorialPothole == null)
        {
            tutorialPothole = CreateHazard(
                name: "Tutorial_Pothole",
                tag: "Pothole",
                position: origin + new Vector3(1.1f, 0f, 0.9f),
                scale: new Vector3(0.8f, 0.02f, 0.8f),
                color: new Color32(87, 87, 87, 255),
                shape: PrimitiveType.Cylinder,
                out tutorialPotholeMaterial);
        }

        if (tutorialMud == null)
        {
            tutorialMud = CreateHazard(
                name: "Tutorial_Mud",
                tag: "Mud",
                position: origin + new Vector3(-1.2f, 0f, -0.7f),
                scale: new Vector3(1.5f, 0.02f, 1.2f),
                color: new Color32(120, 78, 38, 255),
                shape: PrimitiveType.Cube,
                out tutorialMudMaterial);
        }
    }

    Transform CreateHazard(string name, string tag, Vector3 position, Vector3 scale,
        Color color, PrimitiveType shape, out Material matRef)
    {
        var go = GameObject.CreatePrimitive(shape);
        go.name = name;
        go.tag = tag;
        go.layer = LayerMask.NameToLayer("Default");
        go.transform.position = new Vector3(position.x, playerMover ? playerMover.transform.position.y : 0f, position.z);
        go.transform.localScale = scale;
        if (go.TryGetComponent<Collider>(out var col))
            Destroy(col);

        matRef = CreateTutorialMaterial(color);
        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = matRef;
        go.hideFlags = HideFlags.HideAndDontSave;
        return go.transform;
    }

    void CleanupTutorialHazards()
    {
        if (tutorialPothole)
            Destroy(tutorialPothole.gameObject);
        if (tutorialMud)
            Destroy(tutorialMud.gameObject);
        if (tutorialPotholeMaterial)
            Destroy(tutorialPotholeMaterial);
        if (tutorialMudMaterial)
            Destroy(tutorialMudMaterial);
        tutorialPothole = null;
        tutorialMud = null;
        tutorialPotholeMaterial = null;
        tutorialMudMaterial = null;
    }

    Material CreateTutorialMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader)
        {
            color = color,
            hideFlags = HideFlags.HideAndDontSave
        };
        return mat;
    }

    void Update()
    {
        if (!tutorialActive)
            return;
    }

    void ShowOverlay(bool show)
    {
        if (!overlayGroup)
            return;

        overlayGroup.alpha = show ? 1f : 0f;
        overlayGroup.blocksRaycasts = show;
        overlayGroup.interactable = show;
    }

    void ConfigureSkipButton(bool visible)
    {
        if (!skipButton)
            return;
        skipButton.gameObject.SetActive(visible);
        skipButton.interactable = visible;
    }

    void OnDisable()
    {
        if (tutorialActive)
            FinishTutorial();
        else
            CleanupTutorialHazards();
        RestoreVisionRadius();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    static bool IsLevelTwoScene()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.IsValid() && scene.name == "LvL2";
    }

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        lastSceneName = scene.name;
        if (scene.name == "LvL2")
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
            name = "Level2TutorialSolidTex",
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

    void CaptureVisionRadius()
    {
        if (visionMask == null || visionRadiusCaptured)
            return;
        savedVisionRadius = visionMask.currentRadius;
        visionRadiusCaptured = true;
    }

    void RestoreVisionRadius()
    {
        if (!visionRadiusCaptured || visionMask == null)
            return;
        visionMask.UpdateRadius(savedVisionRadius);
        visionRadiusCaptured = false;
    }
}
