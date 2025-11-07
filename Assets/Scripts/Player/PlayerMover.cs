using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMover : MonoBehaviour
{
    public static PlayerMover Active { get; private set; }

    public float moveSpeed = 6f;
    public float accel = 20f;
    public float groundY = 0.1f;

    [Range(0.1f, 2f)]
    public float mudMultiplierFallback = 1f;   // lets you tweak defaults in the inspector

    [Header("Kill-Based Speed")]
[Range(0.02f, 1f)] public float hitSpeedStep = 0.25f;
[Min(0f)] public float minHitSpeedMultiplier = 1f;
[Min(0f)] public float maxHitSpeedMultiplier = 6f;

[Header("Debug")]
public bool logSpeed = false;
[Range(0.05f, 1f)] public float logInterval = 0.25f;

    // Track each mud zone currently touching the player so overlapping slows stack correctly.
    readonly Dictionary<MudZone, float> mudContacts = new();
    float activeMudMultiplier = 1f;
    float stunUntil;
    float hitSpeedMultiplier = 1f;
    float logTimer;

    Rigidbody rb;
    Camera cam;
    Vector3 desiredVel;

    void Awake()
    {
        hitSpeedMultiplier = 1f;
        activeMudMultiplier = mudMultiplierFallback;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        cam = Camera.main;
    }

    void OnEnable()
    {
        ClickToSmite.OnHitResolved += HandleHitResolved;
        Active = this;
    }

    void OnDisable()
    {
        ClickToSmite.OnHitResolved -= HandleHitResolved;
        if (Active == this)
            Active = null;
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;

        // Camera-aligned XZ axes
        Vector3 fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 right = cam.transform.right; right.y = 0f; right.Normalize();

        // ---- Direct key input (WASD + Arrow keys), no analog drift ----
        float h = 0f, v = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  h -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    v += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  v -= 1f;

        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 0f) input.Normalize();

        desiredVel = input * moveSpeed * hitSpeedMultiplier * activeMudMultiplier;
        if (IsStunned())
            desiredVel = Vector3.zero;

        // Pin to ground height
        var p = transform.position;
        if (Mathf.Abs(p.y - groundY) > 0.0001f)
            transform.position = new Vector3(p.x, groundY, p.z);

        
            logTimer += Time.deltaTime;
            if (logTimer >= logInterval)
            {
                logTimer = 0f;
                Vector3 planar = rb.velocity; planar.y = 0f;
                Debug.Log($"[PlayerMover] speed={planar.magnitude:F2} | multiplier={hitSpeedMultiplier:F2}");
            }
        
    }

    void FixedUpdate()
    {
        if (IsStunned())
        {
            rb.velocity = Vector3.zero;
            return;
        }

        // Smooth toward desired velocity; snap to 0 when no input to avoid drift
        Vector3 vel = rb.velocity; vel.y = 0f;
        Vector3 target = desiredVel;

        // if no input, decelerate hard to a full stop
        if (target == Vector3.zero && vel.sqrMagnitude < 0.0001f)
            rb.velocity = Vector3.zero;
        else
            rb.velocity = Vector3.MoveTowards(vel, target, accel * Time.fixedDeltaTime);
    }

    public bool EnterMud(MudZone zone, float multiplier)
    {
        float before = activeMudMultiplier;
        mudContacts[zone] = Mathf.Clamp(multiplier, 0.1f, 1f);
        RefreshMudMultiplier();
        return IsSlowed(activeMudMultiplier) && !IsSlowed(before);
    }

    public void ExitMud(MudZone zone)
    {
        if (mudContacts.Remove(zone))
            RefreshMudMultiplier();
    }

    bool IsSlowed(float value)
    {
        const float eps = 0.001f;
        float baseline = Mathf.Max(0.1f, mudMultiplierFallback);
        return value < baseline - eps;
    }

    public static bool IsActivePlayer(PlayerMover mover)
        => mover != null && Active == mover && mover.isActiveAndEnabled;

    void RefreshMudMultiplier()
    {
        if (mudContacts.Count == 0)
        {
            activeMudMultiplier = mudMultiplierFallback;
            return;
        }

        float slowest = 1f;
        foreach (var kvp in mudContacts)
            slowest = Mathf.Min(slowest, kvp.Value);

        activeMudMultiplier = slowest;
    }

    public bool ApplyStun(float duration)
    {
        float clamped = Mathf.Max(0f, duration);
        float newUntil = Mathf.Max(stunUntil, Time.time + clamped);
        bool applied = newUntil > stunUntil && clamped > 0f;
        stunUntil = newUntil;
        desiredVel = Vector3.zero;
        if (rb != null)
            rb.velocity = Vector3.zero;
        return applied;
    }

    void HandleHitResolved(NPCIdentity identity, bool correct)
    {
        if (ClickToSmite.SuppressGameState)
            return;

        float delta = correct ? hitSpeedStep : -hitSpeedStep;
        hitSpeedMultiplier = Mathf.Clamp(hitSpeedMultiplier + delta, minHitSpeedMultiplier, maxHitSpeedMultiplier);
    }

    void OnValidate()
    {
        maxHitSpeedMultiplier = Mathf.Max(maxHitSpeedMultiplier, minHitSpeedMultiplier);
    }

    bool IsStunned() => Time.time < stunUntil;
}
