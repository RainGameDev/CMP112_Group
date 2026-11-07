using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Stops walls from hiding the player.
// Our camera looks down from the south-east, so a tall wall between the camera
// and the player would block the view. Every wall, pillar, arch and door piece
// gets this script. Each frame, any piece that's in the way turns invisible
// (but still casts its shadow, so the lighting doesn't change), and it comes
// back as soon as it's not in the way any more.
// The LevelManager calls UpdateAll once every frame.
public class CutawayPiece : MonoBehaviour
{
    // a list of every cutaway piece in the level, shared by all of them (that's what static means)
    static readonly List<CutawayPiece> pieces = new List<CutawayPiece>();

    // we check three lines from the camera: to the player's feet, middle and head
    static readonly float[] heights = { 0.3f, 1f, 1.8f };

    Renderer[] renderers;   // the visible parts of this piece
    Bounds bounds;          // a box around the whole piece
    bool hidden;            // is it hidden right now?

    void OnEnable()
    {
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // work out a box that fits around the piece, made a bit bigger so it
        // hides slightly early rather than slightly late
        bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        bounds.Expand(0.3f);

        pieces.Add(this);
    }

    void OnDisable()
    {
        pieces.Remove(this);
    }

    // Goes through every wall piece and hides the ones between the camera and the player.
    public static void UpdateAll(Vector3 cameraPosition, Vector3 playerPosition)
    {
        foreach (var p in pieces)
        {
            bool block = false;
            foreach (float h in heights)
            {
                // a line (ray) from the camera to this point on the player
                Vector3 target = playerPosition + Vector3.up * h;
                Vector3 dir = target - cameraPosition;
                float dist = dir.magnitude;

                // if the ray goes through the piece's box before it reaches the player,
                // the piece is in the way
                if (p.bounds.IntersectRay(new Ray(cameraPosition, dir / dist), out float hit) && hit < dist - 0.2f)
                {
                    block = true;
                    break;
                }
            }
            p.SetHidden(block);
        }
    }

    // makes every piece visible again (used for the wide camera shot at the overlook)
    public static void ShowAll()
    {
        foreach (var p in pieces) p.SetHidden(false);
    }

    void SetHidden(bool hide)
    {
        // nothing to do if it's already in the right state
        if (hide == hidden) return;
        hidden = hide;

        // "ShadowsOnly" means you can't see the mesh but it still casts a shadow
        foreach (var r in renderers)
            r.shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
    }
}
