using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Guides the player through a short step-by-step tutorial before Level 1 begins.
/// Steps:
/// 1. Prompt for movement input.
/// 2. Highlight an impostor and require the player to eliminate them.
/// 3. Highlight a civilian and warn the player not to shoot them, then start the timer.
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

    Step currentStep = Step.Inactive;

    InstructionsManager instructions;
    TimerController timer;
    SpawnManager spawner;
    ClickToSmite clicker;

    RectTransform canvasRect;
    Canvas canvas;
    Camera mainCam;

    GameObject overlayRoot;
    CanvasGroup overlayGroup;
    Text messageText;
    Button continueButton;
    RectTransform arrowRoot;
    RectTransform arrowShaftRect;

    NPCIdentity targetImpostor;
    NPCIdentity targetCivilian;

    bool tutorialActive;
    bool movementCaptured;

    readonly Vector2 arrowOffset = new(-220f, 90f);
    const float arrowTargetHeight = 1.6f;

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

    /// <summary>
    /// Invoked by InstructionsManager. Returns true if the tutorial took over the start flow.
    /// </summary>
    public bool TryBeginTutorial(InstructionsManager mgr)
    {
        if (tutorialActive || !IsLevelOneScene())
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
        instructions.TriggerSpawnIfNeeded();

        if (clicker != null)
            clicker.enabled = true;

        ClickToSmite.OnHitResolved += HandleShotResolved;
        ClickToSmite.HitFilter = FilterHits;

        tutorialActive = true;
        movementCaptured = false;
        currentStep = Step.AwaitMovement;

        UpdateMessage("Use WASD or Arrow keys to move.");
        continueButton.gameObject.SetActive(false);
        arrowRoot.gameObject.SetActive(false);

        return true;
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
        targetImpostor = FindImpostor();

        UpdateMessage("Great! Track down the impostor in red and click to eliminate them.");
        arrowRoot.gameObject.SetActive(targetImpostor != null);

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
        UpdateArrowToTarget(targetImpostor.transform);
    }

    void HandleShotResolved(NPCIdentity id, bool correct)
    {
        if (!tutorialActive)
            return;

        if (currentStep == Step.ClickImpostor && correct && id != null && targetImpostor != null && id == targetImpostor)
        {
            targetImpostor = null;
            AdvanceToCivilianStep();
        }
    }

    void AdvanceToCivilianStep()
    {
        currentStep = Step.ShowCivilian;
        targetCivilian = FindCivilian();

        UpdateMessage("This one is a civilian wearing red. Do NOT eliminate civilians during the mission.\nPress Continue to begin.");
        continueButton.gameObject.SetActive(true);
        continueButton.interactable = true;

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
        UpdateArrowToTarget(targetCivilian.transform);
    }

    void UpdateArrowToTarget(Transform target)
    {
        if (!target || !canvasRect)
            return;

        if (!mainCam)
            mainCam = Camera.main;

        Vector3 worldPoint = target.position + Vector3.up * arrowTargetHeight;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCam, worldPoint);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint))
        {
            Vector2 anchor = localPoint + arrowOffset;
            arrowRoot.anchoredPosition = anchor;

            Vector2 delta = localPoint - anchor;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            arrowRoot.localEulerAngles = new Vector3(0f, 0f, angle);

            float shaftLength = Mathf.Max(40f, delta.magnitude - 30f);
            if (arrowShaftRect != null)
                arrowShaftRect.sizeDelta = new Vector2(shaftLength, arrowShaftRect.sizeDelta.y);
        }
    }

    NPCIdentity FindImpostor()
    {
        var candidates = FindObjectsOfType<NPCIdentity>();
        foreach (var id in candidates)
        {
            if (!id || !id.isActiveAndEnabled)
                continue;
            if (!id.isImpostor)
                continue;
            return id;
        }
        return null;
    }

    NPCIdentity FindCivilian()
    {
        var candidates = FindObjectsOfType<NPCIdentity>();
        foreach (var id in candidates)
        {
            if (!id || !id.isActiveAndEnabled)
                continue;
            if (id.isImpostor)
                continue;
            return id;
        }
        return null;
    }

    void EnsureUI()
    {
        if (overlayRoot)
            return;

        overlayRoot = new GameObject("Level1TutorialOverlay", typeof(RectTransform));
        overlayRoot.transform.SetParent(transform, false);

        var rootRect = overlayRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        overlayGroup = overlayRoot.AddComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;
        overlayGroup.blocksRaycasts = true;
        overlayGroup.interactable = true;

        var dimGO = new GameObject("Dimmer", typeof(RectTransform));
        var dimRect = dimGO.GetComponent<RectTransform>();
        dimRect.SetParent(overlayRoot.transform, false);
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        var dimImage = dimGO.AddComponent<Image>();
        dimImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        dimImage.color = new Color(0f, 0f, 0f, 0.55f);
        dimImage.raycastTarget = false;

        var textGO = new GameObject("Message", typeof(RectTransform));
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.SetParent(overlayRoot.transform, false);
        textRect.anchorMin = new Vector2(0.5f, 0.1f);
        textRect.anchorMax = new Vector2(0.5f, 0.1f);
        textRect.anchoredPosition = new Vector2(0f, 80f);
        textRect.sizeDelta = new Vector2(900f, 140f);

        messageText = textGO.AddComponent<Text>();
        messageText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.fontSize = 28;
        messageText.color = Color.white;
        messageText.raycastTarget = false;

        var buttonGO = new GameObject("ContinueButton", typeof(RectTransform));
        var btnRect = buttonGO.GetComponent<RectTransform>();
        btnRect.SetParent(overlayRoot.transform, false);
        btnRect.anchorMin = new Vector2(0.5f, 0.1f);
        btnRect.anchorMax = new Vector2(0.5f, 0.1f);
        btnRect.anchoredPosition = new Vector2(0f, 15f);
        btnRect.sizeDelta = new Vector2(260f, 70f);

        var btnImage = buttonGO.AddComponent<Image>();
        btnImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        btnImage.type = Image.Type.Sliced;
        btnImage.color = new Color(0.82f, 0.15f, 0.2f, 0.95f);

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

        var btnLabel = btnLabelGO.AddComponent<Text>();
        btnLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        btnLabel.text = "Continue";
        btnLabel.alignment = TextAnchor.MiddleCenter;
        btnLabel.fontSize = 26;
        btnLabel.color = Color.white;
        btnLabel.raycastTarget = false;

        var arrowGO = new GameObject("Arrow", typeof(RectTransform));
        arrowRoot = arrowGO.GetComponent<RectTransform>();
        arrowRoot.SetParent(overlayRoot.transform, false);
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
        arrowShaftRect.sizeDelta = new Vector2(120f, 12f);

        var shaftImage = shaftGO.AddComponent<Image>();
        shaftImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        shaftImage.color = Color.white;
        shaftImage.raycastTarget = false;

        var headGO = new GameObject("Head", typeof(RectTransform));
        var headRect = headGO.GetComponent<RectTransform>();
        headRect.SetParent(arrowRoot, false);
        headRect.anchorMin = new Vector2(1f, 0.5f);
        headRect.anchorMax = new Vector2(1f, 0.5f);
        headRect.pivot = new Vector2(0.5f, 0.5f);
        headRect.anchoredPosition = Vector2.zero;
        headRect.sizeDelta = new Vector2(26f, 26f);
        headRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

        var headImage = headGO.AddComponent<Image>();
        headImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        headImage.color = Color.white;
        headImage.raycastTarget = false;

        arrowRoot.gameObject.SetActive(false);
    }

    void ShowOverlay(bool show)
    {
        if (!overlayRoot) return;

        overlayRoot.SetActive(show);
        if (overlayGroup)
        {
            overlayGroup.alpha = show ? 1f : 0f;
            overlayGroup.blocksRaycasts = show;
            overlayGroup.interactable = show;
        }
    }

    void UpdateMessage(string text)
    {
        if (messageText)
            messageText.text = text;
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
        currentStep = Step.Finished;

        if (ClickToSmite.HitFilter == FilterHits)
            ClickToSmite.HitFilter = null;
        ClickToSmite.OnHitResolved -= HandleShotResolved;

        ShowOverlay(false);

        if (timer == null)
            timer = FindObjectOfType<TimerController>(true);

        if (instructions != null)
            instructions.EnableGameplayUI(true);

        if (timer != null)
            timer.StartTimer(60f);
    }

    void StopTutorialInternal()
    {
        tutorialActive = false;
        currentStep = Step.Inactive;
        ShowOverlay(false);
    }

    bool FilterHits(NPCIdentity id)
    {
        if (!tutorialActive)
            return true;

        switch (currentStep)
        {
            case Step.AwaitMovement:
                return false;
            case Step.ClickImpostor:
                return id != null && targetImpostor != null && id == targetImpostor;
            case Step.ShowCivilian:
                return false;
            default:
                return true;
        }
    }

    static bool IsLevelOneScene()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.IsValid() && scene.name == "LvL1";
    }
}
