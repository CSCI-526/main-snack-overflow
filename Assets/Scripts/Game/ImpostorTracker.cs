using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Reflection;

public class ImpostorTracker : MonoBehaviour
{
    public static ImpostorTracker Instance;

    [Header("Behavior")]
    public bool pauseOnWin = true;
    public bool disableClicksOnWin = true;
    bool winSequenceStarted = false;

    
    public float winDelayAfterLastHit = 0.82f;

    [Header("UI")]
    public TMP_Text impostorText;     
    public GameObject winPanel;       

    
    public int TotalSpawned { get; private set; } = 0;
    public int Killed       { get; private set; } = 0;
    public int Remaining    => remaining;

    int remaining = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (winPanel) winPanel.SetActive(false);
        UpdateUI();
    }

    
    public void ResetCount()
    {
        remaining = 0;
        TotalSpawned = 0;   
        Killed = 0;         
        UpdateUI();
        if (winPanel) winPanel.SetActive(false);
        winSequenceStarted = false;
    }

    
    public void RegisterImpostor()
    {
        remaining++;
        TotalSpawned++;     
        UpdateUI();
    }

    
    public void OnImpostorKilled()
    {
        if (remaining <= 0) return; 
        remaining--;
        Killed++;                    
        UpdateUI();

        if (remaining <= 0 && !winSequenceStarted)
        {
            winSequenceStarted = true;
            StartCoroutine(WinAfterBeam());
        }
    }

   System.Collections.IEnumerator WinAfterBeam()
    {
        yield return new WaitForSecondsRealtime(winDelayAfterLastHit);

        var timer = FindObjectOfType<TimerController>(true);
        if (timer) timer.StopTimer();

        if (pauseOnWin) Time.timeScale = 0f;

        if (disableClicksOnWin)
        {
            var click = FindObjectOfType<ClickToSmite>(true);
            if (click) click.enabled = false;
        }

        if (winPanel)
        {
            winPanel.SetActive(true);

            if (timer)
            {
                timer.PreparePanelForClicks(winPanel);
                timer.SetTopLeftButtonsVisible(false);   
            }

            ConfigureWinPanelExitButton();
        }

        if (AnalyticsManager.I != null)
            AnalyticsManager.I.EndAttemptSuccess();

        ProgressManager.SetLevel1Complete();

    }



    void ConfigureWinPanelExitButton()
    {
        if (!winPanel) return;

        Button exitButton = null;

        foreach (var button in winPanel.GetComponentsInChildren<Button>(true))
        {
            if (button.name == "Exit")
            {
                exitButton = button;
                break;
            }
        }

        if (!exitButton)
        {
            foreach (var tmp in winPanel.GetComponentsInChildren<TMP_Text>(true))
            {
                var txt = tmp.text.Trim().ToLower();
                if (txt.Contains("exit"))
                {
                    exitButton = tmp.GetComponentInParent<Button>(true);
                    if (exitButton) break;
                }
            }
        }

        if (!exitButton) return;

        bool isLevelTwo = SceneManager.GetActiveScene().name == "LvL2";
        var label = exitButton.GetComponentInChildren<TMP_Text>(true);
        if (label) label.text = isLevelTwo ? "Exit" : "Play Level 2";

        if (exitButton.targetGraphic) exitButton.targetGraphic.raycastTarget = true;
        ResetButtonOnClick(exitButton);
        if (isLevelTwo)
            exitButton.onClick.AddListener(ReturnToHome);
        else
            exitButton.onClick.AddListener(LoadLevelTwo);
    }


    void ResetButtonOnClick(Button button)
    {
        if (!button) return;

        var field = typeof(Button).GetField("m_OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) return;

        var evt = new Button.ButtonClickedEvent();
        field.SetValue(button, evt);
    }


    void ReturnToHome()
    {
        var loader = FindObjectOfType<SceneLoader>(true);
        if (loader != null)
        {
            loader.Load("Home");
            return;
        }

        SceneManager.LoadScene("Home");
    }


    void LoadLevelTwo()
    {
        var loader = FindObjectOfType<SceneLoader>(true);
        if (loader != null)
        {
            loader.LoadLevel2();
            return;
        }

        if (ProgressManager.IsLevelUnlocked(2))
            SceneManager.LoadScene("LvL2");
        else
            Debug.LogWarning("Level 2 requested before it was unlocked.");
    }


    void UpdateUI()
    {
        if (impostorText) impostorText.text = $"Impostors Left: {remaining}";
    }
}
