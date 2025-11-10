using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LightCollectible : MonoBehaviour
{
    public float lightReward = 1f;
    public bool destroyOnPickup = true;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PlayerMover>();
        if (!player) return;

        if (PlayerLightController.Instance != null)
            PlayerLightController.Instance.HandleLightOrbPickup(lightReward);

        if (destroyOnPickup)
            Destroy(gameObject);
    }
}
