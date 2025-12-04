//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections;

//public class InstructionsManager : MonoBehaviour
//{
//    public TextMeshProUGUI instructionsText;
//    public GameObject instructionsPanel;
//    public Button startButton;

//    [TextArea(3, 10)]
//    public string fullText =
//        "Welcome, Agent!\n\n" +
//        "Mission:\nYou will receive clues about imposters — their color and the shape in which they roam the city.\n" +
//        "Memorize these carefully.\nOnce the clues disappear, find and eliminate all imposters by matching both color and shape.\n\n" +
//        "You have 1 minute to kill them all. Good luck!";
//    public float typingSpeed = 0.03f;

//    [Header("Game Flow")]
//    public MemoryBarController memoryBar;
//    public TimerController timerController;
//    public GameObject[] enableOnGameplay;

//    void Start()
//    {
//        Time.timeScale = 0f; 
//        startButton.gameObject.SetActive(false);
//        StartCoroutine(TypeText());
//        startButton.onClick.AddListener(OnStartClicked);

//        if (memoryBar != null)
//            memoryBar.OnMemoryPhaseComplete += HandleMemoryComplete;  // now matches Action
//    }

//    void OnDestroy()
//    {
//        startButton.onClick.RemoveListener(OnStartClicked);
//        if (memoryBar != null)
//            memoryBar.OnMemoryPhaseComplete -= HandleMemoryComplete;
//    }

//    IEnumerator TypeText()
//    {
//        instructionsText.text = "";
//        foreach (char c in fullText)
//        {
//            instructionsText.text += c;
//            yield return new WaitForSecondsRealtime(typingSpeed);
//        }
//        yield return new WaitForSecondsRealtime(0.3f);
//        startButton.gameObject.SetActive(true);
//    }

//    void OnStartClicked()
//    {
//        instructionsPanel.SetActive(false);

//        if (memoryBar != null)
//            memoryBar.BeginMemoryPhase();   // pauses & shows clues, then fires event
//    }

//    // CHANGED: no parameters
//    void HandleMemoryComplete()
//    {
//        if (timerController != null)
//            timerController.StartTimer(90f);
//        FindObjectOfType<SpawnManager>()?.StartSpawning();


//        if (enableOnGameplay != null)
//            foreach (var go in enableOnGameplay)
//                if (go) go.SetActive(true);
//    }
//}

// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using System.Collections;

// public class InstructionsManager : MonoBehaviour
// {
//     public TextMeshProUGUI instructionsText;
//     public GameObject instructionsPanel;
//     public Button startButton;

//     [TextArea(3, 10)]
//     public string fullText =
//         "Welcome, Agent!\n\n" +
//         "Mission:\nYou will receive clues about imposters — their color and the shape in which they roam the city.\n" +
//         "Memorize these carefully.\nOnce the clues disappear, find and eliminate all imposters by matching both color and shape.\n\n" +
//         "You have 1 minute to kill them all. Good luck!";

//     public float typingSpeed = 0.03f;

//     [Header("Game Flow")]
//     public MemoryBarController memoryBar;
//     public TimerController timerController;
//     public GameObject[] enableOnGameplay;

    
//     void Start()
//     {
//         Time.timeScale = 0f;
//         startButton.gameObject.SetActive(false);
//         startButton.onClick.AddListener(OnStartClicked);

//         if (memoryBar != null)
//             memoryBar.OnMemoryPhaseComplete += HandleMemoryComplete;

        
//         instructionsPanel.SetActive(false); 
//     }

//     void OnDestroy()
//     {
//         startButton.onClick.RemoveListener(OnStartClicked);
//         if (memoryBar != null)
//             memoryBar.OnMemoryPhaseComplete -= HandleMemoryComplete;
//     }

    
//     public void StartInstructions()
//     {
//         instructionsPanel.SetActive(true);
//         StartCoroutine(TypeText());
//     }

//     public IEnumerator TypeText()
//     {
//         instructionsText.text = "";
//         foreach (char c in fullText)
//         {
//             instructionsText.text += c;
//             yield return new WaitForSecondsRealtime(typingSpeed);
//         }
//         yield return new WaitForSecondsRealtime(0.6f);
//         startButton.gameObject.SetActive(true);
//     }

//     void OnStartClicked()
//     {
//         instructionsPanel.SetActive(false);

//         if (memoryBar != null)
//             memoryBar.BeginMemoryPhase();   
//     }
// void HandleMemoryComplete()
// {
   
//     if (timerController != null)
//         timerController.StartTimer(90f);

//     FindObjectOfType<SpawnManager>()?.StartSpawning();

//     if (enableOnGameplay != null)
//         foreach (var go in enableOnGameplay)
//             if (go) go.SetActive(true);
// }

// }



// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using System.Collections;

// public class InstructionsManager : MonoBehaviour
// {
//     [Header("UI References")]
//     public TextMeshProUGUI instructionsText;
//     public GameObject instructionsPanel;
//     public Button startButton;
//     public Button skipButton;


//     [TextArea(3, 10)]
//     public string fullText =
//         "Memorize the shapes and colors.\n\n" +
//         "• The color represents the impostor’s color.\n" +
//         "• The shape represents the impostor’s movement path.\n\n" +
//         "Find and eliminate impostors that match both color and shape.\n" +
//         "You only have 3 beams to catch them!\n\n" +
//         "Controls: WASD / Arrow Keys";

//     public float typingSpeed = 0.02f;

//     [Header("Game Flow")]
//     public MemoryBarController memoryBar;
//     public TimerController timerController;
//     public GameObject[] enableOnGameplay;

//     void Start()
//     {
//         Time.timeScale = 0f;
//         startButton.gameObject.SetActive(false);
//         skipButton.gameObject.SetActive(true);
//         startButton.onClick.AddListener(OnStartClicked);
//         skipButton.onClick.AddListener(SkipIntro);

//         if (memoryBar != null)
//             memoryBar.OnMemoryPhaseComplete += HandleMemoryComplete;

//         instructionsPanel.SetActive(false);
//     }

//     void OnDestroy()
//     {
//         startButton.onClick.RemoveListener(OnStartClicked);
//         if (memoryBar != null)
//             memoryBar.OnMemoryPhaseComplete -= HandleMemoryComplete;
//     }

//     public void StartInstructions()
//     {
//         instructionsPanel.SetActive(true);
//         StartCoroutine(TypeText());
//     }

//     public IEnumerator TypeText()
//     {
//         instructionsText.text = "";
//         foreach (char c in fullText)
//         {
//             instructionsText.text += c;
//             yield return new WaitForSecondsRealtime(typingSpeed);
//         }
//         yield return new WaitForSecondsRealtime(0.5f);
//         startButton.gameObject.SetActive(true);
//     }

//     void OnStartClicked()
//     {
//         instructionsPanel.SetActive(false);
//         if (memoryBar != null)
//             memoryBar.BeginMemoryPhase();
//     }

//     void HandleMemoryComplete()
//     {
//         if (timerController != null)
//             timerController.StartTimer(90f);

//         FindObjectOfType<SpawnManager>()?.StartSpawning();

//         if (enableOnGameplay != null)
//         {
//             foreach (var go in enableOnGameplay)
//                 if (go) go.SetActive(true);
//         }
//     }
//     void SkipIntro()
//     {
//         StopAllCoroutines();
//         instructionsText.text = fullText; // instantly show the full text
//         startButton.gameObject.SetActive(true); // allow player to proceed
//         skipButton.gameObject.SetActive(false); // hide skip button
//     }
// }

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class InstructionsManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI instructionsText;
    public GameObject instructionsPanel;
    public Button startButton;
    public Button skipButton;

    [Header("Vision Mask")]
    public GameObject visionMask;   // assign VisionMask GO in Inspector

    [Header("Level 1 Override")]
    [SerializeField] bool overrideLevel1Instructions = true;
    [SerializeField] string level1InstructionText = "Find the red impostor capsule x10.";
    [SerializeField] Image level1ImpostorCapsule;
    [SerializeField] RectTransform level1CapsuleParent;
    [SerializeField] Vector2 level1CapsuleAnchor = new Vector2(0.5f, 0.5f);
    [SerializeField] Vector2 level1CapsuleSize = new Vector2(60f, 120f);
    [SerializeField] Vector2 level1CapsuleOffset = new Vector2(0f, 18f);
    [SerializeField] Color level1CapsuleColor = new Color32(255, 0, 0, 255);
    [SerializeField] Sprite level1CapsuleSprite;

    [Header("Level 2 Override")]
    [SerializeField] bool overrideLevel2Instructions = true;
    [SerializeField] string level2InstructionText = "Keep an eye out for impostors. Watch the capsule change colors.";
    [SerializeField] Image level2ImpostorCapsule;
    [SerializeField] RectTransform level2CapsuleParent;
    [SerializeField] Vector2 level2CapsuleAnchor = new Vector2(0.5f, 0.5f);
    [SerializeField] Vector2 level2CapsuleSize = new Vector2(60f, 120f);
    [SerializeField] Vector2 level2CapsuleOffset = new Vector2(0f, 18f);
    [SerializeField] Color[] level2CycleColors = {
        new Color32(255, 0, 0, 255),
        new Color32(0, 160, 255, 255),
        new Color32(90, 200, 90, 255),
        new Color32(240, 200, 20, 255)
    };
    [Range(0.2f, 8f)] [SerializeField] float level2ColorCycleDuration = 1.8f;

    [TextArea(3, 10)]
    public string fullText =
        "Find and eliminate impostors that match both color and shape.\n" +
        "You only have 3 beams to catch them!\n\n" +
        "Controls: WASD / Arrow Keys";

    public float typingSpeed = 0.02f;

    [Header("Game Flow")]
    public TimerController timerController;
    public GameObject[] enableOnGameplay;

    [Header("Auto Start")]
    public bool autoStart = true;
    [Range(0.5f, 6f)] public float autoStartDelay = 4.0f;

    bool spawnTriggered;
    bool gameplayUiEnabled;
    SpawnManager cachedSpawner;
    Coroutine level2CycleRoutine;

    void Start()
    {
        // Pause gameplay while instructions are up
        Time.timeScale = 0f;

        spawnTriggered = false;
        gameplayUiEnabled = false;

        instructionsPanel.SetActive(true);
        ApplyLevelSpecificInstructions();

        SetVisionMaskActive(false);
        EnableGameplayUI(false);

        if (startButton)
        {
            startButton.gameObject.SetActive(false);
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (skipButton)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.onClick.AddListener(SkipIntro);
            var skipLabel = skipButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (skipLabel)
                skipLabel.text = "Skip Tutorial";
        }

        if (autoStart)
            StartCoroutine(AutoStartAfterDelay());
    }

    void OnDestroy()
    {
        if (startButton) startButton.onClick.RemoveListener(OnStartClicked);
        if (skipButton) skipButton.onClick.RemoveListener(SkipIntro);
    }

    public void StartInstructions()
    {
        instructionsPanel.SetActive(true);
        StartCoroutine(TypeText());
    }

    public IEnumerator TypeText()
    {
        ApplyLevelSpecificInstructions();
        instructionsText.text = "";
        if (skipButton) skipButton.gameObject.SetActive(true);

        foreach (char c in fullText)
        {
            instructionsText.text += c;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        yield return new WaitForSecondsRealtime(0.5f);

        if (skipButton) skipButton.gameObject.SetActive(false);
        startButton.gameObject.SetActive(true);
    }

    // START = hide panel, start gameplay directly
    void OnStartClicked()
    {
        instructionsPanel.SetActive(false);
        if (skipButton) skipButton.gameObject.SetActive(false);
        StopLevel2Cycle();

        var tutorial = FindObjectOfType<Level1TutorialController>(true);
        if (tutorial != null && tutorial.isActiveAndEnabled && tutorial.TryBeginTutorial(this))
            return;

        if (Level2TutorialController.TryBeginTutorial(this))
            return;

        if (Level3TutorialController.TryBeginTutorial(this))
            return;

        if (Level4TutorialController.TryBeginTutorial(this))
            return;

        StartGameplayNow(true);
    }

    // SKIP = same as START, but instant
    void SkipIntro()
    {
        StopAllCoroutines();
        instructionsPanel.SetActive(false);
        if (skipButton) skipButton.gameObject.SetActive(false);
        StopLevel2Cycle();

        StartGameplayNow(true);
    }

    IEnumerator AutoStartAfterDelay()
    {
        yield return new WaitForSecondsRealtime(autoStartDelay);
        OnStartClicked(); // just use the same logic
    }

    void StartGameplayNow(bool startTimer)
    {
        Time.timeScale = 1f;

        SetVisionMaskActive(true);
        TriggerSpawnIfNeeded();
        EnableGameplayUI(true);

        if (startTimer && timerController != null)
            timerController.StartTimer(90f);
    }

    public void TriggerSpawnIfNeeded()
    {
        if (spawnTriggered)
            return;

        var spawner = GetSpawner();
        if (spawner != null)
        {
            spawner.StartSpawning();
            spawnTriggered = true;
        }
    }

    public void EnableGameplayUI(bool enable)
    {
        if (gameplayUiEnabled == enable)
            return;
        gameplayUiEnabled = enable;

        if (enableOnGameplay == null)
            return;

        foreach (var go in enableOnGameplay)
            if (go) go.SetActive(enable);
    }

    public void SetVisionMaskActive(bool active)
    {
        if (!visionMask)
            return;

        visionMask.SetActive(active);

        if (VisionMaskController.Instance != null)
        {
            if (active)
                VisionMaskController.Instance.ShowMask();
            else
                VisionMaskController.Instance.HideMask();
        }
    }

    SpawnManager GetSpawner()
    {
        if (cachedSpawner == null)
            cachedSpawner = FindObjectOfType<SpawnManager>();
        return cachedSpawner;
    }

    void ApplyLevelSpecificInstructions()
    {
        bool handledLevel1 = ApplyLevel1Instructions();
        bool handledLevel2 = ApplyLevel2Instructions();

        if (!handledLevel1 && !handledLevel2)
        {
            SetLevel1PreviewVisible(false);
            SetLevel2PreviewVisible(false);
            StopLevel2Cycle();
        }
    }

    bool ApplyLevel1Instructions()
    {
        if (!overrideLevel1Instructions || !IsLevelOneScene())
            return false;

        fullText = string.IsNullOrWhiteSpace(level1InstructionText)
            ? "Find the red impostor capsule x10."
            : level1InstructionText.Trim();

        var capsule = EnsureLevel1CapsulePreview();
        if (capsule)
        {
            capsule.color = level1CapsuleColor;
            capsule.rectTransform.anchorMin = level1CapsuleAnchor;
            capsule.rectTransform.anchorMax = level1CapsuleAnchor;
            capsule.rectTransform.sizeDelta = level1CapsuleSize;
            capsule.rectTransform.anchoredPosition = level1CapsuleOffset;
            capsule.gameObject.SetActive(true);
        }

        SetLevel2PreviewVisible(false);
        StopLevel2Cycle();
        return true;
    }

    bool ApplyLevel2Instructions()
    {
        if (!overrideLevel2Instructions || !IsLevelTwoScene())
            return false;

        fullText = string.IsNullOrWhiteSpace(level2InstructionText)
            ? "Keep an eye out for impostors. Watch the capsule change colors."
            : level2InstructionText.Trim();

        var capsule = EnsureLevel2CapsulePreview();
        if (capsule)
        {
            capsule.rectTransform.anchorMin = level2CapsuleAnchor;
            capsule.rectTransform.anchorMax = level2CapsuleAnchor;
            capsule.rectTransform.sizeDelta = level2CapsuleSize;
            capsule.rectTransform.anchoredPosition = level2CapsuleOffset;
            capsule.gameObject.SetActive(true);
            BeginLevel2Cycle(capsule);
        }

        SetLevel1PreviewVisible(false);
        return true;
    }

    Image EnsureLevel1CapsulePreview()
    {
        if (level1ImpostorCapsule)
            return level1ImpostorCapsule;

        RectTransform parent =
            level1CapsuleParent ? level1CapsuleParent :
            instructionsText ? instructionsText.rectTransform :
            instructionsPanel ? instructionsPanel.transform as RectTransform : null;

        if (!parent)
            return null;

        var go = new GameObject("Level1ImpostorCapsule", typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = level1CapsuleAnchor;
        rect.anchorMax = level1CapsuleAnchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = level1CapsuleSize;
        rect.anchoredPosition = level1CapsuleOffset;

        var img = go.GetComponent<Image>();
        img.sprite = GetLevel1CapsuleSprite();
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.raycastTarget = false;

        level1ImpostorCapsule = img;
        return img;
    }

    void SetLevel1PreviewVisible(bool visible)
    {
        if (level1ImpostorCapsule)
            level1ImpostorCapsule.gameObject.SetActive(visible);
    }

    Image EnsureLevel2CapsulePreview()
    {
        if (level2ImpostorCapsule)
            return level2ImpostorCapsule;

        RectTransform parent =
            level2CapsuleParent ? level2CapsuleParent :
            instructionsText ? instructionsText.rectTransform :
            instructionsPanel ? instructionsPanel.transform as RectTransform : null;

        if (!parent)
            return null;

        var go = new GameObject("Level2ImpostorCapsule", typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = level2CapsuleAnchor;
        rect.anchorMax = level2CapsuleAnchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = level2CapsuleSize;
        rect.anchoredPosition = level2CapsuleOffset;

        var img = go.GetComponent<Image>();
        img.sprite = GetLevel1CapsuleSprite(); // reuse the generated capsule sprite
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.raycastTarget = false;

        level2ImpostorCapsule = img;
        return img;
    }

    void SetLevel2PreviewVisible(bool visible)
    {
        if (level2ImpostorCapsule)
            level2ImpostorCapsule.gameObject.SetActive(visible);
    }

    void BeginLevel2Cycle(Image capsule)
    {
        StopLevel2Cycle();
        if (!capsule)
            return;

        var colors = (level2CycleColors != null && level2CycleColors.Length > 0)
            ? level2CycleColors
            : new[] { Color.red, Color.yellow };

        capsule.color = colors[0];
        level2CycleRoutine = StartCoroutine(Level2ColorCycle(capsule, colors, level2ColorCycleDuration));
    }

    void StopLevel2Cycle()
    {
        if (level2CycleRoutine != null)
        {
            StopCoroutine(level2CycleRoutine);
            level2CycleRoutine = null;
        }
    }

    IEnumerator Level2ColorCycle(Image capsule, Color[] colors, float duration)
    {
        if (capsule == null || colors == null || colors.Length == 0)
            yield break;

        int count = colors.Length;
        int index = 0;

        while (true)
        {
            int nextIndex = (index + 1) % count;
            Color startColor = colors[index];
            Color endColor = colors[nextIndex];

            float t = 0f;
            while (t < 1f)
            {
                capsule.color = Color.Lerp(startColor, endColor, t);
                t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration);
                yield return null;
            }

            index = nextIndex;
        }
    }

    Sprite GetLevel1CapsuleSprite()
    {
        if (level1CapsuleSprite)
            return level1CapsuleSprite;

        if (cachedCapsuleSprite)
            return cachedCapsuleSprite;

        const int width = 96;
        const int height = 192;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Level1CapsuleTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color32[width * height];
        float halfWidth = 0.5f;
        float bodyHalfHeight = 0.35f;
        float capHeight = 0.5f - bodyHalfHeight;
        float edgeFeather = 0.08f;

        for (int y = 0; y < height; y++)
        {
            float v = (y + 0.5f) / height;
            float centeredY = (v - 0.5f);
            float absY = Mathf.Abs(centeredY);
            float horizontalLimit;

            if (absY <= bodyHalfHeight)
            {
                horizontalLimit = halfWidth;
            }
            else
            {
                float capT = (absY - bodyHalfHeight) / capHeight;
                if (capT > 1f)
                    continue;
                horizontalLimit = Mathf.Sqrt(Mathf.Clamp01(1f - capT * capT)) * halfWidth;
            }

            for (int x = 0; x < width; x++)
            {
                float u = (x + 0.5f) / width;
                float centeredX = Mathf.Abs(u - 0.5f);

                float norm = centeredX / halfWidth;
                if (norm > 1f)
                    continue;

                float edgeDistance = (horizontalLimit / halfWidth) - norm;
                float alpha = Mathf.Clamp01(edgeDistance / edgeFeather);
                if (alpha <= 0f)
                    continue;

                float shading = 0.55f + 0.45f * (1f - Mathf.Pow(centeredX / horizontalLimit, 2f));
                byte shade = (byte)Mathf.Clamp(Mathf.RoundToInt(shading * 255f), 0, 255);
                var c = new Color32(shade, shade, shade, (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
                pixels[y * width + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        cachedCapsuleSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), width);
        cachedCapsuleSprite.name = "Level1CapsuleSprite";
        cachedCapsuleSprite.hideFlags = HideFlags.HideAndDontSave;
        return cachedCapsuleSprite;
    }

    static bool IsLevelOneScene()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.IsValid() && scene.name == "LvL1";
    }

    static bool IsLevelTwoScene()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.IsValid() && scene.name == "LvL2";
    }

    static Sprite cachedCapsuleSprite;
}



// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using System.Collections;

// public class InstructionsManager : MonoBehaviour
// {
//     public TextMeshProUGUI instructionsText;
//     public GameObject instructionsPanel;
//     public Button startButton;

//     [TextArea(3, 10)]
//     public string fullText =
//         "Memorize the shapes and colors.\n\n" +
//         "• The color represents the impostor’s color.\n" +
//         "• The shape represents the impostor’s movement path.\n\n" +
//         "Find and eliminate impostors that match both color and shape.\n" +
//         "You only have 3 beams to catch them!\n\n" +
//         "Controls: WASD / Arrow Keys";

//     [Header("Game Flow")]
//     public MemoryBarController memoryBar;
//     public TimerController timerController;
//     public GameObject[] enableOnGameplay;

//     void Start()
//     {
//         Time.timeScale = 0f;
//         startButton.gameObject.SetActive(false);
//         startButton.onClick.AddListener(OnStartClicked);

//         if (memoryBar != null)
//             memoryBar.OnMemoryPhaseComplete += HandleMemoryComplete;

//         instructionsPanel.SetActive(false);
//     }

//     void OnDestroy()
//     {
//         startButton.onClick.RemoveListener(OnStartClicked);
//         if (memoryBar != null)
//             memoryBar.OnMemoryPhaseComplete -= HandleMemoryComplete;
//     }

//     public void StartInstructions()
//     {
//         instructionsPanel.SetActive(true);
//         instructionsText.text = fullText; // instantly show text
//         startButton.gameObject.SetActive(true);
//     }

//     void OnStartClicked()
//     {
//         instructionsPanel.SetActive(false);

//         if (memoryBar != null)
//             memoryBar.BeginMemoryPhase();
//     }

//     void HandleMemoryComplete()
//     {
//         if (timerController != null)
//             timerController.StartTimer(90f);

//         FindObjectOfType<SpawnManager>()?.StartSpawning();

//         if (enableOnGameplay != null)
//             foreach (var go in enableOnGameplay)
//                 if (go) go.SetActive(true);
//     }
// }
