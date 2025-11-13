using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LightCollectible : MonoBehaviour
{
    public float lightReward = 1f;
    public bool destroyOnPickup = true;
    [Header("Magnet")]
    public float magnetActivationRadius = 1.6f;
    public float magnetMoveSpeed = 6f;
    public float magnetSnapDistance = 0.25f;

    bool collected;
    bool magnetActive;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void Update()
    {
        if (collected || magnetActivationRadius <= 0f || magnetMoveSpeed <= 0f)
            return;

        var player = PlayerMover.Active;
        if (!PlayerMover.IsActivePlayer(player))
        {
            SetMagnetState(false);
            return;
        }

        Vector3 targetPos = player.transform.position;
        float activationRadius = magnetActivationRadius;
        float sqrDist = (targetPos - transform.position).sqrMagnitude;
        if (sqrDist > activationRadius * activationRadius)
        {
            SetMagnetState(false);
            return;
        }

        SetMagnetState(true);

        float step = magnetMoveSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

        float snapRadius = Mathf.Max(0.01f, magnetSnapDistance);
        if ((targetPos - transform.position).sqrMagnitude <= snapRadius * snapRadius)
            TryCollect(player);
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PlayerMover>();
        TryCollect(player);
    }

    void TryCollect(PlayerMover player)
    {
        if (collected || !PlayerMover.IsActivePlayer(player))
            return;
        collected = true;
        SetMagnetState(false);
        if (PlayerLightController.Instance != null)
            PlayerLightController.Instance.AddLightEnergy(lightReward);

        if (destroyOnPickup)
            Destroy(gameObject);
    }

    void SetMagnetState(bool active)
    {
        magnetActive = active;
    }

    public bool MagnetActive => magnetActive;
}

