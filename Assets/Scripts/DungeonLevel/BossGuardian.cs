using UnityEngine;

// The Guardian, the boss on the floor of the well.
// It slowly walks after the player, and every few seconds it jumps up and
// slams down, sending a red shockwave ring out across the floor.
// To avoid the ring you either jump over it as it passes, or stand on one
// of the stone plinths (being above the floor keeps you safe).
// The jump before each slam is the warning that a shockwave is coming.
[RequireComponent(typeof(Rigidbody))]
public class BossGuardian : MonoBehaviour
{
    public Vector2 arenaMin;     // world x/z of the well floor (one corner)
    public Vector2 arenaMax;     // the opposite corner
    public float floorY;         // the height of the well floor

    public float moveSpeed = 1.5f;      // it walks slowly
    public float contactDamage = 1f;    // a full heart if you touch it
    public float contactRange = 2f;     // how close counts as touching

    public float slamInterval = 4f;     // seconds between slams
    public float ringSpeed = 5f;        // how fast the shockwave spreads (metres per second)
    public float ringMaxRadius = 9.6f;  // how far the shockwave goes before it disappears
    public float ringWidth = 0.6f;      // how thick the dangerous part of the ring is
    public float ringDamage = 1f;       // a full heart if the ring hits you
    public float safeHeight = 0.3f;   // player this far above the floor is safe from the ring
    public float leapSpeed = 6f;      // it leaps before every slam, which is the tell
    public LineRenderer ring;         // draws the red circle on the floor

    Rigidbody rb;
    float nextSlam;           // when it's allowed to slam next
    float landAt = -1f;       // when it will land from its leap (-1 = not leaping)
    float nextContact;        // stops touching it from hurting every frame
    float ringRadius = -1f;   // current size of the shockwave (-1 = no shockwave right now)
    Vector3 ringCentre;       // where the shockwave started

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (ring) ring.enabled = false;
    }

    // runs when the boss is switched on (when the well is drained):
    // give the player a couple of seconds before the first slam
    void OnEnable()
    {
        nextSlam = Time.time + 2.5f;
    }

    // Is the player down on the well floor with the boss?
    // (inside the arena's rectangle and not up on the walls)
    // "out Vector3 p" also hands back the player's position
    bool PlayerInArena(out Vector3 p)
    {
        var lm = LevelManager.Instance;
        p = lm && lm.Player ? lm.Player.position : Vector3.zero;
        return lm && lm.Player
            && p.x > arenaMin.x && p.x < arenaMax.x
            && p.z > arenaMin.y && p.z < arenaMax.y
            && p.y < floorY + 3f;
    }

    void Update()
    {
        var lm = LevelManager.Instance;
        bool inArena = PlayerInArena(out Vector3 p);

        // leap, then slam on landing
        // Time to slam: jump straight up and work out when it will land
        // (from physics: time in the air = 2 x jump speed / gravity)
        if (inArena && ringRadius < 0f && landAt < 0f && Time.time >= nextSlam)
        {
            rb.linearVelocity = new Vector3(0f, leapSpeed, 0f);
            landAt = Time.time + 2f * leapSpeed / -Physics.gravity.y;
        }

        // Landed: start a shockwave from where it landed and shake the camera
        if (landAt > 0f && Time.time >= landAt)
        {
            landAt = -1f;
            nextSlam = Time.time + slamInterval;
            ringRadius = 0f;
            ringCentre = new Vector3(transform.position.x, floorY + 0.05f, transform.position.z);
            if (ring) ring.enabled = true;
            if (lm) lm.Shake(0.3f, 0.4f);
        }

        // a shockwave is spreading
        if (ringRadius >= 0f)
        {
            ringRadius += ringSpeed * Time.deltaTime;
            DrawRing();

            if (inArena)
            {
                // how far the player is from the centre of the ring (along the floor).
                // If that's about the same as the ring's size, the ring is passing
                // under them right now, and if they're on the floor it hurts them
                float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(ringCentre.x, ringCentre.z));
                if (p.y < floorY + safeHeight && Mathf.Abs(d - ringRadius) < ringWidth)
                    lm.TakeDamage(ringDamage);
            }

            // the ring has gone far enough, remove it
            if (ringRadius > ringMaxRadius)
            {
                ringRadius = -1f;
                if (ring) ring.enabled = false;
            }
        }

        // walking into the boss hurts (at most once every 1.5 seconds)
        if (inArena && Time.time >= nextContact && Vector3.Distance(p, transform.position) < contactRange)
        {
            nextContact = Time.time + 1.5f;
            lm.TakeDamage(contactDamage);
        }
    }

    void FixedUpdate()
    {
        if (landAt > 0f) return;   // mid-leap: let it fly straight up and down

        // walk towards the player while they're in the arena, stopping a bit short
        Vector3 move = Vector3.zero;
        if (PlayerInArena(out Vector3 p))
        {
            Vector3 to = p - transform.position;
            to.y = 0f;
            if (to.magnitude > contactRange * 0.7f) move = to.normalized * moveSpeed;
            if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to.normalized);
        }
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    // draws the shockwave as a circle made of 48 points around the centre
    void DrawRing()
    {
        if (!ring) return;
        ring.positionCount = 48;
        for (int i = 0; i < 48; i++)
        {
            // the angle of this point around the circle, in radians (a full circle = 2 x PI)
            float a = i / 48f * Mathf.PI * 2f;
            ring.SetPosition(i, ringCentre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ringRadius);
        }
    }
}
