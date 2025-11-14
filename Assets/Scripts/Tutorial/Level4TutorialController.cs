using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(205)]
public class Level4TutorialController : MonoBehaviour
{
    static readonly Vector2 MessageSize = new Vector2(660f, 320f);
    static readonly Vector2 MessageAnchor = new Vector2(0.5f, 0.72f);
    static readonly Vector2 MessageOffset = new Vector2(0f, 20f);
    const string LevelFourSceneName = "LvL4";

    static Level4TutorialController instance;
    static bool tutorialCompleted;
    static string lastSceneName;
    static bool sceneLoadedHooked;

    InstructionsManager instructions;
    TimerController timer;
    PlayerMover playerMover;
    SpawnManager spawner;
    ImpostorColorIndicator colorIndicator;
    bool indicatorWasActive;
    bool playerSpawnedByTutorial;
    bool playerWasActive;

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

    bool tutorialActive;
    Camera mainCam;

    GameObject tutorialImpostor;
    NPCIdentity tutorialImpostorIdentity;
    TutorialDrifter tutorialImpostorDrifter;
    Renderer tutorialImpostorRenderer;

    RectTransform arrowRoot;
    RectTransform arrowShaft;
    RectTransform arrowHead;
    static readonly Color ArrowColor = Color.white;
    const float ArrowThickness = 12f;
    const float ArrowHeadSize = 26f;
    static readonly Vector2 ArrowTailOffset = new Vector2(-140f, 55f);
    const float ArrowTargetHeight = 1.6f;

    public static bool TryBeginTutorial(InstructionsManager mgr)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool sceneChanged = sceneName != lastSceneName;
        if (sceneName == LevelFourSceneName && sceneChanged)
            tutorialCompleted = false;
        lastSceneName = sceneName;

        EnsureInstance();
        if (tutorialCompleted || !IsLevelFourScene())
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

        var go = new GameObject("Level4TutorialCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(Level4TutorialController));

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3500;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        go.GetComponent<GraphicRaycaster>().ignoreReversedGraphics = true;

        instance = go.GetComponent<Level4TutorialController>();
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
        spawner = FindObjectOfType<SpawnManager>();
        colorIndicator = ImpostorColorIndicator.Instance;

        EnsurePlayerPresent();
        CapturePlayerState();
        HidePlayerForTutorial();
        EnsureUI();
        EnsureArrow();
        SetupTutorialImpostor();
        SuppressIndicator();

        Time.timeScale = 1f;
        instructions?.SetVisionMaskActive(true);

        ShowOverlay(true);

        string message =
            "<b>Flashing lights hide the impostor</b>\n" +
            "The disco glow hits everyone and makes it confusing to spot the impostor.\n" +
            "Keep your vision sharp so the impostor cannot slip past you.";
        SetMessage(message);

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(FinishTutorial);
        continueButton.gameObject.SetActive(true);
        continueButton.interactable = true;

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

    void FinishTutorial()
    {
        if (!tutorialActive)
            return;

        tutorialActive = false;
        tutorialCompleted = true;

        ShowOverlay(false);
        RestorePlayerState();
        RestorePlayerVisibility();
        CleanupTutorialImpostor();
        RestoreIndicator();

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

        canvas = GetComponent<Canvas>();
        if (!canvas)
            canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3500;
        canvasRect = canvas.GetComponent<RectTransform>();

        overlayRoot = new GameObject("Level4TutorialOverlay", typeof(RectTransform)).GetComponent<RectTransform>();
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
        textRect.anchorMin = new Vector2(0.08f, 0.2f);
        textRect.anchorMax = new Vector2(0.92f, 0.92f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        messageText = textGO.AddComponent<TextMeshProUGUI>();
        messageText.font = GetFontAsset();
        messageText.fontSize = 34f;
        messageText.color = Color.white;
        messageText.alignment = TextAlignmentOptions.Midline;
        messageText.enableWordWrapping = true;

        continueButton = CreateButton("ContinueButton", new Vector2(0.5f, 0.18f), "Continue");
    }

    void EnsureArrow()
    {
        if (arrowRoot || !overlayRoot)
        {
            if (arrowRoot)
                arrowRoot.gameObject.SetActive(false);
            return;
        }

        arrowRoot = new GameObject("TutorialArrow", typeof(RectTransform)).GetComponent<RectTransform>();
        arrowRoot.SetParent(overlayRoot, false);
        arrowRoot.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRoot.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRoot.pivot = new Vector2(0f, 0.5f);
        arrowRoot.anchoredPosition = Vector2.zero;
        arrowRoot.sizeDelta = new Vector2(200f, 40f);
        arrowRoot.gameObject.SetActive(false);

        arrowShaft = new GameObject("Shaft", typeof(RectTransform)).GetComponent<RectTransform>();
        arrowShaft.SetParent(arrowRoot, false);
        arrowShaft.anchorMin = new Vector2(0f, 0.5f);
        arrowShaft.anchorMax = new Vector2(0f, 0.5f);
        arrowShaft.pivot = new Vector2(0f, 0.5f);
        arrowShaft.sizeDelta = new Vector2(120f, ArrowThickness);
        var shaftImage = arrowShaft.gameObject.AddComponent<Image>();
        shaftImage.sprite = GetSolidSprite();
        shaftImage.color = ArrowColor;
        shaftImage.raycastTarget = false;

        arrowHead = new GameObject("Head", typeof(RectTransform)).GetComponent<RectTransform>();
        arrowHead.SetParent(arrowRoot, false);
        arrowHead.anchorMin = new Vector2(0f, 0.5f);
        arrowHead.anchorMax = new Vector2(0f, 0.5f);
        arrowHead.pivot = new Vector2(0.5f, 0.5f);
        arrowHead.sizeDelta = new Vector2(ArrowHeadSize, ArrowHeadSize);
        var headImage = arrowHead.gameObject.AddComponent<Image>();
        headImage.sprite = GetSolidSprite();
        headImage.color = ArrowColor;
        headImage.raycastTarget = false;
        arrowHead.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    Button CreateButton(string name, Vector2 anchor, string label)
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
        image.color = new Color(0.9f, 0.25f, 0.28f, 0.95f);

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
        else
        {
            RestoreIndicator();
            CleanupTutorialImpostor();
            RestorePlayerVisibility();
        }
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    static bool IsLevelFourScene()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.IsValid() && scene.name == LevelFourSceneName;
    }

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        lastSceneName = scene.name;
        if (scene.name == LevelFourSceneName)
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
            name = "Level4TutorialSolidTex",
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

    void SuppressIndicator()
    {
        if (colorIndicator == null)
            colorIndicator = ImpostorColorIndicator.Instance;
        if (!colorIndicator)
            return;
        indicatorWasActive = colorIndicator.gameObject.activeSelf;
        colorIndicator.gameObject.SetActive(false);
    }

    void RestoreIndicator()
    {
        if (!colorIndicator)
            colorIndicator = ImpostorColorIndicator.Instance;
        if (!colorIndicator)
            return;
        colorIndicator.gameObject.SetActive(indicatorWasActive);
    }

    bool SetupTutorialImpostor()
    {
        CleanupTutorialImpostor();
        if (!spawner || !spawner.npcPrefab)
            return false;

        if (!mainCam)
            mainCam = Camera.main;

        Vector3 target = DetermineFocusPoint();
        Vector3 spawnPos = SnapToGround(target);

        tutorialImpostor = Instantiate(spawner.npcPrefab, spawnPos, Quaternion.identity, spawner.npcsParent);
        AlignToGround(tutorialImpostor, spawnPos.y);
        tutorialImpostor.name = "Tutorial4_Impostor";

        tutorialImpostorIdentity = tutorialImpostor.GetComponent<NPCIdentity>() ??
            tutorialImpostor.AddComponent<NPCIdentity>();
        tutorialImpostorIdentity.isImpostor = true;
        tutorialImpostorIdentity.shapeType = PathShape.ShapeType.Circle;
        tutorialImpostorIdentity.colorId = GetImpostorColorId();

        var renderers = tutorialImpostor.GetComponentsInChildren<Renderer>();
        tutorialImpostorRenderer = renderers.Length > 0 ? renderers[0] : null;
        if (spawner.palette)
            tutorialImpostorIdentity.ApplyColor(spawner.palette, renderers);

        foreach (var component in tutorialImpostor.GetComponents<MonoBehaviour>())
        {
            if (component == tutorialImpostorIdentity) continue;
            if (component is NPCWander or PathFollower)
                Destroy(component);
        }

        var rb = tutorialImpostor.GetComponent<Rigidbody>();
        if (rb) rb.velocity = Vector3.zero;

        tutorialImpostorDrifter = tutorialImpostor.AddComponent<TutorialDrifter>();
        tutorialImpostorDrifter.Initialise(1f, 0.4f);

        return true;
    }

    void CleanupTutorialImpostor()
    {
        if (tutorialImpostorDrifter)
            Destroy(tutorialImpostorDrifter);
        if (tutorialImpostor)
            Destroy(tutorialImpostor);
        tutorialImpostor = null;
        tutorialImpostorIdentity = null;
        tutorialImpostorDrifter = null;
        tutorialImpostorRenderer = null;
        if (arrowRoot)
            arrowRoot.gameObject.SetActive(false);
    }

    int GetImpostorColorId()
    {
        if (GameRoundState.Instance != null)
        {
            int id = GameRoundState.Instance.GetImpostorColorId();
            if (id >= 0)
                return id;
        }

        if (spawner && spawner.palette && spawner.palette.Count > 0)
            return Mathf.Clamp(spawner.palette.Count - 1, 0, spawner.palette.Count - 1);

        return 0;
    }

    Vector3 SnapToGround(Vector3 position)
    {
        Vector3 rayOrigin = position + Vector3.up * 3f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            position.y = hit.point.y;
        else
            position.y = 0f;
        return position;
    }

    void AlignToGround(GameObject obj, float groundY)
    {
        if (!obj)
            return;

        var renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer)
        {
            float bottom = renderer.bounds.min.y;
            float offset = groundY - bottom;
            obj.transform.position += new Vector3(0f, offset, 0f);
        }
        else
        {
            obj.transform.position = new Vector3(obj.transform.position.x, groundY, obj.transform.position.z);
        }

        if (spawner && spawner.playerSpawn)
            obj.transform.rotation = Quaternion.Euler(0f, spawner.playerSpawn.eulerAngles.y, 0f);
    }

    Vector3 DetermineFocusPoint()
    {
        if (!mainCam)
            mainCam = Camera.main;

        if (mainCam)
        {
            Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out var hit, 150f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return hit.point;

            var ground = new Plane(Vector3.up, playerMover ? playerMover.transform.position : Vector3.zero);
            if (ground.Raycast(ray, out float enter))
                return ray.GetPoint(enter);
        }

        if (playerStateCaptured)
            return savedPlayerPosition;
        if (playerMover)
            return playerMover.transform.position;
        if (spawner && spawner.playerSpawn)
            return spawner.playerSpawn.position;
        return Vector3.zero;
    }

    void HidePlayerForTutorial()
    {
        if (!playerMover)
            return;
        playerWasActive = playerMover.gameObject.activeSelf;
        playerMover.gameObject.SetActive(false);
    }

    void RestorePlayerVisibility()
    {
        if (!playerMover)
            return;
        bool targetActive = playerSpawnedByTutorial || playerWasActive;
        playerMover.gameObject.SetActive(targetActive);
    }

    void EnsurePlayerPresent()
    {
        if (playerMover)
            return;

        if (spawner == null)
            spawner = FindObjectOfType<SpawnManager>();
        if (spawner == null || !spawner.playerPrefab)
            return;

        Vector3 spawnPos = spawner.playerSpawn ? spawner.playerSpawn.position : Vector3.zero;
        spawnPos.y = 0f;
        Quaternion rotation = Quaternion.identity;
        if (spawner.playerSpawn)
            rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        var player = Instantiate(spawner.playerPrefab, spawnPos, rotation, spawner.playerParent);
        player.name = "Tutorial4_Player";
        playerSpawnedByTutorial = true;
        playerMover = player.GetComponent<PlayerMover>() ?? player.GetComponentInChildren<PlayerMover>();
    }

    void UpdateArrow()
    {
        if (!tutorialActive || !arrowRoot || !messagePanel || !overlayRoot || !tutorialImpostor)
        {
            if (arrowRoot)
                arrowRoot.gameObject.SetActive(false);
            return;
        }

        if (!mainCam)
            mainCam = Camera.main;

        var rend = tutorialImpostorRenderer ? tutorialImpostorRenderer : tutorialImpostor.GetComponentInChildren<Renderer>();
        if (rend)
            tutorialImpostorRenderer = rend;

        Vector3 targetWorld = tutorialImpostor.transform.position + Vector3.up * ArrowTargetHeight;
        if (tutorialImpostorRenderer)
            targetWorld = tutorialImpostorRenderer.bounds.center + Vector3.up * tutorialImpostorRenderer.bounds.extents.y;

        Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(mainCam, targetWorld);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreen, null, out var tipLocal))
        {
            arrowRoot.gameObject.SetActive(false);
            return;
        }

        Vector2 tailLocal = tipLocal + ArrowTailOffset;
        Vector2 delta = tipLocal - tailLocal;
        float distance = delta.magnitude;
        if (distance < 5f)
        {
            arrowRoot.gameObject.SetActive(false);
            return;
        }

        arrowRoot.gameObject.SetActive(true);
        arrowRoot.anchoredPosition = tailLocal;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        arrowRoot.localRotation = Quaternion.Euler(0f, 0f, angle);

        float shaftLength = Mathf.Max(10f, distance - ArrowHeadSize * 0.5f);
        if (arrowShaft)
        {
            arrowShaft.sizeDelta = new Vector2(shaftLength, ArrowThickness);
            arrowShaft.anchoredPosition = Vector2.zero;
        }
        if (arrowHead)
            arrowHead.anchoredPosition = new Vector2(shaftLength, 0f);
    }

    void Update()
    {
        if (!tutorialActive)
            return;
        UpdateArrow();
    }
}
