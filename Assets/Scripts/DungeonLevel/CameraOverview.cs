using UnityEngine;

// This goes on the invisible barrier at the edge of the Well Overlook.
// When the player walks up and presses against the barrier, the camera
// pulls back to show the whole level from above (like a "look out" point).
// When they step away, the camera goes back to normal after a short moment.
[RequireComponent(typeof(Collider))]
public class CameraOverview : MonoBehaviour
{
    public Transform focus;        // the point the wide shot looks at (the middle of the level)
    public float distance = 42f;   // how far back the camera moves from that point
    public float linger = 1f;      // how many seconds the wide shot stays after you stop touching the barrier
    public float contactMargin = 0.15f;   // how close (in metres) the player has to be to count as touching

    Collider barrier;                                 // the barrier's own collider
    float lastTouch = float.NegativeInfinity;         // the last time the player was touching it
    bool active;                                      // is the wide shot on right now?

    void Awake()
    {
        barrier = GetComponent<Collider>();
    }

    void Update()
    {
        var lm = LevelManager.Instance;
        if (!lm || !focus) return;

        // Take the barrier's box and make it slightly bigger. If the player's body
        // is inside that bigger box, they're pressed up against the barrier.
        // (We check this every frame instead of using collision events, because
        // collision events don't keep firing while the player stands still against a wall.)
        Bounds b = barrier.bounds;
        b.Expand(contactMargin * 2f);
        if (lm.PlayerTouching(b)) lastTouch = Time.time;

        // still counts as touching for "linger" seconds afterwards,
        // so the camera doesn't flick in and out if you brush along the edge
        bool touching = Time.time - lastTouch < linger;

        // only tell the level manager when it changes, not every frame
        if (touching && !active) lm.SetOverview(focus.position, distance);
        else if (!touching && active) lm.ClearOverview();
        active = touching;
    }
}
