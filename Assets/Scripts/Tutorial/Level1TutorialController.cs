using System.Globalization;
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
    enum Step
    {
        Inactive,
        AwaitMovement,
        ClickImpostor,
        ShowCivilian,
        Finished
    }

    static readonly Vector2 MessageSizeLarge = new(620f, 150f);
    static readonly Vector2 MessageSizeMedium = new(540f, 130f);
    static readonly Vector2 MessageSizeSmall = new(480f, 120f);

    static readonly Vector2 MovementAnchor = new(0.5f, 0.82f);
    static readonly Vector2 MovementOffset = new(0f, -25f);

    static readonly Vector2 ActionAnchor = new(0.5f, 0.78f);
    static readonly Vector2 ActionOffset = new(0f, -15f);

static readonly Vector2 CivilianAnchor = new(0.5f, 0.78f);
static readonly Vector2 CivilianOffset = new(0f, -15f);

readonly Vector2 arrowTailOffset = new(-140f, 55f);
const float arrowTargetHeight = 1.6f;

const string AccentHex = "#F8B938";
const string CivilianColorKeyword = "orange";
static readonly Color TutorialCivilianColor = new Color32(244, 128, 40, 255);

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
string civilianColorDisplayName = "civilian";
string civilianColorHex = "#F48028";
string impostorColorHex = "#FF4F4F";

    NPCIdentity targetImpostor;
    NPCIdentity targetCivilian;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvasRect = GetComponent<RectTransform>();
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
        RefreshOverlayInteractivity();

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
        civilianColorDisplayName = "orange";

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

    GameObject InstantiateTutorialNpc(bool isImpostor, Vector3 position, int colorId, out NPCIdentity identity, out TutorialDrifter drifter)
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
            foreach (var r in renderers)
            {
                if (!r) continue;
                var mat = new Material(r.material);
                mat.color = TutorialCivilianColor;
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
            Color c = spawner.palette.Get(colorId);
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
        targetCivilian = tutorialCivilianId != null ? tutorialCivilianId : FindCivilian();

        ShowCivilianMessage();
        continueButton.gameObject.SetActive(true);
        continueButton.interactable = true;
        RefreshOverlayInteractivity();

        MaintainCivilianArrow();
    }

    void MaintainCivilianArrow()
    {
        if (targetCivilian == null)
        {
            targetCivilian = FindCivilian();
            if (targetCivilian == null)
            {
                arrowRoot.gameObject.SetActive(false);
                return;
            }
        }

        arrowRoot.gameObject.SetActive(true);
        UpdateArrowToTarget(targetCivilian.transform, 0.95f);
    }

    void UpdateArrowToTarget(Transform target, float yOffsetMultiplier = 1f)
    {
        if (!target || !canvasRect)
            return;

        if (!mainCam)
            mainCam = Camera.main;

        Vector3 boundsCenter = target.position;
        var renderer = target.GetComponentInChildren<Renderer>();
        if (renderer)
            boundsCenter = renderer.bounds.center;

        float halfHeight = 0.8f;
        if (renderer)
            halfHeight = renderer.bounds.extents.y;

        Vector3 worldTip = boundsCenter + Vector3.up * halfHeight * yOffsetMultiplier;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCam, worldTip);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 localPoint))
        {
            Vector2 basePos = localPoint + arrowTailOffset;
            arrowRoot.anchoredPosition = basePos;

            Vector2 delta = localPoint - basePos;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            arrowRoot.localEulerAngles = new Vector3(0f, 0f, angle);

            float tipDistance = delta.magnitude;
            float shaftLength = Mathf.Max(10f, tipDistance - arrowHeadWidth * 0.5f);
            if (arrowShaftRect != null)
            {
                arrowShaftRect.sizeDelta = new Vector2(shaftLength, arrowShaftHeight);
                arrowShaftRect.anchoredPosition = Vector2.zero;
            }

            if (arrowHeadRect != null)
                arrowHeadRect.anchoredPosition = new Vector2(shaftLength, 0f);
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
            AdvanceToCivilianStep();
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
            case Step.ShowCivilian:
                MaintainCivilianArrow();
                break;
        }
    }

    void OnContinueClicked()
    {
        if (!tutorialActive)
            return;

        continueButton.interactable = false;
        FinishTutorial();
    }

    void FinishTutorial()
    {
        tutorialActive = false;
        tutorialCompleted = true;
        currentStep = Step.Finished;
        Time.timeScale = 1f;

        if (continueButton)
            continueButton.gameObject.SetActive(false);
        RefreshOverlayInteractivity();

        if (ClickToSmite.HitFilter == FilterHits)
            ClickToSmite.HitFilter = null;
        ClickToSmite.OnHitResolved -= HandleShotResolved;
        ClickToSmite.SuppressGameState = false;

        ClearTutorialActors();
        ShowOverlay(false);

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
            timer.StartTimer(60f);
    }

    void StopTutorialInternal()
    {
        tutorialActive = false;
        currentStep = Step.Inactive;
        ShowOverlay(false);
        ClickToSmite.SuppressGameState = false;
        ClearTutorialActors();

        if (ImpostorTracker.Instance != null)
            ImpostorTracker.Instance.ResetCount();
    }

    void ClearTutorialActors()
    {
        if (tutorialImpostor)
            Destroy(tutorialImpostor);
        if (tutorialCivilian)
            Destroy(tutorialCivilian);

        tutorialImpostor = null;
        tutorialCivilian = null;
        tutorialImpostorId = null;
        tutorialCivilianId = null;
        tutorialPlayer = null;
        targetImpostor = null;
        targetCivilian = null;

        if (arrowRoot)
            arrowRoot.gameObject.SetActive(false);

        RefreshOverlayInteractivity();
    }

    bool FilterHits(NPCIdentity id)
    {
        if (!tutorialActive)
            return true;

        return currentStep switch
        {
            Step.AwaitMovement => false,
            Step.ClickImpostor => id != null && targetImpostor != null && id == targetImpostor,
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

        var renderer = npc.GetComponentInChildren<Renderer>();
        if (renderer == null)
            return;

        float bottom = renderer.bounds.min.y;
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
        textRect.anchorMin = new Vector2(0.1f, 0.12f);
        textRect.anchorMax = new Vector2(0.9f, 0.88f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        messageText = textGO.AddComponent<TextMeshProUGUI>();
        messageText.font = GetFontAsset();
        messageText.fontSize = 30f;
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

        var arrowGO = new GameObject("Arrow", typeof(RectTransform));
        arrowRoot = arrowGO.GetComponent<RectTransform>();
        arrowRoot.SetParent(overlayRoot, false);
        arrowRoot.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRoot.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRoot.pivot = new Vector2(0f, 0.5f);
        arrowRoot.sizeDelta = new Vector2(200f, 40f);

        var shaftGO = new GameObject("Shaft", typeof(RectTransform));
        arrowShaftRect = shaftGO.GetComponent<RectTransform>();
        arrowShaftRect.SetParent(arrowRoot, false);
        arrowShaftRect.anchorMin = new Vector2(0f, 0.5f);
        arrowShaftRect.anchorMax = new Vector2(0f, 0.5f);
        arrowShaftRect.pivot = new Vector2(0f, 0.5f);
        arrowShaftRect.anchoredPosition = Vector2.zero;
        arrowShaftRect.sizeDelta = new Vector2(120f, arrowShaftHeight);

        var shaftImage = shaftGO.AddComponent<Image>();
        shaftImage.sprite = GetSolidSprite();
        shaftImage.color = Color.white;
        shaftImage.raycastTarget = false;

        var headGO = new GameObject("Head", typeof(RectTransform));
        arrowHeadRect = headGO.GetComponent<RectTransform>();
arrowHeadRect.SetParent(arrowRoot, false);
arrowHeadRect.anchorMin = new Vector2(0f, 0.5f);
arrowHeadRect.anchorMax = new Vector2(0f, 0.5f);
arrowHeadRect.pivot = new Vector2(0.5f, 0.5f);
        arrowHeadRect.anchoredPosition = new Vector2(arrowHeadWidth * 0.5f, 0f);
arrowHeadRect.sizeDelta = new Vector2(arrowHeadWidth, arrowHeadWidth);
        arrowHeadRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

        var headImage = headGO.AddComponent<Image>();
        headImage.sprite = GetSolidSprite();
        headImage.color = Color.white;
        headImage.raycastTarget = false;

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

        bool allowInput = continueButton != null && continueButton.gameObject.activeInHierarchy;
        overlayGroup.blocksRaycasts = allowInput;
        overlayGroup.interactable = allowInput;
    }

    void ShowMovementMessage()
    {
        string title = "<size=32><b>Move Around</b></size>";
        string body = "<size=24>Use <b>W A S D</b> or the <b>Arrow Keys</b> to walk a little.</size>";
        string hint = "<size=22>Take a few steps to continue.</size>";

        SetMessage(
            title + "\n" + body + "\n" + hint,
            MovementAnchor,
            MovementOffset,
            MessageSizeMedium);
    }

    void ShowImpostorMessage()
    {
        string title = $"<size=32><b>Find the <color=#{impostorColorHex}>Red Impostor</color></b></size>";
        string body = $"<size=24>Follow the arrow and <b>left-click</b> the <color=#{impostorColorHex}>red impostor</color>.</size>";
        string hint = null;

        var message = title + "\n" + body;
        if (!string.IsNullOrEmpty(hint))
            message += "\n" + hint;

        SetMessage(
            message,
            ActionAnchor,
            ActionOffset,
            MessageSizeMedium);
    }

    void ShowCivilianMessage()
    {
        string title = $"<size=32><b>Leave <color=#{civilianColorHex}>Civilians</color> Alone</b></size>";
        string body = $"<size=24>This <color=#{civilianColorHex}>orange civilian</color> is harmless. Do <b>not</b> click them.</size>";
        string hint = "<size=22>Press Continue to start the real mission.</size>";

        SetMessage(
            title + "\n" + body + "\n" + hint,
            CivilianAnchor,
            CivilianOffset,
            MessageSizeLarge);
    }

    void SetMessage(string text, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        if (!messagePanel || !messageText)
            return;

        messagePanel.anchorMin = anchor;
        messagePanel.anchorMax = anchor;
        messagePanel.anchoredPosition = offset;
        messagePanel.sizeDelta = size;

        messageText.text = text;
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
