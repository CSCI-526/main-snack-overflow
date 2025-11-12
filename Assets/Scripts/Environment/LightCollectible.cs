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
        if (!PlayerMover.IsActivePlayer(player))
            return;

        if (PlayerLightController.Instance != null)
            PlayerLightController.Instance.AddLightEnergy(lightReward);

        if (destroyOnPickup)
            Destroy(gameObject);
    }
}
