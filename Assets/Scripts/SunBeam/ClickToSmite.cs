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

using UnityEngine;
using UnityEngine.EventSystems;

public class ClickToSmite : MonoBehaviour
{
    public LayerMask npcLayer; // set to NPC layer in Inspector

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
                var grs = GameRoundState.Instance;

                // If no ID, treat as wrong civilian
                if (id == null)
                {
                    HandleWrong(death);
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
                }
                else
                {
                    HandleWrong(death);
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
            AnalyticsManager.I.OnCorrectHit();

        // Notify that an impostor was killed
        if (id != null && id.isImpostor)
            ImpostorTracker.Instance?.OnImpostorKilled();

        // Beam + delete NPC
        SunbeamManager.Instance.Smite(death);
    }

    void HandleWrong(NPCDeath death)
    {
        PreventDoubleHit(death);

        TrackShotFired();

        // Optional: if you want to track total "wrong hits" separately
        // you can add this too (your choice):
        // if (AnalyticsManager.I != null)
        //     AnalyticsManager.I.OnWrongHit();

        // Beam + delete NPC
        SunbeamManager.Instance.Smite(death);

        // Lose a life on wrong hit
        if (LivesManager.Instance != null)
            LivesManager.Instance.LoseLife();
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
}
