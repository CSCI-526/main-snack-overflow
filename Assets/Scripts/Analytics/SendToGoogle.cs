//using System.Collections;
//using System.Globalization;
//using UnityEngine;
//using UnityEngine.Networking;

///// <summary>
///// Posts analytics rows to a Google Form (or an Apps Script proxy for WebGL/CORS).
///// </summary>
//public class SendToGoogle : MonoBehaviour
//{
//    [Header("URLs")]
//    [Tooltip("Direct Google Form /formResponse endpoint (Editor/Standalone)")]
//    [SerializeField]
//    private string directFormURL =
//        "https://docs.google.com/forms/d/e/1FAIpQLSdQuLNFTidl6zu7ALSA1M7x4P6Hxqbnte1wkA5WUke0Eymimw/viewform";

//    [Tooltip("Apps Script Web App (recommended for WebGL to avoid CORS)")]
//    [SerializeField]
//    private string webglProxyURL = "";  // optional: paste your WebGL relay here

//    [SerializeField, Tooltip("Resolved at runtime based on platform")]
//    private string formURL;

//    [Header("Optional shared secret for your Apps Script relay")]
//    [SerializeField] private string sharedSecret = ""; // only used by your relay; ignored by Forms

//    // ====== YOUR GOOGLE FORM entry.* IDs (from your prefill URL) ======
//    private const string F_session_id = "entry.1988806415";
//    private const string F_record_type = "entry.1230211501";   // "level_attempt" | "session"
//    private const string F_level_id = "entry.655540403";
//    private const string F_attempt_number_in_session = "entry.167729959";
//    private const string F_level_outcome = "entry.1123221003";   // "success" | "fail" | "quit"
//    private const string F_level_duration_ms = "entry.2085091337";

//    private const string F_total_kill_attempts = "entry.1531955326";
//    private const string F_correct_kills = "entry.1301450671";
//    private const string F_wrong_kills = "entry.2066203610";

//    private const string F_avg_speed = "entry.2136275487";
//    private const string F_avg_visibility = "entry.1823490812";
//    private const string F_mean_reaction_latency_ms = "entry.992546699";

//    // Session-level (RMI) – OPTIONAL on level rows
//    private const string F_completion_outcome = "entry.359479690";    // session result
//    private const string F_retry_after_failure = "entry.283852561";    // "true" | "false"
//    private const string F_retry_count_within_5min = "entry.1370343711";

//    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

//    private void Awake()
//    {
//#if UNITY_WEBGL && !UNITY_EDITOR
//        formURL = string.IsNullOrEmpty(webglProxyURL) ? directFormURL : webglProxyURL;
//#else
//        formURL = directFormURL;
//#endif
//    }

//    /// <summary>
//    /// Post one row for a level attempt (this is your main analytics row).
//    /// </summary>
//    public IEnumerator PostLevelAttempt(
//        string sessionId,
//        string levelId,
//        int attemptNumberInSession,
//        string levelOutcome,              // "success" | "fail" | "quit"
//        long levelDurationMs,
//        int totalKillAttempts,
//        int correctKills,
//        int wrongKills,
//        float avgSpeed,
//        float avgVisibility,
//        float meanReactionLatencyMs
//    )
//    {
//        var form = new WWWForm();
//        form.AddField(F_session_id, sessionId);
//        form.AddField(F_record_type, "level_attempt");
//        form.AddField(F_level_id, levelId);
//        form.AddField(F_attempt_number_in_session, attemptNumberInSession.ToString());
//        form.AddField(F_level_outcome, levelOutcome);
//        form.AddField(F_level_duration_ms, levelDurationMs.ToString());

//        form.AddField(F_total_kill_attempts, totalKillAttempts.ToString());
//        form.AddField(F_correct_kills, correctKills.ToString());
//        form.AddField(F_wrong_kills, wrongKills.ToString());

//        form.AddField(F_avg_speed, avgSpeed.ToString(CI));
//        form.AddField(F_avg_visibility, avgVisibility.ToString(CI));
//        form.AddField(F_mean_reaction_latency_ms, meanReactionLatencyMs.ToString(CI));

//        if (!string.IsNullOrEmpty(sharedSecret))
//            form.AddField("secret", sharedSecret); // for your relay only

//        using (var req = UnityWebRequest.Post(formURL, form))
//        {
//            req.timeout = 15;
//            yield return req.SendWebRequest();

//            if (req.result != UnityWebRequest.Result.Success)
//                Debug.LogError($"[Analytics] Level POST failed ({req.responseCode}): {req.error}\nBody: {req.downloadHandler?.text}");
//            else
//                Debug.Log($"[Analytics] Level POST OK ({req.responseCode}).");
//        }
//    }

//    /// <summary>
//    /// Optional: post one row at session end for RMI.
//    /// </summary>
//    public IEnumerator PostSessionSummary(
//        string sessionId,
//        string completionOutcome,          // "success" | "fail" | "quit"
//        bool retryAfterFailure,
//        int retryCountWithin5Min
//    )
//    {
//        var form = new WWWForm();
//        form.AddField(F_session_id, sessionId);
//        form.AddField(F_record_type, "session");
//        form.AddField(F_completion_outcome, completionOutcome);
//        form.AddField(F_retry_after_failure, retryAfterFailure ? "true" : "false");
//        form.AddField(F_retry_count_within_5min, retryCountWithin5Min.ToString());

//        if (!string.IsNullOrEmpty(sharedSecret))
//            form.AddField("secret", sharedSecret); // for your relay only

//        using (var req = UnityWebRequest.Post(formURL, form))
//        {
//            req.timeout = 15;
//            yield return req.SendWebRequest();

//            if (req.result != UnityWebRequest.Result.Success)
//                Debug.LogError($"[Analytics] Session POST failed ({req.responseCode}): {req.error}\nBody: {req.downloadHandler?.text}");
//            else
//                Debug.Log($"[Analytics] Session POST OK ({req.responseCode}).");
//        }
//    }
//}

using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SendToGoogle : MonoBehaviour
{
    // 🔹 Hard-coded Google Form endpoint (NO query params)
    private const string FORM_URL =
        "https://docs.google.com/forms/d/e/1FAIpQLSdQuLNFTidl6zu7ALSA1M7x4P6Hxqbnte1wkA5WUke0Eymimw/formResponse";

    // 🔹 entry.* names from your Snack Overflow prefill URL
    private const string F_session_id = "entry.1988806415";
    private const string F_record_type = "entry.1230211501";
    private const string F_level_id = "entry.655540403";
    private const string F_attempt_number_in_session = "entry.167729959";
    private const string F_level_outcome = "entry.1123221003";
    private const string F_level_duration_ms = "entry.2085091337";

    private const string F_total_kill_attempts = "entry.1531955326";
    private const string F_correct_kills = "entry.1301450671";
    private const string F_wrong_kills = "entry.2066203610";

    private const string F_avg_speed = "entry.2136275487";
    private const string F_avg_visibility = "entry.1823490812";
    private const string F_mean_reaction_latency_ms = "entry.992546699";

    // session-level fields (for later, if you want)
    private const string F_completion_outcome = "entry.359479690";
    private const string F_retry_after_failure = "entry.283852561";
    private const string F_retry_count_within_5min = "entry.1370343711";

    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

    // ========= LEVEL ATTEMPT ROW =========
    public IEnumerator PostLevelAttempt(
        string sessionId,
        string levelId,
        int attemptNumberInSession,
        string levelOutcome,
        long levelDurationMs,
        int totalKillAttempts,
        int correctKills,
        int wrongKills,
        float avgSpeed,
        float avgVisibility,
        float meanReactionLatencyMs
    )
    {
        var sb = new StringBuilder();
        sb.Append(FORM_URL);
        sb.Append("?");

        void Add(string key, string value)
        {
            if (sb[sb.Length - 1] != '?') sb.Append("&");
            sb.Append(UnityWebRequest.EscapeURL(key));
            sb.Append("=");
            sb.Append(UnityWebRequest.EscapeURL(value));
        }

        // same as your prefill test
        Add(F_session_id, sessionId);
        Add(F_record_type, "level_attempt");
        Add(F_level_id, levelId);
        Add(F_attempt_number_in_session, attemptNumberInSession.ToString());
        Add(F_level_outcome, levelOutcome);
        Add(F_level_duration_ms, levelDurationMs.ToString());

        Add(F_total_kill_attempts, totalKillAttempts.ToString());
        Add(F_correct_kills, correctKills.ToString());
        Add(F_wrong_kills, wrongKills.ToString());

        Add(F_avg_speed, avgSpeed.ToString(CI));
        Add(F_avg_visibility, avgVisibility.ToString(CI));
        Add(F_mean_reaction_latency_ms, meanReactionLatencyMs.ToString(CI));

        string url = sb.ToString();
        Debug.Log("[Analytics] GET → " + url);

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 15;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"[Analytics] Level GET failed ({req.responseCode}): {req.error}\nBody: {req.downloadHandler?.text}"
                );
            }
            else
            {
                Debug.Log($"[Analytics] Level GET OK ({req.responseCode}).");
            }
        }
    }

    // ========= OPTIONAL: SESSION SUMMARY =========
    public IEnumerator PostSessionSummary(
        string sessionId,
        string completionOutcome,
        bool retryAfterFailure,
        int retryCountWithin5Min
    )
    {
        var sb = new StringBuilder();
        sb.Append(FORM_URL);
        sb.Append("?");

        void Add(string key, string value)
        {
            if (sb[sb.Length - 1] != '?') sb.Append("&");
            sb.Append(UnityWebRequest.EscapeURL(key));
            sb.Append("=");
            sb.Append(UnityWebRequest.EscapeURL(value));
        }

        Add(F_session_id, sessionId);
        Add(F_record_type, "session");
        Add(F_completion_outcome, completionOutcome);
        Add(F_retry_after_failure, retryAfterFailure ? "true" : "false");
        Add(F_retry_count_within_5min, retryCountWithin5Min.ToString());

        string url = sb.ToString();
        Debug.Log("[Analytics] SESSION GET → " + url);

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 15;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"[Analytics] Session GET failed ({req.responseCode}): {req.error}\nBody: {req.downloadHandler?.text}"
                );
            }
            else
            {
                Debug.Log($"[Analytics] Session GET OK ({req.responseCode}).");
            }
        }
    }
}

