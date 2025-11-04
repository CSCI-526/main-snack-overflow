using UnityEngine;
using TMPro;

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
                timer.WireButtonToRetry(winPanel, "Exit");
                timer.SetTopLeftButtonsVisible(false);   
            }
        }

        if (AnalyticsManager.I != null)
            AnalyticsManager.I.EndAttemptSuccess();

        ProgressManager.SetLevel1Complete();

    }



    void UpdateUI()
    {
        if (impostorText) impostorText.text = $"Impostors Left: {remaining}";
    }
}
