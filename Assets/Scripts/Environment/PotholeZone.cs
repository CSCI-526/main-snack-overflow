using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PotholeZone : MonoBehaviour
{
    [Min(0f)]
    public float stunDuration = 5f;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerMover mover))
            mover.ApplyStun(stunDuration);
    }
}
