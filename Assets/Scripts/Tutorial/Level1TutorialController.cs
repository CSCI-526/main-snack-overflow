using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Lightweight onboarding for Level 1.
/// Spawns a single red impostor, a differently-coloured civilian, guides the player through movement,
/// shows click instructions, then hands control back to the normal spawner/timer flow.
/// </summary>
[DefaultExecutionOrder(200)]
public class Level1TutorialController : MonoBehaviour
{
    public static Level1TutorialController Instance { get; private set; }

    enum Step
    {
        Inactive,
        AwaitMovement,
        ClickImpostor,
        VisionReview,
        ClickCivilian,
        ShowCivilian,
        TimerBriefing,
        Finished
    }

static readonly Vector2 MessageSizeLarge = new(660f, 190f);
static readonly Vector2 MessageSizeMedium = new(600f, 160f);
    static readonly Vector2 MessageSizeSmall = new(480f, 120f);

    static readonly Vector2 MovementAnchor = new(0.5f, 0.82f);
    static readonly Vector2 MovementOffset = new(0f, -25f);

    static readonly Vector2 ActionAnchor = new(0.5f, 0.78f);
    static readonly Vector2 ActionOffset = new(0f, -15f);

    static readonly Vector2 CivilianAnchor = new(0.5f, 0.78f);
    static readonly Vector2 CivilianOffset = new(0f, -15f);
    static readonly Vector2 TimerAnchor = new(0.5f, 0.82f);
    static readonly Vector2 TimerOffset = new(0f, -5f);

readonly Vector2 arrowTailOffset = new(-140f, 55f);
const float arrowTargetHeight = 1.6f;

const string AccentHex = "#F8B938";
const string CivilianPenaltyMessage = SunbeamManager.DefaultCivilianMessage;
const string CivilianColorKeyword = "orange";
static readonly Color TutorialCivilianColor = new Color32(230, 110, 25, 255);
static readonly Color32[] AdditionalCivilianColors =
{
    new Color32(96, 4, 48, 255),
    new Color32(150, 0, 0, 255)
};
static readonly Vector3[] AdditionalCivilianOffsets =
{
    new Vector3(6.5f, 0f, 1.8f),
    new Vector3(-1.6f, 0f, -2.8f)
};

    InstructionsManager instructions;
    TimerController timer;
    SpawnManager spawner;
    ClickToSmite clicker;

    Canvas canvas;
    RectTransform canvasRect;
    Camera mainCam;

    CanvasGroup overlayGroup;
    RectTransform overlayRoot;
    RectTransform messagePanel;
    TextMeshProUGUI messageText;
RectTransform arrowRoot;
RectTransform arrowShaftRect;
RectTransform arrowHeadRect;
const float arrowHeadWidth = 26f;
const float arrowShaftHeight = 12f;
Button continueButton;
Button skipTutorialButton;
KillTextController cachedKillText;
bool pendingVisionReduction;
float pendingVisionTargetRadius;

    static Sprite solidSprite;
    static TMP_FontAsset cachedFont;
    static bool tutorialCompleted;

    Step currentStep = Step.Inactive;
    bool tutorialActive;
    bool movementCaptured;

    GameObject tutorialPlayer;
    GameObject tutorialImpostor;
    GameObject tutorialCivilian;
    NPCIdentity tutorialImpostorId;
    NPCIdentity tutorialCivilianId;
TutorialDrifter impostorDrifter;
TutorialDrifter civilianDrifter;
string civilianColorHex = "#F48028";
string impostorColorHex = "#FF4F4F";
TutorialVisionHint visionHint;
    int tutorialCivilianColorId = -1;
    Vector3 tutorialCivilianSpawnPos;
    string[] civilianColorHexSequence;
    class CivilianShowcase
    {
        public GameObject npc;
        public NPCIdentity identity;
        public TutorialDrifter drifter;
        public RectTransform arrowRoot;
        public RectTransform arrowShaft;
        public RectTransform arrowHead;
    }

    readonly List<CivilianShowcase> additionalCivilians = new();

    NPCIdentity targetImpostor;
    NPCIdentity targetCivilian;

    void Awake()
    {
        Instance = this;
        canvas = GetComponent<Canvas>();
        canvasRect = GetComponent<RectTransform>();
        tutorialCompleted = false;
    }

    void OnDisable()
    {
        if (tutorialActive)
            StopTutorialInternal();

        if (ClickToSmite.HitFilter == FilterHits)
            ClickToSmite.HitFilter = null;

        ClickToSmite.OnHitResolved -= HandleShotResolved;
        ClickToSmite.SuppressGameState = false;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsTutorialActive => tutorialActive;

    public bool TryBeginTutorial(InstructionsManager mgr)
    {
        if (tutorialCompleted || tutorialActive || !IsLevelOneScene())
            return false;

        instructions = mgr;
        timer = mgr ? mgr.timerController : FindObjectOfType<TimerController>(true);
        spawner = FindObjectOfType<SpawnManager>();
        clicker = FindObjectOfType<ClickToSmite>(true);
        mainCam = Camera.main;

        EnsureUI();
        ShowOverlay(true);

        Time.timeScale = 1f;
        instructions.SetVisionMaskActive(true);
        VisionMaskController.Instance?.ResetRadius();

        if (!SetupTutorialActors())
        {
            ShowOverlay(false);
            return false;
        }

        if (clicker != null)
            clicker.enabled = true;

        ClickToSmite.OnHitResolved += HandleShotResolved;
        ClickToSmite.HitFilter = FilterHits;
        ClickToSmite.SuppressGameState = true;

        tutorialActive = true;
        movementCaptured = false;
        currentStep = Step.AwaitMovement;

        ShowMovementMessage();
        continueButton.gameObject.SetActive(false);
        arrowRoot.gameObject.SetActive(false);
        ConfigureSkipButton(true);

        // Vision hint overlay (placed near vision circle edge)
        var hintGO = new GameObject("VisionHint", typeof(RectTransform), typeof(Image));
        var hintRect = hintGO.GetComponent<RectTransform>();
        hintRect.SetParent(overlayRoot, false);
        hintRect.anchorMin = Vector2.one;
        hintRect.anchorMax = Vector2.one;
        hintRect.pivot = new Vector2(1f, 1f);
        hintRect.anchoredPosition = new Vector2(-60f, -40f);
        hintRect.sizeDelta = new Vector2(460f, 270f);

        var hintBackground = hintGO.GetComponent<Image>();
        hintBackground.sprite = GetSolidSprite();
        hintBackground.color = new Color(0f, 0f, 0f, 0.88f);
        hintBackground.raycastTarget = false;

        var hintArrowGroup = new GameObject("ArrowGroup", typeof(RectTransform)).GetComponent<RectTransform>();
        hintArrowGroup.SetParent(hintRect, false);
        hintArrowGroup.anchorMin = new Vector2(0f, 0.5f);
        hintArrowGroup.anchorMax = new Vector2(0f, 0.5f);
        hintArrowGroup.pivot = new Vector2(0f, 0.5f);
        hintArrowGroup.anchoredPosition = new Vector2(20f, -46f);
        hintArrowGroup.sizeDelta = Vector2.zero;

        var hintShaftGO = new GameObject("Shaft", typeof(RectTransform), typeof(Image));
        var hintShaftRect = hintShaftGO.GetComponent<RectTransform>();
        hintShaftRect.SetParent(hintArrowGroup, false);
        hintShaftRect.anchorMin = new Vector2(0f, 0.5f);
        hintShaftRect.anchorMax = new Vector2(0f, 0.5f);
        hintShaftRect.pivot = new Vector2(0f, 0.5f);
        hintShaftRect.sizeDelta = new Vector2(56f, 6f);
        hintShaftRect.anchoredPosition = Vector2.zero;

        var hintShaftImage = hintShaftGO.GetComponent<Image>();
        hintShaftImage.sprite = GetSolidSprite();
        hintShaftImage.color = Color.white;
        hintShaftImage.raycastTarget = false;

        var hintHeadGO = new GameObject("Head", typeof(RectTransform), typeof(Image));
        var hintHeadRect = hintHeadGO.GetComponent<RectTransform>();
        hintHeadRect.SetParent(hintArrowGroup, false);
        hintHeadRect.anchorMin = new Vector2(0f, 0.5f);
        hintHeadRect.anchorMax = new Vector2(0f, 0.5f);
        hintHeadRect.pivot = new Vector2(0.5f, 0.5f);
        hintHeadRect.sizeDelta = new Vector2(18f, 18f);
        hintHeadRect.anchoredPosition = new Vector2(hintShaftRect.sizeDelta.x, 0f);
        hintHeadRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

        var hintHeadImage = hintHeadGO.GetComponent<Image>();
        hintHeadImage.sprite = GetSolidSprite();
        hintHeadImage.color = Color.white;
        hintHeadImage.raycastTarget = false;

        var hintTextGO = new GameObject("Label", typeof(RectTransform));
        var hintTextRect = hintTextGO.GetComponent<RectTransform>();
        hintTextRect.SetParent(hintRect, false);
        hintTextRect.anchorMin = new Vector2(0f, 1f);
        hintTextRect.anchorMax = new Vector2(1f, 1f);
        hintTextRect.pivot = new Vector2(0.5f, 1f);
        hintTextRect.offsetMin = new Vector2(36f, -210f);
        hintTextRect.offsetMax = new Vector2(-36f, -100f);

        var hintTMP = hintTextGO.AddComponent<TextMeshProUGUI>();
        hintTMP.font = GetFontAsset();
        hintTMP.fontSize = 34f;
        hintTMP.alignment = TextAlignmentOptions.MidlineLeft;
        hintTMP.color = Color.white;
        hintTMP.enableWordWrapping = true;
        hintTMP.richText = true;
        hintTMP.raycastTarget = false;

        var hintButtonGO = new GameObject("ContinueButton", typeof(RectTransform));
        var hintButtonRect = hintButtonGO.GetComponent<RectTransform>();
        hintButtonRect.SetParent(hintRect, false);
        hintButtonRect.anchorMin = new Vector2(0.5f, 0f);
        hintButtonRect.anchorMax = new Vector2(0.5f, 0f);
        hintButtonRect.pivot = new Vector2(0.5f, 0f);
        hintButtonRect.anchoredPosition = new Vector2(0f, -32f);
        hintButtonRect.sizeDelta = new Vector2(240f, 60f);

        var hintButtonImage = hintButtonGO.AddComponent<Image>();
        hintButtonImage.sprite = GetSolidSprite();
        hintButtonImage.color = new Color(0.9f, 0.25f, 0.28f, 0.95f);

        var hintButtonShadow = hintButtonGO.AddComponent<Shadow>();
        hintButtonShadow.effectDistance = new Vector2(0f, -3f);
        hintButtonShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);

        var hintButton = hintButtonGO.AddComponent<Button>();

        var hintButtonLabelGO = new GameObject("Text", typeof(RectTransform));
        var hintButtonLabelRect = hintButtonLabelGO.GetComponent<RectTransform>();
        hintButtonLabelRect.SetParent(hintButtonGO.transform, false);
        hintButtonLabelRect.anchorMin = Vector2.zero;
        hintButtonLabelRect.anchorMax = Vector2.one;
        hintButtonLabelRect.offsetMin = Vector2.zero;
        hintButtonLabelRect.offsetMax = Vector2.zero;

        var hintButtonText = hintButtonLabelGO.AddComponent<TextMeshProUGUI>();
        hintButtonText.font = GetFontAsset();
        hintButtonText.fontSize = 30f;
        hintButtonText.alignment = TextAlignmentOptions.Center;
        hintButtonText.color = Color.white;
        hintButtonText.text = "Continue";
        hintButtonText.raycastTarget = false;

        visionHint = hintGO.AddComponent<TutorialVisionHint>();
        visionHint.arrowDistanceScale = 0.5f;
        visionHint.Configure(hintRect, hintTMP, hintArrowGroup, hintShaftRect, hintHeadRect, hintTextRect, hintButton);
        visionHint.circlePadding = 0f;
        visionHint.Hide();

        return true;
    }

    bool SetupTutorialActors()
    {
        if (!spawner)
            return false;

        ClearTutorialActors();
        EnsurePlayerPresent();

        if (!tutorialPlayer)
            tutorialPlayer = FindObjectOfType<PlayerMover>()?.gameObject;
        if (!tutorialPlayer)
            return false;

        Vector3 origin = spawner.playerSpawn ? spawner.playerSpawn.position : tutorialPlayer.transform.position;
        origin.y = 0f;

        Vector3 impostorPos = SnapToGround(origin + new Vector3(2.5f, 0f, 2f));
        Vector3 civilianPos = SnapToGround(origin + new Vector3(-2.5f, 0f, 2f));

        int impostorColorId = GetImpostorColorId();
        impostorColorHex = GetColorHex(impostorColorId, impostorColorHex);
        tutorialImpostor = InstantiateTutorialNpc(true, impostorPos, impostorColorId, out tutorialImpostorId, out impostorDrifter);

        int civilianColorId = GetCivilianColorId(impostorColorId);
        tutorialCivilian = InstantiateTutorialNpc(false, civilianPos, civilianColorId, out tutorialCivilianId, out civilianDrifter);
        civilianColorHex = ColorUtility.ToHtmlStringRGB(TutorialCivilianColor);
        tutorialCivilianColorId = civilianColorId;
        tutorialCivilianSpawnPos = civilianPos;

        EnsureAdditionalCivilianShowcase(forceRebuild: true);

        targetImpostor = tutorialImpostorId;
        targetCivilian = tutorialCivilianId;

        if (ImpostorTracker.Instance != null)
        {
            ImpostorTracker.Instance.ResetCount();
            if (tutorialImpostorId != null)
                ImpostorTracker.Instance.RegisterImpostor();
        }

        return tutorialImpostor && tutorialCivilian;
    }

    void EnsurePlayerPresent()
    {
        var existing = FindObjectOfType<PlayerMover>();
        if (existing != null)
        {
            tutorialPlayer = existing.gameObject;
            if (spawner && spawner.playerSpawn)
            {
                Vector3 p = spawner.playerSpawn.position;
                p.y = 0f;
                existing.transform.position = p;
            }
            return;
        }

        if (!spawner || !spawner.playerPrefab)
            return;

        Vector3 spawnPos = spawner.playerSpawn ? spawner.playerSpawn.position : Vector3.zero;
        spawnPos.y = 0f;

        tutorialPlayer = Instantiate(spawner.playerPrefab, spawnPos, Quaternion.identity, spawner.playerParent);
    }

    GameObject InstantiateTutorialNpc(bool isImpostor, Vector3 position, int colorId, out NPCIdentity identity, out TutorialDrifter drifter, Color? civilianOverrideColor = null)
    {
        identity = null;
        drifter = null;

        if (!spawner || !spawner.npcPrefab)
            return null;

        Vector3 spawnPos = SnapToGround(position);
        var npc = Instantiate(spawner.npcPrefab, spawnPos, Quaternion.identity, spawner.npcsParent);
        AlignToGround(npc, spawnPos.y);
        npc.name = isImpostor ? "Tutorial_Impostor" : "Tutorial_Civilian";

        identity = npc.GetComponent<NPCIdentity>();
        if (!identity) identity = npc.AddComponent<NPCIdentity>();
        identity.isImpostor = isImpostor;
        identity.shapeType = PathShape.ShapeType.Circle;
        identity.colorId = colorId;

        var renderers = npc.GetComponentsInChildren<Renderer>();
        if (spawner.palette)
            identity.ApplyColor(spawner.palette, renderers);

        if (!isImpostor)
        {
            Color tint = civilianOverrideColor ?? TutorialCivilianColor;
            foreach (var r in renderers)
            {
                if (!r) continue;
                var mat = new Material(r.material);
                mat.color = tint;
                r.material = mat;
            }
        }

        foreach (var component in npc.GetComponents<MonoBehaviour>())
        {
            if (component == identity) continue;
            if (component is NPCWander or PathFollower)
                Destroy(component);
        }

        var rb = npc.GetComponent<Rigidbody>();
        if (rb) rb.velocity = Vector3.zero;

        drifter = npc.AddComponent<TutorialDrifter>();
        drifter.Initialise(radius: 1f, speed: 0.35f);

        return npc;
    }

    int GetImpostorColorId()
    {
        if (GameRoundState.Instance != null)
        {
            int id = GameRoundState.Instance.GetImpostorColorId();
            if (id >= 0) return id;
        }

        if (spawner && spawner.palette && spawner.palette.Count > 0)
            return Mathf.Clamp(spawner.palette.Count - 1, 0, spawner.palette.Count - 1);

        return 0;
    }

    int GetCivilianColorId(int avoidColorId)
    {
        if (!spawner || !spawner.palette || spawner.palette.Count == 0)
            return avoidColorId;

        int fallback = -1;
        for (int i = 0; i < spawner.palette.Count; i++)
        {
            if (i == avoidColorId) continue;
            string name = spawner.palette.GetName(i);
            if (NameMatchesKeyword(name, "orange"))
                return i;
            if (!NameMatchesKeyword(name, "red") && fallback == -1)
                fallback = i;
        }

        if (fallback != -1)
            return fallback;

        return (avoidColorId + 1) % spawner.palette.Count;
    }

    static bool NameMatchesKeyword(string name, string keyword)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(keyword))
            return false;
        return name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    string GetColorHex(int colorId, string fallback)
    {
        if (spawner && spawner.palette && colorId >= 0 && colorId < spawner.palette.Count)
        {
            var state = GameRoundState.Instance;
            bool isImpostorColor = state != null && state.IsImpostorColor(colorId);
            Color c = spawner.palette.GetForRole(colorId, isImpostorColor);
            return ColorUtility.ToHtmlStringRGB(c);
        }
        return fallback.TrimStart('#');
    }

    string GetColorDisplayName(int colorId)
    {
        if (!spawner || !spawner.palette || colorId < 0 || colorId >= spawner.palette.Count)
            return "civilian";

        string name = spawner.palette.GetName(colorId);
        if (string.IsNullOrWhiteSpace(name))
            return "civilian";

        name = name.Replace('_', ' ');
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.Trim());
    }

    void ListenForMovementInput()
    {
        if (movementCaptured)
            return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            movementCaptured = true;
            AdvanceToImpostorStep();
        }
    }

    void AdvanceToImpostorStep()
    {
        currentStep = Step.ClickImpostor;
        targetImpostor = tutorialImpostorId != null ? tutorialImpostorId : FindImpostor();

        ShowImpostorMessage();
        MaintainImpostorArrow();
    }

    void MaintainImpostorArrow()
    {
        if (targetImpostor == null)
        {
            targetImpostor = FindImpostor();
            if (targetImpostor == null)
            {
                arrowRoot.gameObject.SetActive(false);
                return;
            }
        }

        arrowRoot.gameObject.SetActive(true);
        UpdateArrowToTarget(targetImpostor.transform, 1.0f);
    }

    void AdvanceToCivilianStep()
    {
        currentStep = Step.ShowCivilian;
        targetCivilian = null;
        arrowRoot?.gameObject.SetActive(false);
        HideAdditionalCivilianArrows();

        ApplyPendingVisionPenalty();
        ShowCivilianMessage();
        continueButton.gameObject.SetActive(true);
        continueButton.interactable = true;
        RefreshOverlayInteractivity();
        ShowCivilianVisionPrompt();
    }

    void MaintainCivilianArrow()
    {
        if (targetCivilian == null)
        {
            targetCivilian = FindCivilian();
            if (targetCivilian == null && arrowRoot)
                arrowRoot.gameObject.SetActive(false);
        }

        if (targetCivilian != null)
        {
            arrowRoot.gameObject.SetActive(true);
            UpdateArrowToTarget(targetCivilian.transform, 0.95f);
        }

        foreach (var showcase in additionalCivilians)
            MaintainAdditionalCivilianArrow(showcase);
    }

    void UpdateArrowToTarget(Transform target, float yOffsetMultiplier = 1f,
        RectTransform customRoot = null, RectTransform customShaft = null, RectTransform customHead = null)
    {
        var root = customRoot ? customRoot : arrowRoot;
        var shaft = customShaft ? customShaft : arrowShaftRect;
        var head = customHead ? customHead : arrowHeadRect;

        if (!target || !canvasRect || !root)
            return;

        if (!mainCam)
            mainCam = Camera.main;

        var renderer = target.GetComponentInChildren<Renderer>();
        Vector3 boundsCenter = renderer ? renderer.bounds.center : target.position;
        float halfHeight = renderer ? renderer.bounds.extents.y : 0.8f;

        Vector3 worldTip = boundsCenter + Vector3.up * halfHeight * yOffsetMultiplier;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCam, worldTip);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 localPoint))
        {
            Vector2 basePos = localPoint + arrowTailOffset;
            root.anchoredPosition = basePos;

            Vector2 delta = localPoint - basePos;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            root.localEulerAngles = new Vector3(0f, 0f, angle);

            float tipDistance = delta.magnitude;
            float shaftLength = Mathf.Max(10f, tipDistance - arrowHeadWidth * 0.5f);
            if (shaft != null)
            {
                shaft.sizeDelta = new Vector2(shaftLength, arrowShaftHeight);
                shaft.anchoredPosition = Vector2.zero;
            }

            if (head != null)
                head.anchoredPosition = new Vector2(shaftLength, 0f);
        }
    }

    NPCIdentity FindImpostor()
    {
        var identities = FindObjectsOfType<NPCIdentity>();
        foreach (var id in identities)
        {
            if (id && id.isImpostor)
                return id;
        }
        return null;
    }

    NPCIdentity FindCivilian()
    {
        var identities = FindObjectsOfType<NPCIdentity>();
        foreach (var id in identities)
        {
            if (id && !id.isImpostor)
                return id;
        }
        return null;
    }

    void HandleShotResolved(NPCIdentity id, bool correct)
    {
        if (!tutorialActive)
            return;

        if (id != null)
            DisableDrifter(id);

        if (currentStep == Step.ClickImpostor && correct && id != null && targetImpostor != null && id == targetImpostor)
        {
            if (ImpostorTracker.Instance != null)
                ImpostorTracker.Instance.ResetCount();

            targetImpostor = null;
            arrowRoot.gameObject.SetActive(false);
            ShowVisionGrowthPrompt();
            return;
        }

        if (currentStep == Step.ClickCivilian && !correct)
        {
            ShowCivilianPenaltyNotification();
            RemoveCivilianFromScene(id);
            AdvanceToCivilianStep();
            return;
        }
    }

    void DisableDrifter(NPCIdentity id)
    {
        if (!id) return;

        if (tutorialImpostorId == id && impostorDrifter)
            impostorDrifter.enabled = false;
        if (tutorialCivilianId == id && civilianDrifter)
            civilianDrifter.enabled = false;
    }

    void ShowCivilianVisionPrompt()
    {
        var maskController = VisionMaskController.Instance;
        RectTransform maskRect = maskController ? maskController.MaskRect : null;

        if (maskController)
            maskController.UpdateRadius(maskController.initialRadius);

        float radius = maskController ? maskController.currentRadius : 0.26f;

        if (visionHint != null && maskRect != null)
        {
            visionHint.Show(
                increase: false,
                maskRect: maskRect,
                radius: radius,
                customMessage: "Killing civilians shrinks your vision and slows your speed.",
                requireContinue: false,
                continueCallback: null,
                persistent: true);
        }
    }

    void ApplyPendingVisionPenalty()
    {
        if (!pendingVisionReduction)
            return;
        var maskController = VisionMaskController.Instance;
        if (!maskController)
            return;
        maskController.UpdateRadius(pendingVisionTargetRadius);
        pendingVisionReduction = false;
    }

    void ShowCivilianPenaltyNotification()
    {
        if (!cachedKillText)
        {
            cachedKillText = KillTextController.Instance;
            if (!cachedKillText)
                cachedKillText = FindObjectOfType<KillTextController>(true);
        }

        var killText = cachedKillText;
        if (!killText || killText.label == null)
            return;

        var labelRect = killText.label.rectTransform;
        if (labelRect)
            labelRect.SetAsLastSibling();
        killText.transform.SetAsLastSibling();
    }

    public bool HandleVisionAdjustmentRequest(bool increase, VisionMaskController controller, float delta)
    {
        if (!tutorialActive || currentStep != Step.ClickCivilian)
            return false;
        if (increase || controller == null)
            return false;

        pendingVisionReduction = true;
        pendingVisionTargetRadius = controller.currentRadius + delta;
        return true;
    }

    void ShowTimerBriefing()
    {
        currentStep = Step.TimerBriefing;
        visionHint?.Hide();
        HideAdditionalCivilianArrows();
        arrowRoot.gameObject.SetActive(PointArrowToTimer());

        string message =
            "<b>Time to Hunt</b>\n" +
            "You have <b>1 minute 30 seconds</b> to eliminate all 10 impostors.\n" +
            "Press Continue to begin the mission.";

        SetMessage(
            message,
            TimerAnchor,
            TimerOffset + new Vector2(0f, 20f),
            MessageSizeLarge);

        continueButton.interactable = true;
        continueButton.gameObject.SetActive(true);
        RefreshOverlayInteractivity();

        ConfigureSkipButton(false);
    }

    bool PointArrowToTimer()
    {
        if (!arrowRoot || !canvasRect || timer == null || timer.timerText == null)
            return false;

        var timerRect = timer.timerText.rectTransform;
        if (!timerRect)
            return false;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        float offset = 25f;
        Vector3 timerLocal = new Vector3(timerRect.rect.xMin - offset, timerRect.rect.center.y, 0f);
        Vector3 worldTarget = timerRect.TransformPoint(timerLocal);
        Vector2 screenTip = RectTransformUtility.WorldToScreenPoint(cam, worldTarget);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenTip, cam, out Vector2 localTip))
            return false;

        Vector2 basePos = localTip + new Vector2(-180f, -40f);
        arrowRoot.anchoredPosition = basePos;

        Vector2 delta = localTip - basePos;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        arrowRoot.localEulerAngles = new Vector3(0f, 0f, angle);

        float tipDistance = Mathf.Max(20f, delta.magnitude);
        float shaftLength = Mathf.Max(10f, tipDistance - arrowHeadWidth * 0.5f);
        if (arrowShaftRect != null)
        {
            arrowShaftRect.sizeDelta = new Vector2(shaftLength, arrowShaftHeight);
            arrowShaftRect.anchoredPosition = Vector2.zero;
        }

        if (arrowHeadRect != null)
            arrowHeadRect.anchoredPosition = new Vector2(shaftLength, 0f);

        return true;
    }

    void ShowVisionGrowthPrompt()
    {
        currentStep = Step.VisionReview;

        continueButton.gameObject.SetActive(false);
        continueButton.interactable = false;
        if (overlayGroup)
        {
            overlayGroup.interactable = true;
            overlayGroup.blocksRaycasts = true;
        }
        visionHint?.Hide();
        SetMessageVisible(false);

        var maskRect = VisionMaskController.Instance ? VisionMaskController.Instance.MaskRect : null;
        float radius = VisionMaskController.Instance ? VisionMaskController.Instance.currentRadius : 0.26f;

        if (visionHint != null && maskRect != null)
        {
            visionHint.Show(
                increase: true,
                maskRect: maskRect,
                radius: radius,
                customMessage: "Killing impostors expands your vision and increases your speed.",
                requireContinue: true,
                continueCallback: OnVisionGrowthContinue);
        }
        else
        {
            OnVisionGrowthContinue();
        }
    }

    void OnVisionGrowthContinue()
    {
        SetMessageVisible(true);
        visionHint?.Hide();
        RefreshOverlayInteractivity();
        BeginCivilianClickPractice();
    }

    void BeginCivilianClickPractice()
    {
        EnsurePrimaryCivilianPresent();
        EnsureAdditionalCivilianShowcase(forceRebuild: true);
        currentStep = Step.ClickCivilian;
        continueButton.gameObject.SetActive(false);
        continueButton.interactable = false;
        ConfigureSkipButton(true);

        targetCivilian = tutorialCivilianId != null ? tutorialCivilianId : FindCivilian();
        MaintainCivilianArrow();

        const string whiteCivilian = "<color=#FFFFFF>Civilian</color>";
        string message =
            $"<b>Click on any {whiteCivilian}</b>\n" +
            "Take the shot now.";

        SetMessage(
            message,
            ActionAnchor,
            ActionOffset,
            MessageSizeMedium);
    }

    void Update()
    {
        if (!tutorialActive)
            return;

        if (!mainCam)
            mainCam = Camera.main;

        switch (currentStep)
        {
            case Step.AwaitMovement:
                ListenForMovementInput();
                break;
            case Step.ClickImpostor:
                MaintainImpostorArrow();
                break;
            case Step.VisionReview:
                // waiting for player acknowledgement
                break;
            case Step.ClickCivilian:
                MaintainCivilianArrow();
                break;
            case Step.TimerBriefing:
                PointArrowToTimer();
                break;
        }
    }

    void OnContinueClicked()
    {
        if (!tutorialActive)
            return;

        if (currentStep == Step.ShowCivilian)
        {
            ShowTimerBriefing();
            return;
        }

        continueButton.interactable = false;
        FinishTutorial();
    }

    void OnSkipTutorialClicked()
    {
        ConfigureSkipButton(false);
        if (tutorialActive)
            FinishTutorial();
    }

    void FinishTutorial()
    {
        tutorialActive = false;
        tutorialCompleted = true;
        currentStep = Step.Finished;
        Time.timeScale = 1f;

        visionHint?.Hide();
        VisionMaskController.Instance?.ResetRadius();

        if (continueButton)
            continueButton.gameObject.SetActive(false);
        ConfigureSkipButton(false);

        if (ClickToSmite.HitFilter == FilterHits)
            ClickToSmite.HitFilter = null;
        ClickToSmite.OnHitResolved -= HandleShotResolved;
        ClickToSmite.SuppressGameState = false;

        ClearTutorialActors();
        ShowOverlay(false);
        SetMessageVisible(true);

        if (ImpostorTracker.Instance != null)
            ImpostorTracker.Instance.ResetCount();

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

    void StopTutorialInternal()
    {
        tutorialActive = false;
        currentStep = Step.Inactive;
        ShowOverlay(false);
        SetMessageVisible(true);
        ClickToSmite.SuppressGameState = false;
        ClearTutorialActors();
        visionHint?.Hide();
        VisionMaskController.Instance?.ResetRadius();
        ConfigureSkipButton(false);
        RefreshOverlayInteractivity();

        if (ImpostorTracker.Instance != null)
            ImpostorTracker.Instance.ResetCount();
    }

    void ClearTutorialActors()
    {
        pendingVisionReduction = false;
        pendingVisionTargetRadius = 0f;

        if (tutorialImpostor)
            Destroy(tutorialImpostor);
        if (tutorialCivilian)
            Destroy(tutorialCivilian);
        CleanupAdditionalCivilianShowcase();

        tutorialImpostor = null;
        tutorialCivilian = null;
        tutorialImpostorId = null;
        tutorialCivilianId = null;
        tutorialCivilianColorId = -1;
        tutorialCivilianSpawnPos = Vector3.zero;
        tutorialPlayer = null;
        targetImpostor = null;
        targetCivilian = null;

        if (arrowRoot)
            arrowRoot.gameObject.SetActive(false);

        RefreshOverlayInteractivity();
        visionHint?.Hide();
    }

    void EnsureAdditionalCivilianShowcase(bool forceRebuild = false)
    {
        if (forceRebuild)
            CleanupAdditionalCivilianShowcase();

        if (additionalCivilians.Count > 0)
            return;
        if (!tutorialCivilian)
            return;

        int baseColorId = tutorialCivilianId ? tutorialCivilianId.colorId :
            GetCivilianColorId(tutorialImpostorId ? tutorialImpostorId.colorId : -1);

        Vector3 anchor = tutorialPlayer ? tutorialPlayer.transform.position : tutorialCivilian.transform.position;
        for (int i = 0; i < AdditionalCivilianOffsets.Length && i < AdditionalCivilianColors.Length; i++)
        {
            var showcase = new CivilianShowcase();
            Vector3 spawnPos = SnapToGround(anchor + AdditionalCivilianOffsets[i]);
            showcase.npc = InstantiateTutorialNpc(false, spawnPos, baseColorId,
                out showcase.identity, out showcase.drifter, AdditionalCivilianColors[i]);
            showcase.arrowRoot = CreateArrowInstance($"ExtraCivilianArrow_{i}", out showcase.arrowShaft, out showcase.arrowHead);
            if (showcase.arrowRoot)
                showcase.arrowRoot.gameObject.SetActive(false);
            additionalCivilians.Add(showcase);
        }
    }

    void MaintainAdditionalCivilianArrow(CivilianShowcase showcase)
    {
        if (showcase == null || showcase.arrowRoot == null)
            return;

        Transform target = null;
        if (showcase.identity)
            target = showcase.identity.transform;
        else if (showcase.npc)
            target = showcase.npc.transform;

        if (!target)
        {
            showcase.arrowRoot.gameObject.SetActive(false);
            return;
        }

        showcase.arrowRoot.gameObject.SetActive(true);
        UpdateArrowToTarget(target, 0.95f, showcase.arrowRoot, showcase.arrowShaft, showcase.arrowHead);
    }

    void CleanupAdditionalCivilianShowcase()
    {
        foreach (var showcase in additionalCivilians)
        {
            if (showcase == null) continue;
            if (showcase.drifter)
                Destroy(showcase.drifter);
            if (showcase.npc)
                Destroy(showcase.npc);
            if (showcase.arrowRoot)
                Destroy(showcase.arrowRoot.gameObject);
        }
        additionalCivilians.Clear();
    }

    void HideAdditionalCivilianArrows()
    {
        foreach (var showcase in additionalCivilians)
        {
            if (showcase?.arrowRoot)
                showcase.arrowRoot.gameObject.SetActive(false);
        }
    }

    void RemoveCivilianFromScene(NPCIdentity id)
    {
        if (!id)
            return;
        if (tutorialCivilianId == id)
        {
            if (civilianDrifter)
            {
                civilianDrifter.enabled = false;
                civilianDrifter = null;
            }
            tutorialCivilian = null;
            tutorialCivilianId = null;
            return;
        }

        for (int i = additionalCivilians.Count - 1; i >= 0; i--)
        {
            var showcase = additionalCivilians[i];
            if (showcase == null || showcase.identity != id)
                continue;
            if (showcase.drifter)
            {
                showcase.drifter.enabled = false;
                showcase.drifter = null;
            }
            if (showcase.arrowRoot)
                Destroy(showcase.arrowRoot.gameObject);
            additionalCivilians.RemoveAt(i);
            break;
        }
    }

    void EnsurePrimaryCivilianPresent()
    {
        if (tutorialCivilian)
            return;
        if (tutorialCivilianColorId < 0)
            return;
        tutorialCivilian = InstantiateTutorialNpc(false, tutorialCivilianSpawnPos, tutorialCivilianColorId, out tutorialCivilianId, out civilianDrifter);
    }

    RectTransform CreateArrowInstance(string name, out RectTransform shaftRect, out RectTransform headRect)
    {
        shaftRect = null;
        headRect = null;
        if (!overlayRoot)
            return null;

        var arrowGO = new GameObject(name, typeof(RectTransform));
        var root = arrowGO.GetComponent<RectTransform>();
        root.SetParent(overlayRoot, false);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0f, 0.5f);
        root.sizeDelta = new Vector2(200f, 40f);

        var shaftGO = new GameObject("Shaft", typeof(RectTransform), typeof(Image));
        shaftRect = shaftGO.GetComponent<RectTransform>();
        shaftRect.SetParent(root, false);
        shaftRect.anchorMin = new Vector2(0f, 0.5f);
        shaftRect.anchorMax = new Vector2(0f, 0.5f);
        shaftRect.pivot = new Vector2(0f, 0.5f);
        shaftRect.anchoredPosition = Vector2.zero;
        shaftRect.sizeDelta = new Vector2(120f, arrowShaftHeight);
        var shaftImage = shaftGO.GetComponent<Image>();
        shaftImage.sprite = GetSolidSprite();
        shaftImage.color = Color.white;
        shaftImage.raycastTarget = false;

        var headGO = new GameObject("Head", typeof(RectTransform), typeof(Image));
        headRect = headGO.GetComponent<RectTransform>();
        headRect.SetParent(root, false);
        headRect.anchorMin = new Vector2(0f, 0.5f);
        headRect.anchorMax = new Vector2(0f, 0.5f);
        headRect.pivot = new Vector2(0.5f, 0.5f);
        headRect.anchoredPosition = new Vector2(arrowHeadWidth * 0.5f, 0f);
        headRect.sizeDelta = new Vector2(arrowHeadWidth, arrowHeadWidth);
        headRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        var headImage = headGO.GetComponent<Image>();
        headImage.sprite = GetSolidSprite();
        headImage.color = Color.white;
        headImage.raycastTarget = false;

        return root;
    }

    string[] GetCivilianColorHexes()
    {
        if (civilianColorHexSequence != null)
            return civilianColorHexSequence;

        var list = new List<string>(1 + AdditionalCivilianColors.Length)
        {
            ColorUtility.ToHtmlStringRGB(TutorialCivilianColor)
        };
        for (int i = 0; i < AdditionalCivilianColors.Length; i++)
            list.Add(ColorUtility.ToHtmlStringRGB(AdditionalCivilianColors[i]));
        civilianColorHexSequence = list.ToArray();
        return civilianColorHexSequence;
    }

    bool FilterHits(NPCIdentity id)
    {
        if (!tutorialActive)
            return true;

        return currentStep switch
        {
            Step.AwaitMovement => false,
            Step.ClickImpostor => id != null && targetImpostor != null && id == targetImpostor,
            Step.VisionReview => false,
            Step.ClickCivilian => id != null && !id.isImpostor,
            Step.ShowCivilian => false,
            _ => true,
        };
    }

    Vector3 SnapToGround(Vector3 position)
    {
        float probeHeight = spawner != null ? spawner.groundProbeHeight : 4f;
        float probeDistance = spawner != null ? spawner.groundProbeDistance : 6f;
        LayerMask mask = spawner != null ? spawner.groundMask : Physics.DefaultRaycastLayers;

        Vector3 origin = position + Vector3.up * probeHeight;
        float maxDistance = probeHeight + probeDistance;

        if (Physics.Raycast(origin, Vector3.down, out var hit, maxDistance, mask, QueryTriggerInteraction.Ignore))
            position.y = hit.point.y;

        return position;
    }

    void AlignToGround(GameObject npc, float groundY)
    {
        if (!npc)
            return;

        var colliders = npc.GetComponentsInChildren<Collider>();
        Bounds bounds = default;
        bool hasBounds = false;
        foreach (var col in colliders)
        {
            if (col == null) continue;
            if (!hasBounds)
            {
                bounds = col.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        if (!hasBounds)
        {
            var renderer = npc.GetComponentInChildren<Renderer>();
            if (renderer == null)
                return;
            bounds = renderer.bounds;
        }

        float bottom = bounds.min.y;
        float offset = groundY - bottom;
        npc.transform.position += new Vector3(0f, offset, 0f);
    }

    void EnsureUI()
    {
        if (overlayRoot)
            return;

        overlayRoot = new GameObject("Level1TutorialOverlay", typeof(RectTransform)).GetComponent<RectTransform>();
        overlayRoot.SetParent(transform, false);
        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;

        overlayGroup = overlayRoot.gameObject.AddComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;
        overlayGroup.blocksRaycasts = false;
        overlayGroup.interactable = false;

        messagePanel = new GameObject("MessagePanel", typeof(RectTransform)).GetComponent<RectTransform>();
        messagePanel.SetParent(overlayRoot, false);
        messagePanel.anchorMin = MovementAnchor;
        messagePanel.anchorMax = MovementAnchor;
        messagePanel.pivot = new Vector2(0.5f, 0.5f);
        messagePanel.sizeDelta = MessageSizeMedium;
        messagePanel.anchoredPosition = MovementOffset;

        var panelImage = messagePanel.gameObject.AddComponent<Image>();
        panelImage.sprite = GetSolidSprite();
        panelImage.color = new Color(0.06f, 0.07f, 0.12f, 0.9f);
        panelImage.raycastTarget = false;

        var panelShadow = messagePanel.gameObject.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        panelShadow.effectDistance = new Vector2(0f, -3f);

        var textGO = new GameObject("Message", typeof(RectTransform));
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.SetParent(messagePanel, false);
        textRect.anchorMin = new Vector2(0.07f, 0.08f);
        textRect.anchorMax = new Vector2(0.93f, 0.92f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        messageText = textGO.AddComponent<TextMeshProUGUI>();
        messageText.font = GetFontAsset();
        messageText.fontSize = 34f;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = Color.white;
        messageText.raycastTarget = false;
        messageText.richText = true;
        messageText.enableWordWrapping = true;

        var messageOutline = messageText.gameObject.AddComponent<Outline>();
        messageOutline.effectColor = new Color(0f, 0f, 0f, 0.4f);
        messageOutline.effectDistance = new Vector2(1.5f, -1.5f);

        var buttonGO = new GameObject("ContinueButton", typeof(RectTransform));
        var btnRect = buttonGO.GetComponent<RectTransform>();
        btnRect.SetParent(overlayRoot, false);
        btnRect.anchorMin = new Vector2(0.5f, 0.22f);
        btnRect.anchorMax = new Vector2(0.5f, 0.22f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(240f, 66f);
        btnRect.anchoredPosition = new Vector2(0f, 0f);

        var btnImage = buttonGO.AddComponent<Image>();
        btnImage.sprite = GetSolidSprite();
        btnImage.type = Image.Type.Simple;
        btnImage.color = new Color(0.9f, 0.25f, 0.28f, 0.95f);
        btnImage.raycastTarget = true;

        var btnShadow = buttonGO.AddComponent<Shadow>();
        btnShadow.effectDistance = new Vector2(0f, -3f);
        btnShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);

        continueButton = buttonGO.AddComponent<Button>();
        continueButton.targetGraphic = btnImage;
        continueButton.onClick.AddListener(OnContinueClicked);
        continueButton.gameObject.SetActive(false);

        var btnLabelGO = new GameObject("Label", typeof(RectTransform));
        var btnLabelRect = btnLabelGO.GetComponent<RectTransform>();
        btnLabelRect.SetParent(buttonGO.transform, false);
        btnLabelRect.anchorMin = Vector2.zero;
        btnLabelRect.anchorMax = Vector2.one;
        btnLabelRect.offsetMin = Vector2.zero;
        btnLabelRect.offsetMax = Vector2.zero;

        var btnLabel = btnLabelGO.AddComponent<TextMeshProUGUI>();
        btnLabel.font = GetFontAsset();
        btnLabel.text = "Continue";
        btnLabel.fontSize = 30f;
        btnLabel.alignment = TextAlignmentOptions.Center;
        btnLabel.color = Color.white;
        btnLabel.raycastTarget = false;

        var skipGO = new GameObject("SkipTutorialButton", typeof(RectTransform));
        var skipRect = skipGO.GetComponent<RectTransform>();
        skipRect.SetParent(overlayRoot, false);
        skipRect.anchorMin = new Vector2(0.5f, 0.15f);
        skipRect.anchorMax = new Vector2(0.5f, 0.15f);
        skipRect.pivot = new Vector2(0.5f, 0.5f);
        skipRect.sizeDelta = new Vector2(220f, 58f);
        skipRect.anchoredPosition = new Vector2(0f, 0f);

        var skipImage = skipGO.AddComponent<Image>();
        skipImage.sprite = GetSolidSprite();
        skipImage.color = new Color(0.25f, 0.28f, 0.33f, 0.95f);

        var skipShadow = skipGO.AddComponent<Shadow>();
        skipShadow.effectDistance = new Vector2(0f, -3f);
        skipShadow.effectColor = new Color(0f, 0f, 0f, 0.45f);

        skipTutorialButton = skipGO.AddComponent<Button>();
        skipTutorialButton.targetGraphic = skipImage;
        skipTutorialButton.onClick.AddListener(OnSkipTutorialClicked);
        skipTutorialButton.gameObject.SetActive(false);

        var skipLabelGO = new GameObject("Label", typeof(RectTransform));
        var skipLabelRect = skipLabelGO.GetComponent<RectTransform>();
        skipLabelRect.SetParent(skipGO.transform, false);
        skipLabelRect.anchorMin = Vector2.zero;
        skipLabelRect.anchorMax = Vector2.one;
        skipLabelRect.offsetMin = Vector2.zero;
        skipLabelRect.offsetMax = Vector2.zero;

        var skipLabel = skipLabelGO.AddComponent<TextMeshProUGUI>();
        skipLabel.font = GetFontAsset();
        skipLabel.text = "Skip Tutorial";
        skipLabel.fontSize = 26f;
        skipLabel.alignment = TextAlignmentOptions.Center;
        skipLabel.color = Color.white;
        skipLabel.raycastTarget = false;

        arrowRoot = CreateArrowInstance("Arrow", out arrowShaftRect, out arrowHeadRect);
        if (arrowRoot)
            arrowRoot.gameObject.SetActive(false);
    }

    void ShowOverlay(bool show)
    {
        if (!overlayRoot)
            return;

        overlayRoot.gameObject.SetActive(show);
        if (overlayGroup)
        {
            overlayGroup.alpha = show ? 1f : 0f;
            RefreshOverlayInteractivity();
        }
    }

    void RefreshOverlayInteractivity()
    {
        if (!overlayGroup)
            return;

        bool allowInput = (continueButton != null && continueButton.gameObject.activeInHierarchy) ||
                          (skipTutorialButton != null && skipTutorialButton.gameObject.activeInHierarchy);
        overlayGroup.blocksRaycasts = allowInput;
        overlayGroup.interactable = allowInput;
    }

    void ShowMovementMessage()
    {
        string message =
            "<b>Move Around</b>\n" +
            "Use <b>W A S D</b> or the <b>Arrow Keys</b> to walk.\n" +
            "Take a few steps to continue.";

        SetMessage(
            message,
            MovementAnchor,
            MovementOffset,
            MessageSizeMedium);
    }

    void ShowImpostorMessage()
    {
        string message =
            $"<b>Find the <color=#{impostorColorHex}>Red Impostor</color></b>\n" +
            $"Follow the arrow and <b>left-click</b> the <color=#{impostorColorHex}>red impostor</color>.";

        SetMessage(
            message,
            ActionAnchor,
            ActionOffset,
            MessageSizeMedium);
    }

    void ShowCivilianMessage()
    {
        string coloredWord = "<color=#FFFFFF>Civilians</color>";
        string message =
            $"<b>Leave {coloredWord} Alone</b>\n" +
            $"If a non-player character is <color=#{impostorColorHex}>not red</color>, do <b>not</b> click them.";

        SetMessage(
            message,
            CivilianAnchor,
            CivilianOffset,
            MessageSizeLarge);
    }

    string GetColoredCivilianWord()
    {
        const string word = "Civilians";
        var colors = GetCivilianColorHexes();
        var builder = new StringBuilder(word.Length * 32);
        for (int i = 0; i < word.Length; i++)
        {
            string hex = colors[i % colors.Length];
            builder.Append("<color=#").Append(hex).Append('>').Append(word[i]).Append("</color>");
        }
        return builder.ToString();
    }

    void SetMessage(string text, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        if (!messagePanel || !messageText)
            return;

        messagePanel.gameObject.SetActive(true);
        messagePanel.anchorMin = anchor;
        messagePanel.anchorMax = anchor;
        messagePanel.anchoredPosition = offset;
        messagePanel.sizeDelta = size;

        messageText.text = text;
    }

    void SetMessageVisible(bool visible)
    {
        if (messagePanel)
            messagePanel.gameObject.SetActive(visible);
        RefreshOverlayInteractivity();
    }

    void ConfigureSkipButton(bool visible)
    {
        if (!skipTutorialButton)
            return;

        skipTutorialButton.gameObject.SetActive(visible);
        skipTutorialButton.interactable = visible;
        RefreshOverlayInteractivity();
    }

    static bool IsLevelOneScene()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.IsValid() && scene.name == "LvL1";
    }

    static Sprite GetSolidSprite()
    {
        if (solidSprite != null)
            return solidSprite;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "TutorialSolidTex",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Repeat,
            hideFlags = HideFlags.HideAndDontSave
        };
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);

        solidSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        solidSprite.name = "TutorialSolidSprite";
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

        if (cachedFont == null)
        {
            var legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (legacy != null)
            {
                cachedFont = TMP_FontAsset.CreateFontAsset(legacy);
                if (cachedFont != null)
                    cachedFont.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        return cachedFont;
    }
}
