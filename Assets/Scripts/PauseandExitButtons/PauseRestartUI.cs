using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

public class PauseRestartUI : MonoBehaviour
{
    [Header("Assign if you want, or leave blank to find by name")]
    [SerializeField] private Button pauseButton;       
    [SerializeField] private Button exitButton;        
    [SerializeField] private GameObject pauseOverlay;  

    [Header("Find-by-name (case-sensitive)")]
    [SerializeField] private string pauseButtonName  = "PauseButton";
    [SerializeField] private string exitButtonName   = "ExitButton";
    [SerializeField] private string pauseOverlayName = "PauseOverlay";

    private TimerController timer;
    private bool paused = false;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnwireButtons();
    }

    void OnEnable()
    {
        TryRebindAll();
    }

    void OnDisable()
    {
        UnwireButtons();
    }

    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;
        paused = false;

        TryRebindAll();

        if (pauseOverlay) pauseOverlay.SetActive(false);
        SetButtonLabel(pauseButton, "Pause");
    }

    
    void TogglePause()
    {
        paused = !paused;

        if (paused)
        {

            if (AnalyticsManager.I != null)
                AnalyticsManager.I.RegisterPauseClicked();

            Time.timeScale = 0f;
            if (timer) timer.StopTimer();
            SetButtonLabel(pauseButton, "Resume");
            ShowOverlay();
        }
        else
        {
            Time.timeScale = 1f;
            if (timer) timer.ResumeTimer();
            SetButtonLabel(pauseButton, "Pause");
            HideOverlay();
        }
    }

    void ExitToHome()
    {
        Time.timeScale = 1f;
        if (timer) timer.StopTimer();

        var loader = FindObjectOfType<SceneLoader>(true);
        if (loader != null)
        {
            loader.Load("Home");
            return;
        }

        SceneManager.LoadScene("Home", LoadSceneMode.Single);
    }

    
    void TryRebindAll()
    {
        EnsureEventSystem();

        timer = FindObjectOfType<TimerController>(true);

        if (!pauseButton) pauseButton = FindButtonByName(pauseButtonName);
        if (!exitButton)  exitButton  = FindButtonByName(exitButtonName);

        if (!pauseOverlay) pauseOverlay = FindOverlayByName(pauseOverlayName);

        UnwireButtons();
        if (pauseButton) pauseButton.onClick.AddListener(TogglePause);
        if (exitButton)  exitButton.onClick.AddListener(ExitToHome);

        SetButtonLabel(pauseButton, "Pause");
    }

    void UnwireButtons()
    {
        if (pauseButton) pauseButton.onClick.RemoveListener(TogglePause);
        if (exitButton)  exitButton.onClick.RemoveListener(ExitToHome);
    }

    Button FindButtonByName(string targetName)
    {
        if (string.IsNullOrEmpty(targetName)) return null;

        var canvases = GameObject.FindObjectsOfType<Canvas>(true);
        foreach (var canvas in canvases)
        {
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == targetName)
                {
                    var b = t.GetComponent<Button>();
                    if (b) return b;
                }
            }
        }
        return null;
    }

    GameObject FindOverlayByName(string targetName)
    {
        if (string.IsNullOrEmpty(targetName)) return null;

        var canvases = GameObject.FindObjectsOfType<Canvas>(true);
        foreach (var canvas in canvases)
        {
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == targetName)
                    return t.gameObject;
            }
        }
        return null;
    }

    
    void ShowOverlay()
    {
        if (!pauseOverlay) return;

        if (!pauseOverlay.activeSelf) pauseOverlay.SetActive(true);

        var img = pauseOverlay.GetComponent<Image>();
        if (img) img.raycastTarget = false;  

        
        pauseOverlay.transform.SetAsFirstSibling();

        
        if (pauseButton)
        {
            var buttonsRoot = pauseButton.transform.parent;
            if (buttonsRoot) buttonsRoot.SetAsLastSibling();
        }
        if (exitButton && exitButton.transform.parent != null && pauseButton &&
            exitButton.transform.parent != pauseButton.transform.parent)
        {
            exitButton.transform.parent.SetAsLastSibling();
        }
    }

    void HideOverlay()
    {
        if (pauseOverlay && pauseOverlay.activeSelf)
            pauseOverlay.SetActive(false);
    }

    
    void SetButtonLabel(Button b, string text)
    {
        if (!b) return;
        var tmp = b.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp)
        {
            tmp.text = text;
            tmp.enableAutoSizing = false;
            tmp.enableWordWrapping = false;
            return;
        }
        var uText = b.GetComponentInChildren<Text>(true);
        if (uText)
        {
            uText.text = text;
            uText.resizeTextForBestFit = false;
        }
    }

    void EnsureEventSystem()
    {
        if (!FindObjectOfType<EventSystem>())
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }
    }
}
