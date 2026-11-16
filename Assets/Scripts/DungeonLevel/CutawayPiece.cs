using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CutawayPiece : MonoBehaviour
{
    static List<CutawayPiece> allPieces = new();

    Renderer[] renderers;
    Bounds box;
    bool hidden;

    void OnEnable()
    {
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        box = renderers[0].bounds;
        foreach (Renderer r in renderers) box.Encapsulate(r.bounds);
        box.Expand(0.3f);

        allPieces.Add(this);
    }

    void OnDisable()
    {
        allPieces.Remove(this);
    }

    public static void UpdateAll(Vector3 cameraPos, Vector3 playerPos)
    {
        foreach (CutawayPiece piece in allPieces)
        {
            bool inTheWay = piece.Blocks(cameraPos, playerPos + Vector3.up * 0.3f)
                         || piece.Blocks(cameraPos, playerPos + Vector3.up * 1f)
                         || piece.Blocks(cameraPos, playerPos + Vector3.up * 1.8f);
            piece.SetHidden(inTheWay);
        }
    }

    public static void ShowAll()
    {
        foreach (CutawayPiece piece in allPieces) piece.SetHidden(false);
    }

    bool Blocks(Vector3 cameraPos, Vector3 point)
    {
        Vector3 dir = point - cameraPos;
        float distance = dir.magnitude;
        Ray ray = new Ray(cameraPos, dir / distance);
        return box.IntersectRay(ray, out float hitDistance) && hitDistance < distance - 0.2f;
    }

    void SetHidden(bool hide)
    {
        if (hide == hidden) return;
        hidden = hide;

        foreach (Renderer r in renderers)
            r.shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
    }
}
