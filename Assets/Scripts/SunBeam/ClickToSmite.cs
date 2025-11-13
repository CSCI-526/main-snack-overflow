//using UnityEngine;
//using UnityEngine.EventSystems;

//public class ClickToSmite : MonoBehaviour
//{
//    public LayerMask npcLayer; // set to NPC layer in Inspector

//    void Update()
//    {
//        if (Input.GetMouseButtonDown(0))
//        {
//            // Ignore UI clicks
//            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
//                return;

//            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
//            if (Physics.Raycast(ray, out RaycastHit hit, 200f, npcLayer))
//            {
//                var death = hit.collider.GetComponentInParent<NPCDeath>();
//                if (death == null) return;

//                var id = hit.collider.GetComponentInParent<NPCIdentity>();
//                var grs = GameRoundState.Instance;

//                // If no ID, treat as wrong civilian
//                if (id == null)
//                {
//                    HandleWrong(death);
//                    return;
//                }

//                bool correct = (grs != null) && grs.MatchesAllowed(id.shapeType, id.colorId);

//                if (correct)
//                {
//                    HandleCorrect(death, id);
//                }
//                else
//                {
//                    HandleWrong(death);
//                }
//            }
//        }
//    }

//    void HandleCorrect(NPCDeath death, NPCIdentity id)
//    {
//        // Disable colliders immediately so we don't double count
//        PreventDoubleHit(death);

//        // Notify that an impostor was killed
//        if (id != null && id.isImpostor)
//            ImpostorTracker.Instance?.OnImpostorKilled();

//        // Beam + delete NPC
//        SunbeamManager.Instance.Smite(death);
//    }

//    void HandleWrong(NPCDeath death)
//    {
//        PreventDoubleHit(death);

//        // Beam + delete NPC
//        SunbeamManager.Instance.Smite(death);

//        // Lose a life on wrong hit
//        if (LivesManager.Instance != null)
//            LivesManager.Instance.LoseLife();
//    }

//    void PreventDoubleHit(NPCDeath death)
//    {
//        var cols = death.GetComponentsInChildren<Collider>(false);
//        for (int i = 0; i < cols.Length; i++)
//            cols[i].enabled = false;
//    }
//}

using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickToSmite : MonoBehaviour
{
    public LayerMask npcLayer; // set to NPC layer in Inspector

    // Reference to VisionMaskController
    public VisionMaskController visionMaskController;

    /// <summary>
    /// Optional hook that can veto hits (e.g. tutorial gating). Return true to allow the shot.
    /// </summary>
    public static Func<NPCIdentity, bool> HitFilter { get; set; }

    /// <summary>
    /// When true, skips gameplay side-effects (lives, win panels). Used for tutorials.
    /// </summary>
    public static bool SuppressGameState { get; set; }

    /// <summary>
    /// Fired after a shot resolves. Bool indicates whether the hit was correct.
    /// </summary>
    public static event Action<NPCIdentity, bool> OnHitResolved;

    void Start()
    {
        if (visionMaskController == null)
            visionMaskController = VisionMaskController.Instance;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Ignore UI clicks
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 200f, npcLayer))
            {
                var death = hit.collider.GetComponentInParent<NPCDeath>();
                if (death == null) return;

                var id = hit.collider.GetComponentInParent<NPCIdentity>();

                if (HitFilter != null && !HitFilter.Invoke(id))
                    return;

                var grs = GameRoundState.Instance;

                // If no ID, treat as wrong civilian
                if (id == null)
                {
                    HandleWrong(death, null);
                    return;
                }

                bool correct = false;
                if (grs != null)
                    correct = grs.IsImpostorColor(id.colorId);
                else
                    correct = id.isImpostor;

                if (correct)
                {
                    id.isImpostor = true;
                    HandleCorrect(death, id);
                    OnHitResolved?.Invoke(id, true);
                }
                else
                {
                    HandleWrong(death, id);
                }
            }
        }
    }

    void HandleCorrect(NPCDeath death, NPCIdentity id)
    {
        // Disable colliders immediately so we don't double count
        PreventDoubleHit(death);

        TrackShotFired();

        // ✅ Track correct hit
        if (AnalyticsManager.I != null)
        {
            AnalyticsManager.I.OnFirstTargetingAction();
            AnalyticsManager.I.OnCorrectHit();
        }


        // Notify that an impostor was killed
        if (!SuppressGameState && id != null && id.isImpostor)
            ImpostorTracker.Instance?.OnImpostorKilled();

        // Beam + delete NPC
        SunbeamManager.Instance.Smite(death);

        AdjustVision(true);
    }

    void HandleWrong(NPCDeath death, NPCIdentity id)
    {
        PreventDoubleHit(death);

        TrackShotFired();
        if (AnalyticsManager.I != null)
            AnalyticsManager.I.OnFirstTargetingAction();

        // Optional: if you want to track total "wrong hits" separately
        // if (AnalyticsManager.I != null)
        //     AnalyticsManager.I.OnWrongHit();

        // Beam + delete NPC
        SunbeamManager.Instance.Smite(death);

        // Lose a life on wrong hit
        if (!SuppressGameState && LivesManager.Instance != null)
            LivesManager.Instance.LoseLife();

        AdjustVision(false);

        OnHitResolved?.Invoke(id, false);
    }


    void PreventDoubleHit(NPCDeath death)
    {
        var cols = death.GetComponentsInChildren<Collider>(false);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = false;
    }

    void TrackShotFired()
    {
        if (AnalyticsManager.I != null)
            AnalyticsManager.I.OnShotFired();
    }

    void AdjustVision(bool increase)
    {
        if (visionMaskController == null)
        {
            Debug.LogWarning("VisionMaskController is not assigned in ClickToSmite.");
            return;
        }

        float delta = increase ? 0.05f : -0.05f;
        float target = visionMaskController.currentRadius + delta;
        visionMaskController.UpdateRadius(target);

        bool tutorialHandling = Level1TutorialController.Instance != null && Level1TutorialController.Instance.IsTutorialActive;
        if (!tutorialHandling)
            ShowVisionPrompt(increase, visionMaskController.currentRadius);
    }

    void ShowVisionPrompt(bool increase, float radius)
    {
        if (TutorialVisionHint.Instance == null || visionMaskController == null)
            return;
        var maskRect = visionMaskController.MaskRect;
        if (maskRect == null) return;
        TutorialVisionHint.Instance.Show(increase, maskRect, radius);
    }
}
