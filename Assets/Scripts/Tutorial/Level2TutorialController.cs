using System;
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
    SpawnManager spawner;
    Func<NPCIdentity, bool> previousHitFilter;

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
    GameObject tutorialImpostor;
    Renderer[] tutorialImpostorRenderers;
    Material[] tutorialImpostorMaterials;
    RectTransform arrowRoot;
    RectTransform arrowShaft;
    RectTransform arrowHead;
    float colorSwapTimer = -1f;
    bool colorSwapTriggered;

    Step currentStep = Step.Inactive;
    Transform arrowTarget;
    Camera mainCam;
    bool tutorialActive;

    static readonly Vector2 MessageSize = new Vector2(660f, 210f);
    static readonly Vector2 MessageAnchor = new Vector2(0.5f, 0.78f);
    static readonly Vector2 MessageOffset = Vector2.zero;
    static readonly Vector2 ArrowTailOffset = new(-140f, 55f);
    const float ArrowHeadSize = 26f;
    const float ArrowThickness = 12f;
    const float ArrowTargetHeight = 1.4f;
    static readonly Color TutorialRed = new Color(0.93f, 0.17f, 0.19f);
    static readonly Color TutorialOrange = new Color(1f, 0.57f, 0.07f);

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
        spawner = FindObjectOfType<SpawnManager>();
        CaptureVisionRadius();

        CapturePlayerState();
        tutorialCenter = savedPlayerPosition;
        CreateTutorialHazards();

        EnsureUI();
        ShowOverlay(true);

        Time.timeScale = 1f;
        instructions?.SetVisionMaskActive(true);

        ApplyHitFilter();

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
        SetupTutorialImpostor();
        colorSwapTimer = 15f;
        colorSwapTriggered = false;
        UpdateImpostorColor(TutorialRed);
        UpdateImpostorPreviewMessage();
        arrowRoot?.gameObject.SetActive(true);

        continueButton.gameObject.SetActive(false);
        continueButton.interactable = false;

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(ShowPotholeMessage);
        ConfigureSkipButton(true);
    }

    void ShowPotholeMessage()
    {
        HideImpostorArrow();
        CleanupTutorialImpostor();
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
        HideImpostorArrow();
        CleanupTutorialImpostor();
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
            timer.StartTimer(90f);

        RestoreVisionRadius();
        CleanupTutorialHazards();
        CleanupTutorialImpostor();
        ReleaseHitFilter();
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
        EnsureArrow();
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
        shaftImage.raycastTarget = false;

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

    void HideImpostorArrow()
    {
        if (arrowRoot)
            arrowRoot.gameObject.SetActive(false);
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
        if (currentStep == Step.ShowImpostorColor)
        {
            if (!colorSwapTriggered && colorSwapTimer >= 0f)
            {
                colorSwapTimer -= Time.deltaTime;
                if (colorSwapTimer <= 0f)
                {
                    colorSwapTimer = 0f;
                    TriggerColorSwap();
                }
                else
                {
                    UpdateImpostorPreviewMessage();
                }
            }
            UpdateImpostorArrow();
        }
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

    void SetupTutorialImpostor()
    {
        CleanupTutorialImpostor();

        Vector3 spawnPos = DetermineImpostorSpawnPoint();
        if (spawner && spawner.npcPrefab)
        {
            tutorialImpostor = Instantiate(spawner.npcPrefab, spawnPos, Quaternion.identity, spawner.npcsParent);
            tutorialImpostor.name = "Tutorial2_Impostor";
            AlignToGround(tutorialImpostor, spawnPos.y);
            if (tutorialImpostor.TryGetComponent<PathFollower>(out var pf))
                Destroy(pf);
            if (tutorialImpostor.TryGetComponent<NPCWander>(out var wander))
                Destroy(wander);
        }
        else
        {
            tutorialImpostor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tutorialImpostor.transform.position = spawnPos;
            tutorialImpostor.transform.localScale = new Vector3(0.4f, 0.65f, 0.4f);
            if (tutorialImpostor.TryGetComponent<Collider>(out var col))
                Destroy(col);
        }

        tutorialImpostor.hideFlags = HideFlags.HideAndDontSave;

        var drifter = tutorialImpostor.AddComponent<TutorialDrifter>();
        drifter.Initialise(0.8f, 0.4f);

        tutorialImpostorRenderers = tutorialImpostor.GetComponentsInChildren<Renderer>();
        if (tutorialImpostorRenderers != null && tutorialImpostorRenderers.Length > 0)
        {
            tutorialImpostorMaterials = new Material[tutorialImpostorRenderers.Length];
            for (int i = 0; i < tutorialImpostorRenderers.Length; i++)
            {
                var renderer = tutorialImpostorRenderers[i];
                if (!renderer) continue;
                tutorialImpostorMaterials[i] = new Material(renderer.sharedMaterial)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                renderer.sharedMaterial = tutorialImpostorMaterials[i];
            }
        }
    }

    Vector3 DetermineImpostorSpawnPoint()
    {
        Vector3 focus = DetermineFocusPoint();
        return SnapToGround(focus);
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

            var ground = new Plane(Vector3.up, playerStateCaptured ? savedPlayerPosition : (playerMover ? playerMover.transform.position : Vector3.zero));
            if (ground.Raycast(ray, out float enter))
                return ray.GetPoint(enter);
        }

        if (playerStateCaptured)
            return savedPlayerPosition;
        if (playerMover)
            return playerMover.transform.position;
        return Vector3.zero;
    }

    Vector3 SnapToGround(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * 2f;
        if (Physics.Raycast(origin, Vector3.down, out var hit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            position.y = hit.point.y;
        else
            position.y = playerStateCaptured ? savedPlayerPosition.y : position.y;
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
            obj.transform.position += new Vector3(0f, groundY - bottom, 0f);
        }
        else
        {
            obj.transform.position = new Vector3(obj.transform.position.x, groundY, obj.transform.position.z);
        }
    }

    void CleanupTutorialImpostor()
    {
        HideImpostorArrow();
        if (tutorialImpostorMaterials != null)
        {
            foreach (var mat in tutorialImpostorMaterials)
                if (mat) Destroy(mat);
        }
        tutorialImpostorMaterials = null;
        tutorialImpostorRenderers = null;
        if (tutorialImpostor)
            Destroy(tutorialImpostor);
        tutorialImpostor = null;
        colorSwapTimer = -1f;
        colorSwapTriggered = false;
    }

    void UpdateImpostorColor(Color color)
    {
        if (tutorialImpostorMaterials == null)
            return;
        foreach (var mat in tutorialImpostorMaterials)
            if (mat) mat.color = color;
    }

    void UpdateImpostorPreviewMessage()
    {
        string heading =
            "<b>Watch the current " +
            "<color=#FF4D4D>I</color>" +
            "<color=#2ECC71>m</color>" +
            "<color=#3498DB>p</color>" +
            "<color=#FF69B4>o</color>" +
            "<color=#F1C40F>s</color>" +
            "<color=#FF4D4D>t</color>" +
            "<color=#2ECC71>e</color>" +
            "<color=#3498DB>r</color> color</b>\n";
        if (!colorSwapTriggered)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(colorSwapTimer));
            string body =
                "Currently the color is <color=#FF4D4D>Red</color>.\n" +
                $"Color changes in {seconds} seconds.";
            SetMessage(heading + body);
        }
        else
        {
            string body =
                "The color just changed to another color.\n" +
                "It swaps every 15 seconds, so keep an eye on it.";
            SetMessage(heading + body);
        }
    }

    void TriggerColorSwap()
    {
        colorSwapTriggered = true;
        UpdateImpostorColor(TutorialOrange);
        UpdateImpostorPreviewMessage();
        if (continueButton)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.interactable = true;
        }
    }

    void UpdateImpostorArrow()
    {
        if (!arrowRoot || !canvasRect || !tutorialImpostor)
        {
            HideImpostorArrow();
            return;
        }
        if (!mainCam)
            mainCam = Camera.main;

        Vector3 tipWorld = GetImpostorTip();
        Vector2 tipScreen = RectTransformUtility.WorldToScreenPoint(mainCam, tipWorld);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, tipScreen,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out var tipLocal))
        {
            HideImpostorArrow();
            return;
        }

        Vector2 basePos = tipLocal + ArrowTailOffset;
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
        arrowRoot.gameObject.SetActive(true);
    }

    Vector3 GetImpostorTip()
    {
        var primary = GetPrimaryImpostorRenderer();
        if (primary)
            return primary.bounds.center + Vector3.up * primary.bounds.extents.y;
        if (tutorialImpostor)
            return tutorialImpostor.transform.position + Vector3.up * ArrowTargetHeight;
        return Vector3.zero;
    }

    Renderer GetPrimaryImpostorRenderer()
    {
        if (tutorialImpostorRenderers == null)
            return null;
        foreach (var renderer in tutorialImpostorRenderers)
        {
            if (renderer)
                return renderer;
        }
        return null;
    }

    void OnDisable()
    {
        if (tutorialActive)
            FinishTutorial();
        else
            CleanupTutorialHazards();
        RestoreVisionRadius();
        CleanupTutorialImpostor();
        ReleaseHitFilter();
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

    void ApplyHitFilter()
    {
        if (ClickToSmite.HitFilter == FilterHits)
            return;
        previousHitFilter = ClickToSmite.HitFilter;
        ClickToSmite.HitFilter = FilterHits;
    }

    void ReleaseHitFilter()
    {
        if (ClickToSmite.HitFilter == FilterHits)
            ClickToSmite.HitFilter = previousHitFilter;
        previousHitFilter = null;
    }

    bool FilterHits(NPCIdentity identity)
    {
        if (previousHitFilter != null && !previousHitFilter(identity))
            return false;

        if (!tutorialActive)
            return true;

        return currentStep != Step.ShowImpostorColor;
    }
}
