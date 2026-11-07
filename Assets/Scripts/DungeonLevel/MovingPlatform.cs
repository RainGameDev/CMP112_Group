using UnityEngine;

// The raft in the Flooded Cistern. It moves back and forth across the water
// on a loop and carries the player when they stand on it.
// One full loop: wait, move across, wait, move back = 6 seconds by default.
[RequireComponent(typeof(Rigidbody))]
public class MovingPlatform : MonoBehaviour
{
    public Vector3 travel;           // how far and which way it moves (in metres)
    public float moveTime = 2.5f;    // seconds to get from one end to the other
    public float pauseTime = 0.5f;   // seconds it waits at each end

    Rigidbody rb;
    Collider[] colliders;
    Vector3 start;   // where the raft starts (one end of its path)

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // kinematic means physics doesn't push it around, we move it ourselves
        rb.isKinematic = true;
        colliders = GetComponentsInChildren<Collider>();
        start = rb.position;
    }

    // FixedUpdate runs in step with the physics, which is what you want
    // for something the player stands on
    void FixedUpdate()
    {
        // work out where we are in the loop and turn it into k:
        // 0 = at the start, 1 = at the far end
        float t = Time.time % (2f * (moveTime + pauseTime));
        float k;
        if (t < pauseTime) k = 0f;                                               // waiting at the start
        else if (t < pauseTime + moveTime) k = (t - pauseTime) / moveTime;       // moving across
        else if (t < 2f * pauseTime + moveTime) k = 1f;                          // waiting at the far end
        else k = 1f - (t - 2f * pauseTime - moveTime) / moveTime;                // moving back

        // SmoothStep eases in and out so the raft doesn't jerk at each end
        Vector3 next = start + travel * Mathf.SmoothStep(0f, 1f, k);
        Vector3 delta = next - rb.position;   // how far it moves this physics step

        // Carry the player: check a thin box just above the raft's top surface.
        // If the player is standing in it, move them by the same amount the raft moves.
        // (Our PlayerMovement sets the player's velocity directly every frame, so
        // friction alone wouldn't carry them.)
        var lm = LevelManager.Instance;
        if (lm && delta != Vector3.zero && colliders.Length > 0)
        {
            Bounds b = colliders[0].bounds;
            foreach (var c in colliders) b.Encapsulate(c.bounds);
            var top = new Bounds(new Vector3(b.center.x, b.max.y + 0.15f, b.center.z),
                                 new Vector3(b.size.x, 0.3f, b.size.z));
            if (lm.PlayerTouching(top)) lm.MovePlayer(delta);
        }

        // MovePosition moves a kinematic rigidbody smoothly so physics knows about it
        rb.MovePosition(next);
    }
}
