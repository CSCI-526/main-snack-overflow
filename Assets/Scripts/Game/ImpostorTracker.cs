using System;
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

        int completedLevel = GetCurrentLevelNumber();
        if (completedLevel > 0)
            ProgressManager.MarkLevelComplete(completedLevel);
        else
            ProgressManager.SetLevel1Complete(); // fallback for non-level scenes

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

        if (!exitButton)
            exitButton = FindWinRetryButton();

        if (!exitButton) return;

        string sceneName = SceneManager.GetActiveScene().name;
        bool isLevelTwo = sceneName == "LvL2";
        bool isLevelThree = sceneName == "LvL3";
        bool isLevelFour = sceneName == "LvL4";
        var label = exitButton.GetComponentInChildren<TMP_Text>(true);

        ResetButtonOnClick(exitButton);
        if (isLevelTwo)
            ApplyButtonTextPadding(exitButton, new Vector2(40f, 12f));
        else
            ApplyButtonTextPadding(exitButton, new Vector2(24f, 12f));

        if (isLevelFour)
        {
            if (label) label.text = "Exit";
            CenterButton(exitButton);
            exitButton.onClick.AddListener(ReturnToHome);
        }
        else if (isLevelThree)
        {
            if (label) label.text = "Play Level 4";
            EnsureButtonWidth(exitButton, 320f);
            exitButton.onClick.AddListener(LoadLevelFour);
        }
        else if (isLevelTwo)
        {
            if (label) label.text = "Play Level 3";
            EnsureButtonWidth(exitButton, 320f);
            exitButton.onClick.AddListener(LoadLevelThree);
        }
        else
        {
            if (label) label.text = "Play Level 2";
            exitButton.onClick.AddListener(LoadLevelTwo);
        }

        if (exitButton.targetGraphic) exitButton.targetGraphic.raycastTarget = true;
    }

    Button FindWinRetryButton()
    {
        if (!winPanel)
            return null;

        foreach (var button in winPanel.GetComponentsInChildren<Button>(true))
        {
            if (!button) continue;
            if (button.name == "WinRetry" || button.name == "Retry")
                return button;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            var text = label ? label.text.Trim().ToLowerInvariant() : string.Empty;
            if (text.Contains("retry"))
                return button;
        }

        return null;
    }

    void ResetButtonOnClick(Button button)
    {
        if (!button) return;

        var field = typeof(Button).GetField("m_OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) return;

        var evt = new Button.ButtonClickedEvent();
        field.SetValue(button, evt);
    }

    void ApplyButtonTextPadding(Button button, Vector2 padding)
    {
        if (!button) return;

        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (!label) return;

        var rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding.x, padding.y);
        rect.offsetMax = new Vector2(-padding.x, -padding.y);

        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Truncate;
        label.alignment = TextAlignmentOptions.Midline;
    }

    void CenterButton(Button button)
    {
        if (!button) return;
        if (button.TryGetComponent<RectTransform>(out var rect))
            rect.anchoredPosition = Vector2.zero;
    }

    void EnsureButtonWidth(Button button, float minWidth)
    {
        if (!button) return;
        if (button.TryGetComponent<RectTransform>(out var rect))
        {
            if (rect.sizeDelta.x < minWidth)
                rect.sizeDelta = new Vector2(minWidth, rect.sizeDelta.y);
        }
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

    void LoadLevelThree()
    {
        var loader = FindObjectOfType<SceneLoader>(true);
        if (loader != null)
        {
            loader.LoadLevel3();
            return;
        }

        SceneManager.LoadScene("LvL3");
    }

    void LoadLevelFour()
    {
        var loader = FindObjectOfType<SceneLoader>(true);
        if (loader != null)
        {
            loader.LoadLevel4();
            return;
        }

        SceneManager.LoadScene("LvL4");
    }

    int GetCurrentLevelNumber()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName)) return -1;

        int number = 0;
        bool hasDigit = false;

        foreach (char c in sceneName)
        {
            if (!char.IsDigit(c)) continue;
            hasDigit = true;
            number = (number * 10) + (c - '0');
        }

        return hasDigit ? number : -1;
    }

    void UpdateUI()
    {
        if (impostorText) impostorText.text = $"Impostors Left: {remaining}";
    }
}
