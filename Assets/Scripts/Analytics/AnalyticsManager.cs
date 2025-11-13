//using System;
//using System.Collections;
//using UnityEngine;
//using UnityEngine.SceneManagement;

///// <summary>
///// Tracks per-attempt analytics metrics and posts one row to Google Forms
///// (or an Apps Script relay) through SendToGoogle.cs.
///// Attach this to a persistent GameObject in your first scene.
///// </summary>
//[DefaultExecutionOrder(-1000)]
//public class AnalyticsManager : MonoBehaviour
//{
//    public static AnalyticsManager I { get; private set; }

//    [Header("Sender (auto-assigned if missing)")]
//    [SerializeField] private SendToGoogle sender;

//    // === Metrics collected per game attempt ===
//    private string sessionId;         // unique per player runtime
//    private string levelId;           // current Scene name
//    private int shotsFired;           // number of player shots
//    private int correctHits;          // number of correct hits
//    private float timeTakenSec;       // time taken in this attempt
//    private int completed;            // 1 = success, 0 = fail
//    private int pauseClicks;          // number of pause button presses

//    // Internals
//    private float attemptStartTime;
//    private bool attemptRunning;

//    private void Awake()
//    {
//        // Singleton
//        if (I != null && I != this) { Destroy(gameObject); return; }
//        I = this;
//        DontDestroyOnLoad(gameObject);

//        sessionId = Guid.NewGuid().ToString("N");
//        levelId = SceneManager.GetActiveScene().name;

//        // Ensure sender
//        if (sender == null)
//            sender = GetComponent<SendToGoogle>() ?? gameObject.AddComponent<SendToGoogle>();

//        // Keep levelId in sync if you add more scenes later
//        SceneManager.activeSceneChanged += (_, newScene) => levelId = newScene.name;
//    }

//    // -------- Public hooks --------

//    /// <summary>Call when clues close and actual gameplay starts.</summary>
//    public void OnAttemptStart()
//    {
//        shotsFired = 0;
//        correctHits = 0;
//        timeTakenSec = 0f;
//        completed = 0;
//        // If you want pause clicks per attempt (not per session), uncomment:
//        // pauseClicks = 0;

//        levelId = SceneManager.GetActiveScene().name;
//        attemptStartTime = Time.time;
//        attemptRunning = true;
//        Debug.Log("[Analytics] Attempt started for " + levelId);
//    }

//    /// <summary>Call every time the player fires.</summary>
//    public void OnShotFired()
//    {
//        if (!attemptRunning) return;
//        shotsFired++;
//    }

//    /// <summary>Call when a correct impostor is hit.</summary>
//    public void OnCorrectHit()
//    {
//        if (!attemptRunning) return;
//        correctHits++;
//    }

//    /// <summary>Call when Pause is clicked.</summary>
//    public void RegisterPauseClicked()
//    {
//        pauseClicks++;
//    }
//    public void OnPauseClicked() => RegisterPauseClicked();

//    /// <summary>Call on win.</summary>
//    public void EndAttemptSuccess()
//    {
//        if (!attemptRunning) return;
//        timeTakenSec = Time.time - attemptStartTime;
//        completed = 1;
//        attemptRunning = false;
//        StartCoroutine(SendRow());
//    }

//    /// <summary>Call on fail/timeout.</summary>
//    public void EndAttemptFail()
//    {
//        if (!attemptRunning) return;
//        timeTakenSec = Time.time - attemptStartTime;
//        completed = 0;
//        attemptRunning = false;
//        StartCoroutine(SendRow());
//    }

//    // -------- Sender --------
//    private IEnumerator SendRow()
//    {
//        float accuracyPercent = (shotsFired > 0)
//            ? (correctHits / (float)shotsFired) * 100f
//            : 0f;

//        Debug.Log($"[Analytics] Sending row → session={sessionId}, level={levelId}, " +
//                  $"shots={shotsFired}, correct={correctHits}, acc={accuracyPercent:0.##}%, " +
//                  $"time={timeTakenSec:0.##}s, completed={completed}, pauses={pauseClicks}");

//        yield return sender.SendAttemptRow(
//            sessionId: sessionId,
//            levelId: levelId,
//            shotsFired: shotsFired,
//            correctHits: correctHits,
//            accuracyPercent: accuracyPercent,
//            timeTakenSec: timeTakenSec,
//            completed: completed,
//            pauseClicks: pauseClicks
//        );
//    }
//}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tracks per-level-attempt metrics (ASR, RL, DAR, RMI) and posts rows to Google Forms
/// through SendToGoogle.
/// Attach this to a persistent GameObject in your first scene.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager I { get; private set; }

    [Header("Sender (auto-assigned if missing)")]
    [SerializeField] private SendToGoogle sender;

    [Header("Attempt sequencing")]
    [SerializeField] private int attemptNumberInSession = 0;

    // ===== IDs & timing =====
    private string sessionId;
    private string currentLevelId;
    private float attemptStartTime;
    private bool attemptRunning;

    // ===== DAR counters =====
    private int totalKillAttempts;
    private int correctKills;
    private int wrongKills;

    // ===== ASR sampling =====
    private float speedSum;
    private int speedSamples;
    private float visibilitySum;
    private int visibilitySamples;

    // ===== RL (reaction latency) =====
    private readonly List<float> reactionLatenciesMs = new();
    private double lastImpostorIdentifiableTime = -1; // Time.realtimeSinceStartup

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        sessionId = Guid.NewGuid().ToString("N");
        currentLevelId = SceneManager.GetActiveScene().name;

        // auto-assign sender so you don't have to drag it in the Inspector
        if (sender == null)
            sender = GetComponent<SendToGoogle>() ?? gameObject.AddComponent<SendToGoogle>();

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        currentLevelId = newScene.name;
    }

    // ================= PUBLIC HOOKS =================

    /// <summary>
    /// Call when the player gains control and the true level attempt starts.
    /// </summary>
    public void StartLevelAttempt()
    {
        attemptRunning = true;
        attemptStartTime = Time.time;
        attemptNumberInSession++;

        totalKillAttempts = 0;
        correctKills = 0;
        wrongKills = 0;

        speedSum = 0f; speedSamples = 0;
        visibilitySum = 0f; visibilitySamples = 0;

        reactionLatenciesMs.Clear();
        lastImpostorIdentifiableTime = -1;

        currentLevelId = SceneManager.GetActiveScene().name;

        Debug.Log($"[Analytics] Start attempt #{attemptNumberInSession} on {currentLevelId}, session={sessionId}");
    }

    // Backwards-compat for old name, in case something still calls it
    public void OnAttemptStart() => StartLevelAttempt();

    /// <summary>
    /// Call when the level attempt ends (success/fail/quit).
    /// Triggers a POST of one level row to Google Forms.
    /// </summary>
    public void EndLevelAttempt(string levelOutcome)
    {
        if (!attemptRunning) return;
        attemptRunning = false;

        long levelDurationMs = (long)((Time.time - attemptStartTime) * 1000f);

        // recompute wrongKills as a safety
        int computedWrong = Mathf.Max(0, totalKillAttempts - correctKills);
        if (wrongKills < computedWrong)
            wrongKills = computedWrong;

        float avgSpeed = speedSamples > 0 ? (speedSum / speedSamples) : 0f;
        float avgVisibility = visibilitySamples > 0 ? (visibilitySum / visibilitySamples) : 0f;
        float meanRL = reactionLatenciesMs.Count > 0 ? Mean(reactionLatenciesMs) : 0f;

        Debug.Log(
            $"[Analytics] End attempt → level={currentLevelId}, outcome={levelOutcome}, " +
            $"kills {correctKills}/{totalKillAttempts}, wrong={wrongKills}, " +
            $"avgSpeed={avgSpeed:0.###}, avgVis={avgVisibility:0.###}, RL={meanRL:0.##}ms, " +
            $"dur={levelDurationMs}ms"
        );

        if (sender != null)
        {
            StartCoroutine(sender.PostLevelAttempt(
                sessionId: sessionId,
                levelId: currentLevelId,
                attemptNumberInSession: attemptNumberInSession,
                levelOutcome: levelOutcome,               // "success" | "fail" | "quit"
                levelDurationMs: levelDurationMs,
                totalKillAttempts: totalKillAttempts,
                correctKills: correctKills,
                wrongKills: wrongKills,
                avgSpeed: avgSpeed,
                avgVisibility: avgVisibility,
                meanReactionLatencyMs: meanRL
            ));
        }
        else
        {
            Debug.LogWarning("[Analytics] No SendToGoogle sender assigned.");
        }
    }

    /// <summary>Wrapper for success.</summary>
    public void EndAttemptSuccess() => EndLevelAttempt("success");

    /// <summary>Wrapper for fail/timeout.</summary>
    public void EndAttemptFail() => EndLevelAttempt("fail");

    /// <summary>
    /// Optional hook if you want to count pauses (for RMI/engagement later).
    /// Currently just logs.
    /// </summary>
    public void OnPauseClicked()
    {
        if (!attemptRunning) return;
        Debug.Log("[Analytics] Pause clicked.");
        // You can add a counter here later if you want.
    }

    // ===== DAR hooks =====

    /// <summary>Call on every kill attempt / click.</summary>
    public void OnShotFired()
    {
        if (!attemptRunning) return;
        totalKillAttempts++;
    }

    /// <summary>Call on each correct impostor kill.</summary>
    public void OnCorrectHit()
    {
        if (!attemptRunning) return;
        correctKills++;
    }

    /// <summary>Optional explicit wrong-hit hook.</summary>
    public void OnWrongHit()
    {
        if (!attemptRunning) return;
        wrongKills++;
    }

    /// <summary>Alternative explicit API.</summary>
    public void RegisterKillAttempt(bool wasCorrect)
    {
        if (!attemptRunning) return;
        totalKillAttempts++;
        if (wasCorrect) correctKills++;
        else wrongKills++;
    }

    // ===== ASR hooks =====

    public void SampleSpeed(float currentSpeed)
    {
        if (!attemptRunning) return;
        speedSum += currentSpeed;
        speedSamples++;
    }

    public void SampleVisibility(float currentVisibility)
    {
        if (!attemptRunning) return;
        visibilitySum += currentVisibility;
        visibilitySamples++;
    }

    // ===== RL hooks =====

    /// <summary>
    /// Called when an impostor becomes validly identifiable.
    /// </summary>
    public void OnImpostorIdentifiable()
    {
        if (!attemptRunning) return;
        lastImpostorIdentifiableTime = Time.realtimeSinceStartupAsDouble;
    }

    /// <summary>
    /// Called on first targeting action for that impostor.
    /// </summary>
    public void OnFirstTargetingAction()
    {
        if (!attemptRunning) return;
        if (lastImpostorIdentifiableTime < 0) return;

        double now = Time.realtimeSinceStartupAsDouble;
        float ms = (float)((now - lastImpostorIdentifiableTime) * 1000.0);
        reactionLatenciesMs.Add(ms);
        lastImpostorIdentifiableTime = -1;
    }

    // ===== Optional session summary for RMI (you can call this later) =====

    public void PostSessionSummary(string completionOutcome, bool retriedAfterFailure, int retryCountWithin5Min)
    {
        if (sender == null)
        {
            Debug.LogWarning("[Analytics] No sender for PostSessionSummary.");
            return;
        }

        StartCoroutine(sender.PostSessionSummary(
            sessionId: sessionId,
            completionOutcome: completionOutcome,
            retryAfterFailure: retriedAfterFailure,
            retryCountWithin5Min: retryCountWithin5Min
        ));
    }

    // ================= helpers =================

    private static float Mean(List<float> xs)
    {
        if (xs == null || xs.Count == 0) return 0f;
        float s = 0f;
        for (int i = 0; i < xs.Count; i++) s += xs[i];
        return s / xs.Count;
    }
}
