using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TimerController : MonoBehaviour
{
    [Header("Config")]
    public float startTime = 90f;

    [Header("Scene refs")]
    public TextMeshProUGUI timerText;
    public GameObject gameOverPanel;
    [Tooltip("If not assigned, we search any Canvas (even inactive) for this name.")]
    public string gameOverPanelName = "GameOver Panel";
    [Tooltip("Name of the Retry button inside the GameOver panel.")]
    public string retryButtonName = "Retry";
    [Tooltip("Child TMP name inside the GameOver Panel where the score is written.")]
    public string scoreTextName = "ScoreText";

    [Header("Top-left UI")]
    public GameObject topLeftButtonsRoot;   


    float currentTime;
    bool isGameOver = false;
    bool isRunning = false;
    bool _reloading = false;
    bool timeExpired = false;
    int killedAtTimeout = -1;
    int remainingAtTimeout = -1;
    public bool IsGameOver => isGameOver;
    public bool IsOutOfTime => currentTime <= 0.0001f;
    public bool IsDisplayingZeroTime => currentTime < 1f; // matches when the HUD shows 00:00
    public bool HasTimeExpired => timeExpired;

    [Header("Visual FX")]
    public float warningThreshold = 15f;                 // last N seconds
    public Color normalColor = Color.white;
    public Color warningColor = new Color(0.55f, 0f, 0f, 1f);   // dark red
    [Range(0f, 1f)] public float warningBlinkMinAlpha = 0.35f;
    [Range(0f, 1f)] public float warningBlinkMaxAlpha = 1f;
    public float warningBlinkSpeed = 6f;

    [Header("Warning Audio")]
    [SerializeField] AudioClip clockTickingClip;
    [Tooltip("Optional resource path override if the clip reference is missing.")]
    public string clockTickingResourcePath = "Audio/clocktickingsound";
    [Range(0f, 1.5f)] public float clockTickingVolume = 1f;

    [Header("Game Over Audio")]
    [SerializeField] AudioClip gameOverClip;
    [Tooltip("Optional resource path override if the clip reference is missing.")]
    public string gameOverClipResourcePath = "Audio/GameOverMusic";
    [Range(0f, 1.5f)] public float gameOverVolume = 1f;

    [Header("Timer Background")]
    public Image timerBackground;
    public Color backgroundColor = new Color(0.2f, 0.45f, 0.9f, 0.95f);
    public Color warningBackgroundColor = new Color(1f, 0.45f, 0.2f, 0.95f);
    [Tooltip("X = horizontal padding, Y = vertical padding (in pixels).")]
    public Vector2 backgroundPadding = new Vector2(20f, 8f);
    [Tooltip("If true, background stays square; otherwise it can be rectangular.")]
    public bool maintainSquareBackground = false;

    [Header("Timer Placement")]
    [Tooltip("Offset applied to the timer badge (parent rect if available).")]
    public Vector2 timerPositionOffset = new Vector2(0f, -45f);
    [Tooltip("Extra padding between the timer badge and the top screen edge.")]
    public float topEdgeMargin = 1f;
    [Tooltip("Guarantees a minimum gap even for scenes with older serialized values.")]
    public float minimumTopEdgeMargin = 12f;

    [Tooltip("Normal (smaller) font size.")]
    public float baseFontSize = 60f;

    [Tooltip("Font size during warning (slightly larger).")]
    public float warningFontSize = 72f;

    [Tooltip("Beat/pulse scale range during warning.")]
    public float pulseScaleMin = 0.95f, pulseScaleMax = 1.15f;

    [Tooltip("Beating speed (Hz-ish).")]
    public float pulseSpeed = 5f;

RectTransform _rt;
RectTransform _backgroundRect;
RectTransform _timerContainer;
Vector2 _timerContainerBaseAnchoredPos;
bool _hasTimerBasePos;
float _lastBackgroundSide;
    AudioSource _warningAudioSource;
    bool _warningActive;
    AudioSource _gameOverAudioSource;

    const int TOP_SORT_ORDER = 5000;
    const int TIMER_SORT_ORDER = 4500;

    void Awake() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;
    void OnDisable() => StopWarningAudio();

    void Start() => ResetAndShowPaused();

    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;
        _reloading = false;

        EnsureEventSystem();

        if (!timerText)
        {
            var go = GameObject.Find("TimerText");
            if (go) timerText = go.GetComponent<TextMeshProUGUI>();
        }
        if (timerText) timerText.gameObject.SetActive(true);

        if (timerText)
        {
            _rt = timerText.rectTransform;
            timerText.color = normalColor;
            timerText.fontSize = baseFontSize;
            _rt.localScale = Vector3.one;
            EnsureTimerOnTop();
            EnsureTimerBackground();
            UpdateTimerBackgroundVisual(false);
            CacheTimerContainer();
            ApplyTimerPositionOffset();
        }

        if (!gameOverPanel)
            gameOverPanel = FindPanelByNameIncludingInactive(gameOverPanelName);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        BindRetryButton();
        ResetAndShowPaused();
    }

    void Update()
    {
        if (isGameOver || !isRunning) return;

        currentTime -= Time.deltaTime;
        currentTime = Mathf.Clamp(currentTime, 0f, startTime);
        UpdateTimerUI();

        if (currentTime <= 0f)
        {
            timeExpired = true;
            if (killedAtTimeout < 0)
                CaptureStateAtTimeout();
            GameOver();
        }
    }

    public void StartTimer(float seconds = -1f)
    {
        if (seconds > 0f) startTime = seconds;

        currentTime = startTime;
        isGameOver = false;
        isRunning = true;
        timeExpired = false;
        killedAtTimeout = -1;
        remainingAtTimeout = -1;
        StopWarningAudio();
        _warningActive = false;

        if (timerText) timerText.gameObject.SetActive(true);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        UpdateTimerUI();

        SetTopLeftButtonsVisible(true);

        if (AnalyticsManager.I != null)
        {
            AnalyticsManager.I.StartLevelAttempt();

            // RL start: impostors are now identifiable and the player can act
            AnalyticsManager.I.OnImpostorIdentifiable();
        }

    }

    public void StopTimer()
    {
        isRunning = false;
        StopWarningAudio();
        _warningActive = false;
    }

    public void ResumeTimer()
    {
        if (!isGameOver) isRunning = true;
    }

    public void ForceGameOver() => GameOver();

    public void Retry()
    {
        if (_reloading) return;
        _reloading = true;

        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    void ResetAndShowPaused()
    {
        isRunning = false;
        isGameOver = false;
        currentTime = Mathf.Max(0f, startTime);
        timeExpired = false;
        killedAtTimeout = -1;
        remainingAtTimeout = -1;
        UpdateTimerUI();
        EnsureTimerOnTop();
        UpdateTimerBackgroundVisual(false);
        StopWarningAudio();
        _warningActive = false;
    }

    void UpdateTimerUI()
    {
        if (!timerText) return;

        int m = Mathf.FloorToInt(currentTime / 60f);
        int s = Mathf.FloorToInt(currentTime % 60f);
        timerText.text = $"{m:00}:{s:00}";

        // --- visual state ---
        bool inWarning = isRunning && !isGameOver && currentTime <= warningThreshold && currentTime > 0f;

        if (!inWarning)
        {
            // normal state
            timerText.color = normalColor;
            timerText.fontSize = baseFontSize;
            if (_rt) _rt.localScale = Vector3.one;
        }
        else
        {
            // warning state: dark red + beating
            float blink = (Mathf.Sin(Time.unscaledTime * warningBlinkSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(warningBlinkMinAlpha, warningBlinkMaxAlpha, blink);
            var warnColor = warningColor;
            warnColor.a = Mathf.Clamp01(alpha);
            timerText.color = warnColor;
            timerText.fontSize = warningFontSize;

            if (_rt)
            {
                // nice smooth beat using sine
                float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
                float sc = Mathf.Lerp(pulseScaleMin, pulseScaleMax, t);
                _rt.localScale = new Vector3(sc, sc, 1f);
            }
        }

        UpdateTimerBackgroundVisual(inWarning);
        UpdateWarningState(inWarning);
        ApplyTimerPositionOffset();
    }

    void UpdateWarningState(bool inWarning)
    {
        if (inWarning == _warningActive)
            return;

        _warningActive = inWarning;
        if (_warningActive)
            StartWarningAudio();
        else
            StopWarningAudio();
    }

    void StartWarningAudio()
    {
        EnsureWarningAudioSource();
        EnsureWarningClip();

        if (_warningAudioSource && clockTickingClip)
        {
            _warningAudioSource.volume = clockTickingVolume;
            _warningAudioSource.clip = clockTickingClip;
            if (!_warningAudioSource.isPlaying)
                _warningAudioSource.Play();
        }
    }

    void StopWarningAudio()
    {
        if (_warningAudioSource && _warningAudioSource.isPlaying)
            _warningAudioSource.Stop();
    }

    void EnsureWarningAudioSource()
    {
        if (_warningAudioSource)
            return;

        var go = new GameObject("TimerWarningAudio");
        go.transform.SetParent(transform, false);
        _warningAudioSource = go.AddComponent<AudioSource>();
        _warningAudioSource.playOnAwake = false;
        _warningAudioSource.loop = true;
        _warningAudioSource.spatialBlend = 0f;
        _warningAudioSource.volume = clockTickingVolume;
    }

    void EnsureWarningClip()
    {
        if (clockTickingClip)
            return;

        if (!string.IsNullOrEmpty(clockTickingResourcePath))
            clockTickingClip = Resources.Load<AudioClip>(clockTickingResourcePath);
    }

    void EnsureGameOverClip()
    {
        if (gameOverClip)
            return;

        if (!string.IsNullOrEmpty(gameOverClipResourcePath))
            gameOverClip = Resources.Load<AudioClip>(gameOverClipResourcePath);
    }

    void EnsureGameOverAudioSource()
    {
        if (_gameOverAudioSource)
            return;

        var go = new GameObject("TimerGameOverAudio");
        go.transform.SetParent(transform, false);
        _gameOverAudioSource = go.AddComponent<AudioSource>();
        _gameOverAudioSource.playOnAwake = false;
        _gameOverAudioSource.loop = false;
        _gameOverAudioSource.spatialBlend = 0f;
    }

    void PlayGameOverAudio()
    {
        EnsureGameOverAudioSource();
        EnsureGameOverClip();

        if (_gameOverAudioSource && gameOverClip)
            _gameOverAudioSource.PlayOneShot(gameOverClip, gameOverVolume);
    }

    void EnsureTimerBackground()
    {
        if (!timerText)
            return;

        if (!timerBackground)
        {
            var parent = timerText.transform.parent;
            if (parent)
            {
                var existing = parent.Find("TimerBackground");
                if (existing)
                    timerBackground = existing.GetComponent<Image>();
            }
        }

        if (!timerBackground)
        {
            Transform parent = timerText.transform.parent;
            var bgGO = new GameObject("TimerBackground", typeof(RectTransform), typeof(Image));
            var rect = bgGO.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            timerBackground = bgGO.GetComponent<Image>();
            timerBackground.raycastTarget = false;
        }

        _backgroundRect = timerBackground ? timerBackground.rectTransform : null;
        MaintainBackgroundSiblingOrder();
    }

    void UpdateTimerBackgroundVisual(bool inWarning)
    {
        if (!timerText)
            return;

        if (!timerBackground)
            EnsureTimerBackground();
        if (!timerBackground)
            return;

        if (!_backgroundRect)
            _backgroundRect = timerBackground.rectTransform;

        var textRect = _rt ? _rt : timerText.rectTransform;
        CopyRectTransform(_backgroundRect, textRect);

        float width = Mathf.Max(timerText.preferredWidth, textRect.rect.width);
        float height = Mathf.Max(timerText.preferredHeight, textRect.rect.height);

        if (width <= 0f)
            width = timerText.fontSize * 2f;
        if (height <= 0f)
            height = timerText.fontSize * 1.4f;

        float paddedWidth = width + Mathf.Max(0f, backgroundPadding.x);
        float paddedHeight = height + Mathf.Max(0f, backgroundPadding.y);

        float targetWidth = paddedWidth;
        float targetHeight = paddedHeight;
        if (maintainSquareBackground)
        {
            float side = Mathf.Max(paddedWidth, paddedHeight);
            targetWidth = targetHeight = side;
            _lastBackgroundSide = side;
        }
        else
        {
            _lastBackgroundSide = targetHeight;
        }

        _backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        _backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        timerBackground.color = inWarning ? warningBackgroundColor : backgroundColor;
        _lastBackgroundSide = Mathf.Max(targetWidth, targetHeight);

        MaintainBackgroundSiblingOrder();
        timerBackground.gameObject.SetActive(timerText.gameObject.activeSelf);
    }

    void CacheTimerContainer()
    {
        _timerContainer = null;
        if (!timerText)
            return;

        var parent = timerText.transform.parent as RectTransform;
        _timerContainer = parent ? parent : _rt;
        if (_timerContainer && !_hasTimerBasePos)
        {
            _timerContainerBaseAnchoredPos = _timerContainer.anchoredPosition;
            _hasTimerBasePos = true;
        }
    }

    void ApplyTimerPositionOffset()
    {
        if (!_rt)
        {
            if (timerText) _rt = timerText.rectTransform;
            else return;
        }

        if (_timerContainer == null || _timerContainer != timerText.transform.parent)
            CacheTimerContainer();
        if (!_timerContainer)
            return;

        if (!_hasTimerBasePos)
        {
            _timerContainerBaseAnchoredPos = _timerContainer.anchoredPosition;
            _hasTimerBasePos = true;
        }

        Vector2 desired = _timerContainerBaseAnchoredPos + timerPositionOffset;

        float height = maintainSquareBackground ? _lastBackgroundSide : 0f;
        if (height <= 0f && _backgroundRect)
            height = _backgroundRect.rect.height;
        if (height <= 0f && _rt)
            height = _rt.rect.height;
        if (height <= 0f)
            height = 120f;

        float enforcedMargin = Mathf.Max(topEdgeMargin, minimumTopEdgeMargin);
        float minCenterY = -(height * 0.5f + enforcedMargin);
        desired.y = Mathf.Min(desired.y, minCenterY);

        if (_timerContainer.anchoredPosition != desired)
            _timerContainer.anchoredPosition = desired;
    }

    void MaintainBackgroundSiblingOrder()
    {
        if (!timerBackground || !timerText)
            return;

        var bgTransform = timerBackground.transform;
        var textTransform = timerText.transform;

        if (bgTransform.parent != textTransform.parent)
        {
            bgTransform.SetParent(textTransform.parent, false);
        }

        if (!bgTransform.parent)
            return;

        int targetIndex = Mathf.Max(0, textTransform.GetSiblingIndex());
        bgTransform.SetSiblingIndex(targetIndex);
        textTransform.SetSiblingIndex(bgTransform.GetSiblingIndex() + 1);
    }

    void CopyRectTransform(RectTransform target, RectTransform source)
    {
        if (!target || !source)
            return;

        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
    }


    void GameOver()
    {
        if (isGameOver)
            return;

        if (_rt) _rt.localScale = Vector3.one;

        isGameOver = true;
        isRunning = false;
        StopWarningAudio();
        _warningActive = false;

        PlayGameOverAudio();
        
        TryFillGameOverScore();

        ShowPanelOnTopAndMakeClickable(gameOverPanel);

        SetTopLeftButtonsVisible(false);

        // 🔹 Analytics: record a failed attempt (timeout)
        if (AnalyticsManager.I != null)
            AnalyticsManager.I.EndAttemptFail();

        Time.timeScale = 0f;
    }

    
    void TryFillGameOverScore()
    {
        if (!gameOverPanel) return;

        TextMeshProUGUI scoreTMP = null;
        var tmps = gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in tmps)
            if (t.name == scoreTextName) { scoreTMP = t; break; }

        if (!scoreTMP)
        {
            Debug.Log("[TimerController] ScoreText not found under GameOver Panel (optional).");
            return;
        }

        int total = 0, killed = 0, remaining = 0;
        var tracker = ImpostorTracker.Instance ?? FindObjectOfType<ImpostorTracker>(true);
        if (tracker != null)
        {
            killed = tracker.Killed;
            remaining = tracker.Remaining;
            total = tracker.TotalSpawned;

            if (total <= 0) total = killed + remaining; 
        }

        if (HasTimeExpired)
        {
            if (remainingAtTimeout >= 0 && total > 0)
            {
                int preTimeoutKills = total - remainingAtTimeout;
                killed = Mathf.Clamp(preTimeoutKills, 0, total);
            }
            else if (killedAtTimeout >= 0)
            {
                killed = Mathf.Min(killed, killedAtTimeout);
            }
        }

        scoreTMP.text = $"You eliminated {killed} out of {total} imposters!";
    }

    void CaptureStateAtTimeout()
    {
        var tracker = ImpostorTracker.Instance ?? FindObjectOfType<ImpostorTracker>(true);
        killedAtTimeout = tracker != null ? tracker.Killed : 0;
        remainingAtTimeout = tracker != null ? tracker.Remaining : -1;
    }

   
    void EnsureEventSystem()
    {
        if (!FindObjectOfType<EventSystem>())
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }
    }

    void EnsureTimerOnTop()
    {
        if (!timerText)
            return;

        var canvas = timerText.GetComponentInParent<Canvas>();
        if (!canvas)
            return;

        canvas.overrideSorting = true;
        if (canvas.sortingOrder < TIMER_SORT_ORDER)
            canvas.sortingOrder = TIMER_SORT_ORDER;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    }

    GameObject FindPanelByNameIncludingInactive(string panelName)
    {
        if (string.IsNullOrEmpty(panelName)) return null;

        var canvases = GameObject.FindObjectsOfType<Canvas>(true);
        foreach (var canvas in canvases)
        {
            var trs = canvas.GetComponentsInChildren<Transform>(true);
            foreach (var t in trs)
                if (t.name == panelName)
                    return t.gameObject;
        }
        return GameObject.Find(panelName);
    }

    void ShowPanelOnTopAndMakeClickable(GameObject panel)
    {
        if (!panel) return;

        var t = panel.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);

            var cg = t.GetComponent<CanvasGroup>();
            if (cg)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            t = t.parent;
        }

        var topCanvas = panel.GetComponent<Canvas>();
        if (!topCanvas) topCanvas = panel.AddComponent<Canvas>();
        topCanvas.overrideSorting = true;
        topCanvas.sortingOrder = TOP_SORT_ORDER;
        topCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var gr = panel.GetComponent<GraphicRaycaster>();
        if (!gr) panel.AddComponent<GraphicRaycaster>();

        panel.transform.SetAsLastSibling();

        BindRetryButton();
    }

    void BindRetryButton()
    {
        if (!gameOverPanel) return;

        Button retry = null;
        var buttons = gameOverPanel.GetComponentsInChildren<Button>(true);
        foreach (var b in buttons)
            if (b.name == retryButtonName) { retry = b; break; }

        if (retry == null)
        {
            var texts = gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
                if (t.text.Trim().ToLower().Contains("retry"))
                {
                    retry = t.GetComponentInParent<Button>(true);
                    if (retry) break;
                }
        }

        if (retry == null) return;

        if (retry.targetGraphic != null)
            retry.targetGraphic.raycastTarget = true;

        retry.onClick.RemoveAllListeners();
        retry.onClick.AddListener(Retry);

        var et = retry.gameObject.GetComponent<EventTrigger>();
        if (et && et.triggers != null)
        {
            et.triggers.RemoveAll(e => e != null && e.eventID == EventTriggerType.PointerClick);
            if (et.triggers.Count == 0) Destroy(et);
        }
    }

    public void SetTopLeftButtonsVisible(bool visible)
{
    var go = topLeftButtonsRoot ? topLeftButtonsRoot : GameObject.Find("TopLeftButtons");
    if (go) go.SetActive(visible);
}

    
public void PreparePanelForClicks(GameObject panel)
{
    if (!panel) return;

    
    var t = panel.transform;
    while (t != null)
    {
        if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
        var cg = t.GetComponent<CanvasGroup>();
        if (cg) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
        t = t.parent;
    }

    
    var c = panel.GetComponent<Canvas>();
    if (!c) c = panel.AddComponent<Canvas>();
    c.overrideSorting = true;
    c.sortingOrder = 5000;         
    c.renderMode = RenderMode.ScreenSpaceOverlay;

    if (!panel.GetComponent<GraphicRaycaster>())
        panel.AddComponent<GraphicRaycaster>();

    panel.transform.SetAsLastSibling();
}


public void WireButtonToRetry(GameObject root, string buttonNameOrText = "Exit")
{
    if (!root) return;

    Button target = null;

    
    foreach (var b in root.GetComponentsInChildren<Button>(true))
        if (b.name == buttonNameOrText) { target = b; break; }

    
    if (!target)
    {
        foreach (var tmp in root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
        {
            var txt = tmp.text.Trim().ToLower();
            if (txt.Contains(buttonNameOrText.Trim().ToLower()))
            {
                target = tmp.GetComponentInParent<Button>(true);
                if (target) break;
            }
        }
    }

    if (!target) return;

    if (target.targetGraphic) target.targetGraphic.raycastTarget = true;
    target.onClick.RemoveAllListeners();
    target.onClick.AddListener(Retry);   
}


}
