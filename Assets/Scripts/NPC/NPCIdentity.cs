using UnityEngine;

public class NPCIdentity : MonoBehaviour
{
    public int colorId = -1;                 // stable index into palette
    public bool isImpostor = false;
    public PathShape.ShapeType shapeType;    // the path shape they follow (or None if wanderer)

    public void ApplyColor(NPCColorPalette palette, Renderer[] renderers)
    {
        if (!palette || colorId < 0) return;
        var c = palette.GetForRole(colorId, isImpostor);
        foreach (var r in renderers)
        {
            if (!r) continue;
            // Instance material or use MPB later
            if (r.material != null)
            {
                r.material = new Material(r.material);
                r.material.color = c;
            }
        }
    }
}
