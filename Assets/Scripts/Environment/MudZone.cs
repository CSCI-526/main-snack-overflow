using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class MudZone : MonoBehaviour
{
    readonly HashSet<PlayerMover> playersInside = new();
    [Range(0.1f, 1f)]
    public float speedMultiplier = 0.2f; 

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerMover mover) && playersInside.Add(mover))
        {
            bool slowed = mover.EnterMud(this, speedMultiplier);
            if (slowed && PlayerMover.IsActivePlayer(mover))
                ShowMudMessage();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerMover mover) && playersInside.Remove(mover))
            mover.ExitMud(this);
    }

    void ShowMudMessage()
    {
        HazardHintManager.TryShowMudHint();
    }
}