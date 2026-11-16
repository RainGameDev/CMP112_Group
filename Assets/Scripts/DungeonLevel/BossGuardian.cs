using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BossGuardian : MonoBehaviour
{
    public Vector2 arenaMin;
    public Vector2 arenaMax;
    public float floorY;

    public float moveSpeed = 1.5f;
    public float contactDamage = 1f;
    public float contactRange = 2f;

    public float slamInterval = 4f;
    public float ringSpeed = 5f;
    public float ringMaxRadius = 9.6f;
    public float ringWidth = 0.6f;
    public float ringDamage = 1f;
    public float safeHeight = 0.3f;
    public float leapSpeed = 6f;
    public LineRenderer ring;

    Rigidbody rb;
    float nextSlamTime;
    float nextContactTime;

    bool leaping;
    float landTime;

    bool ringActive;
    float ringRadius;
    Vector3 ringCentre;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (ring) ring.enabled = false;
    }

    void OnEnable()
    {
        nextSlamTime = Time.time + 2.5f;
    }

    bool PlayerInArena()
    {
        LevelManager lm = LevelManager.Instance;
        if (!lm || !lm.Player) return false;

        Vector3 p = lm.Player.position;
        return p.x > arenaMin.x && p.x < arenaMax.x
            && p.z > arenaMin.y && p.z < arenaMax.y
            && p.y < floorY + 3f;
    }

    void Update()
    {
        LevelManager lm = LevelManager.Instance;
        bool inArena = PlayerInArena();
        Vector3 playerPos = inArena ? lm.Player.position : Vector3.zero;

        if (inArena && !leaping && !ringActive && Time.time >= nextSlamTime)
        {
            leaping = true;
            rb.linearVelocity = new Vector3(0f, leapSpeed, 0f);
            landTime = Time.time + 2f * leapSpeed / -Physics.gravity.y;
        }

        if (leaping && Time.time >= landTime)
        {
            leaping = false;
            nextSlamTime = Time.time + slamInterval;
            ringActive = true;
            ringRadius = 0f;
            ringCentre = new Vector3(transform.position.x, floorY + 0.05f, transform.position.z);
            if (ring) ring.enabled = true;
            if (lm) lm.Shake(0.3f, 0.4f);
        }

        if (ringActive)
        {
            ringRadius += ringSpeed * Time.deltaTime;
            DrawRing();

            if (inArena)
            {
                Vector3 flat = playerPos - ringCentre;
                flat.y = 0f;
                bool onFloor = playerPos.y < floorY + safeHeight;
                bool ringUnderPlayer = Mathf.Abs(flat.magnitude - ringRadius) < ringWidth;
                if (onFloor && ringUnderPlayer) lm.TakeDamage(ringDamage);
            }

            if (ringRadius > ringMaxRadius)
            {
                ringActive = false;
                if (ring) ring.enabled = false;
            }
        }

        if (inArena && Time.time >= nextContactTime && Vector3.Distance(playerPos, transform.position) < contactRange)
        {
            nextContactTime = Time.time + 1.5f;
            lm.TakeDamage(contactDamage);
        }
    }

    void FixedUpdate()
    {
        if (leaping) return;

        Vector3 move = Vector3.zero;
        if (PlayerInArena())
        {
            Vector3 toPlayer = LevelManager.Instance.Player.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toPlayer);
            if (toPlayer.magnitude > contactRange * 0.7f) move = toPlayer.normalized * moveSpeed;
        }
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    void DrawRing()
    {
        if (!ring) return;
        ring.positionCount = 48;
        for (int i = 0; i < 48; i++)
        {
            float angle = i / 48f * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius;
            ring.SetPosition(i, ringCentre + offset);
        }
    }
}
