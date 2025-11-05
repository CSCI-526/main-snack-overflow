using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MudZone : MonoBehaviour
{
    [Range(0.1f, 1f)]
    public float speedMultiplier = 0.2f; 

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerMover mover))
            mover.EnterMud(this, speedMultiplier);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerMover mover))
            mover.ExitMud(this);
    }
}