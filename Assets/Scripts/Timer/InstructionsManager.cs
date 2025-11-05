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
    public Button skipButton;

    [Header("Vision Mask")]
    public GameObject visionMask;   // assign VisionMask GO in Inspector


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

    void Start()
    {
        // Pause gameplay while instructions are up
        Time.timeScale = 0f;

        instructionsPanel.SetActive(true);

        if (visionMask) visionMask.SetActive(false); 

        if (startButton)
        {
            startButton.gameObject.SetActive(false);
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (skipButton)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.onClick.AddListener(SkipIntro);
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

        Time.timeScale = 1f;

        if (visionMask) visionMask.SetActive(true);  

        // Start timer if assigned
        if (timerController != null)
            timerController.StartTimer(60f);

        // Spawn NPCs or impostors
        var spawner = FindObjectOfType<SpawnManager>();
        if (spawner != null)
            spawner.StartSpawning();

        // Enable gameplay UI
        if (enableOnGameplay != null)
        {
            foreach (var go in enableOnGameplay)
                if (go) go.SetActive(true);
        }
    }

    // SKIP = same as START, but instant
    void SkipIntro()
    {
        StopAllCoroutines();
        instructionsPanel.SetActive(false);
        if (skipButton) skipButton.gameObject.SetActive(false);

        Time.timeScale = 1f;

        if (visionMask) visionMask.SetActive(true);

        if (timerController != null)
            timerController.StartTimer(60f);

        var spawner = FindObjectOfType<SpawnManager>();
        if (spawner != null)
            spawner.StartSpawning();

        if (enableOnGameplay != null)
        {
            foreach (var go in enableOnGameplay)
                if (go) go.SetActive(true);
        }
    }

    IEnumerator AutoStartAfterDelay()
    {
        yield return new WaitForSecondsRealtime(autoStartDelay);
        OnStartClicked(); // just use the same logic
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
