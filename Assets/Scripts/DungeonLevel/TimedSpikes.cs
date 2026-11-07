using UnityEngine;

// Spikes in the Spike Run corridor that go up and down on a timer.
// They hurt the player while they're up, so you have to time your jump.
// Just before they come up they poke out a little bit as a warning,
// so the player has a fair chance to see it coming.
public class TimedSpikes : MonoBehaviour
{
    public float upTime = 2f;       // seconds the spikes stay up
    public float downTime = 2f;     // seconds the spikes stay down
    public float warnTime = 0.45f;  // how long before coming up they poke out as a warning
    public float damage = 0.5f;     // half a heart
    public float moveSpeed = 6f;    // how fast they slide up and down

    Vector3 upPosition;   // where the spikes sit when they're fully up (where they were placed)
    Bounds hurtBox;       // the area that hurts the player
    float sinkDepth;      // how far down they go to hide in the floor

    void Start()
    {
        upPosition = transform.position;

        // the hurt box is the size of the spike model
        hurtBox = new Bounds(transform.position, Vector3.zero);
        foreach (var r in GetComponentsInChildren<Renderer>())
            hurtBox.Encapsulate(r.bounds);

        // sink them by their own height (plus a tiny bit) so they hide completely
        sinkDepth = hurtBox.size.y + 0.05f;
    }

    void Update()
    {
        // work out where we are in the up/down cycle
        // (% gives the remainder, so t goes 0 -> 4 seconds and then starts again)
        float cycle = upTime + downTime;
        float t = Time.time % cycle;
        bool up = t < upTime;
        bool warning = !up && t > cycle - warnTime;   // the last bit of "down" time

        // fully up, a bit up (the warning), or fully hidden
        float sink = up ? 0f : warning ? sinkDepth * 0.65f : sinkDepth;
        Vector3 target = upPosition + Vector3.down * sink;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        // hurt the player if they're standing in the spikes while they're up
        // (the level manager gives the player a second of invincibility after a hit,
        // so this doesn't take a heart every single frame)
        var lm = LevelManager.Instance;
        if (up && lm && lm.PlayerTouching(hurtBox))
            lm.TakeDamage(damage);
    }
}
