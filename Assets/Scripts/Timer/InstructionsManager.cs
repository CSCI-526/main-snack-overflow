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
//            timerController.StartTimer(60f);
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
//         timerController.StartTimer(60f);

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
//             timerController.StartTimer(60f);

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

public class InstructionsManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI instructionsText;
    public GameObject instructionsPanel;
    public Button startButton;
    public Button skipButton;   // NEW: assign in Inspector

    [TextArea(3, 10)]
    public string fullText =
        "Memorize the shapes and colors.\n\n" +
        "• The color represents the impostor’s color.\n" +
        "• The shape represents the impostor’s movement path.\n\n" +
        "Find and eliminate impostors that match both color and shape.\n" +
        "You only have 3 beams to catch them!\n\n" +
        "Controls: WASD / Arrow Keys";

    public float typingSpeed = 0.02f;

    [Header("Game Flow")]
    public MemoryBarController memoryBar;
    public TimerController timerController;
    public GameObject[] enableOnGameplay;

    [Header("Auto Start")]
    public bool autoStart = true;
    [Range(0.5f, 6f)] public float autoStartDelay = 4.0f;

    void Start()
    {
        // Pause gameplay while instructions are up
        Time.timeScale = 0f;

        instructionsPanel.SetActive(true);

        // Hide buttons (no click flow)
        if (startButton) startButton.gameObject.SetActive(false);
        if (skipButton)  skipButton.gameObject.SetActive(false);

        // Subscribe to memory complete (so timer/spawn happen once)
        if (memoryBar != null)
            memoryBar.OnMemoryPhaseComplete += HandleMemoryComplete;

        // Auto-advance after delay
        if (autoStart) StartCoroutine(AutoStartAfterDelay());
    }

    void OnDestroy()
    {
        startButton.onClick.RemoveListener(OnStartClicked);
        if (skipButton) skipButton.onClick.RemoveListener(SkipIntro);

        if (memoryBar != null)
            memoryBar.OnMemoryPhaseComplete -= HandleMemoryComplete;
    }

    // Call this from your entry point to show instructions
    public void StartInstructions()
    {
        instructionsPanel.SetActive(true);
        StartCoroutine(TypeText());
    }

    public IEnumerator TypeText()
{
    instructionsText.text = "";
    if (skipButton) skipButton.gameObject.SetActive(true); // visible while typing

    foreach (char c in fullText)
    {
        instructionsText.text += c;
        yield return new WaitForSecondsRealtime(typingSpeed);
    }

    yield return new WaitForSecondsRealtime(0.5f);

    if (skipButton) skipButton.gameObject.SetActive(false); // hide after typing done
    startButton.gameObject.SetActive(true);
}


    // START = hide panel, begin memory phase ONLY (no timer/spawn here)
    void OnStartClicked()
    {
        instructionsPanel.SetActive(false);
        if (skipButton) skipButton.gameObject.SetActive(false);

        // Keep the game paused; MemoryBar handles pause/blur and will unpause when it ends
        if (memoryBar != null)
            memoryBar.BeginMemoryPhase();
    }

    // SKIP = same as START, but instantly (no typing)
    void SkipIntro()
    {
        StopAllCoroutines();                 // stop typing
        instructionsPanel.SetActive(false);  // hide panel immediately
        if (skipButton) skipButton.gameObject.SetActive(false);

        // Begin memory phase ONLY — no timer/spawn here to avoid duplicates
        if (memoryBar != null)
            memoryBar.BeginMemoryPhase();
    }

    // This is the ONLY place that starts timer & spawns
    void HandleMemoryComplete()
    {
        // Resume gameplay time (MemoryBar typically unpauses, but this is safe)
        Time.timeScale = 1f;

        if (timerController != null)
            timerController.StartTimer(60f);

        // Spawn once, right after memory phase completes
        var spawner = FindObjectOfType<SpawnManager>();
        if (spawner != null)
            spawner.StartSpawning();

        // Enable gameplay HUD / inputs
        if (enableOnGameplay != null)
        {
            foreach (var go in enableOnGameplay)
                if (go) go.SetActive(true);
        }
    }

    IEnumerator AutoStartAfterDelay()
    {
        // Ensure the single-line is visible the whole time
        // (Set your one-line text in the Inspector on instructionsText)
        yield return new WaitForSecondsRealtime(autoStartDelay);

        // Hide intro panel and start Memory phase (THIS replaces the Start button)
        instructionsPanel.SetActive(false);

        if (memoryBar != null)
            memoryBar.BeginMemoryPhase();
    }

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
//             timerController.StartTimer(60f);

//         FindObjectOfType<SpawnManager>()?.StartSpawning();

//         if (enableOnGameplay != null)
//             foreach (var go in enableOnGameplay)
//                 if (go) go.SetActive(true);
//     }
// }
