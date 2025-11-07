using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class PotholeZone : MonoBehaviour
{
    readonly HashSet<PlayerMover> playersInside = new();
    [Min(0f)]
    public float stunDuration = 5f;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerMover mover) && playersInside.Add(mover))
        {
            bool stunned = mover.ApplyStun(stunDuration);
            if (stunned && PlayerMover.IsActivePlayer(mover))
                ShowPotholeMessage();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerMover mover))
            playersInside.Remove(mover);
    }


    void ShowPotholeMessage()
    {
        HazardHintManager.TryShowPotholeHint(stunDuration);
    }
}
