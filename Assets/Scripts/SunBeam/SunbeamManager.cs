using UnityEngine;

public class SunbeamManager : MonoBehaviour
{
    public static SunbeamManager Instance { get; private set; }
    public GameObject sunbeamPrefab; // assign the Sunbeam prefab in Inspector

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Smite(NPCDeath npcDeath)
    {
        if (npcDeath == null) return;

        // 1) Check impostor flag from NPCIdentity
        var identity   = npcDeath.GetComponent<NPCIdentity>();
        bool isImpostor = identity != null && identity.isImpostor; // uses your flag:contentReference[oaicite:2]{index=2}

        // 2) Spawn and init beam
        var go   = Instantiate(sunbeamPrefab);
        var beam = go.GetComponent<Sunbeam>();

        // 3) Wrap the "on hit" callback so we can show the popup AND then do the cute death
        void OnBeamHit()
        {
            if (isImpostor)
            {
                // Center-screen popup for ~1–2s (your ImposterKilledText/Popup controller)
                KillTextController.Instance?.Show("Imposter Down!\nEyes Sharp, Feet Faster!");
                // (Optional) score/remaining-impostors bookkeeping can go here too
                // ImpostorTracker.Instance?.OnImpostorKilled();
            }
            else
            {
                // Optional feedback when player taps a civilian
                KillTextController.Instance?.Show("Civilian Down!\nSpeed Drop, Vision Blur!");
            }

            // Finally, do the shrink-and-destroy
            npcDeath.DieCute(); // your existing cute death:contentReference[oaicite:3]{index=3}
        }

        // Pass: target transform, on-hit callback, and impostor flag (for beam color, etc.)
        beam.Init(npcDeath.transform, OnBeamHit, isImpostor);
    }
}
