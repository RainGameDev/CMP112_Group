using UnityEngine;

// The skeleton enemy's brain (its AI). Each skeleton:
//  - wanders around near where it was placed (its "home")
//  - only notices the player if it can actually see them (nothing in the way)
//  - shows a "!" and hops when it spots you, then chases you
//  - warns you before every attack: it stops, leans back and its eyes glow
//    brighter (the wind-up), then lunges forward in a fixed direction, so you
//    have time to step out of the way
//  - won't walk off ledges, and gives up and walks home if you lead it too far
// Its health is our team's HealthManager, and dying is handled by DeathSequence.
//
// It works like a "state machine": the skeleton is always in exactly one state
// (patrolling, chasing, winding up...), and each state decides what it does and
// when it switches to another state.
[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    [Header("Parts")]
    public Transform body;          // visual root, leans during attacks (only used for the old primitive body)
    public Renderer[] eyes;         // the two glowing eye spheres
    public GameObject alertMark;    // the "!" above the head

    [Header("Senses")]
    public float aggroRange = 8f;              // how far away it can spot you (metres)
    public float leashRange = 14f;             // gives up past this distance from home
    public float maxHeightDifference = 1.5f;   // ignores you if you're much higher or lower

    [Header("Movement")]
    public float patrolSpeed = 1.2f;    // walking speed when wandering
    public float patrolRadius = 2.5f;   // how far from home it wanders
    public float chaseSpeed = 2.8f;     // speed when chasing you (slower than the player so you can get away)
    public float maxDrop = 1f;          // won't step off anything deeper than this

    [Header("Attack")]
    public float attackRange = 1.5f;     // how close it gets before attacking
    public float attackWindup = 0.5f;    // seconds of warning before the lunge
    public float lungeSpeed = 7f;        // how fast it lunges
    public float lungeTime = 0.18f;      // how long the lunge lasts
    public float attackCooldown = 1.1f;  // seconds between attacks
    public float damage = 0.5f;          // half a heart per hit
    public float knockback = 6f;         // how hard it gets pushed back when you hit it

    // every state the skeleton can be in
    enum State { Patrol, Alert, Chase, Windup, Lunge, Recover, Stunned, Return }

    // read by SkeletonRigAnimator to pose the body
    public bool IsWindingUp => state == State.Windup;
    public bool IsLunging => state == State.Lunge;
    public bool IsStunned => state == State.Stunned || state == State.Alert;

    State state = State.Patrol;   // it starts off wandering
    float stateUntil;             // when the current timed state ends
    Rigidbody rb;
    HealthManager health;
    float lastHealth;             // health last frame, so we notice when it gets hit
    Vector3 home;                 // where it was placed, it wanders around here
    Vector3 patrolTarget;         // the spot it's walking to while wandering
    Vector3 lungeDir;             // the direction it locked in for its lunge
    bool lungeHit;                // so one lunge can only hurt you once
    float nextAttack;             // when it's allowed to attack again
    MaterialPropertyBlock eyeBlock;                  // used to change just this skeleton's eye colour
    Color eyeColor = new Color(3f, 0.4f, 0.15f);     // bright red-orange (above 1 so it glows with bloom)

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<HealthManager>();
        lastHealth = health ? health.currentHealth : 0f;
        eyeBlock = new MaterialPropertyBlock();
    }

    // OnEnable runs when the skeleton appears (at the start, or when the
    // ambush skeletons are switched on), so home is wherever it appears
    void OnEnable()
    {
        home = transform.position;
        patrolTarget = home;
        if (alertMark) alertMark.SetActive(false);
    }

    void Update()
    {
        var lm = LevelManager.Instance;

        // knocked back when hit: if health went down since last frame,
        // push it away from the player and stun it for a moment
        if (health && health.currentHealth < lastHealth && lm && lm.Player)
        {
            Vector3 away = transform.position - lm.Player.position;
            away.y = 0f;
            rb.linearVelocity = away.normalized * knockback + Vector3.up * 2f;
            SetState(State.Stunned, 0.35f);
        }
        if (health) lastHealth = health.currentHealth;

        // body language: lean back while winding up, forward while lunging
        // (only does anything for the old primitive body, the PSX model is posed by SkeletonRigAnimator)
        float lean = state == State.Windup ? -25f : state == State.Lunge ? 20f : 0f;
        if (body) body.localRotation = Quaternion.Slerp(body.localRotation, Quaternion.Euler(lean, 0f, 0f), 12f * Time.deltaTime);

        // eyes: very bright when attacking, dim when calm, normal otherwise
        float glow = state == State.Windup || state == State.Lunge ? 2.5f : state == State.Patrol || state == State.Return ? 0.6f : 1f;
        foreach (var e in eyes)
        {
            if (!e) continue;
            e.GetPropertyBlock(eyeBlock);
            eyeBlock.SetColor("_BaseColor", eyeColor * glow);
            e.SetPropertyBlock(eyeBlock);
        }
    }

    void LateUpdate()
    {
        // make the "!" face the camera so it's always readable
        var cam = Camera.main;
        if (alertMark && alertMark.activeSelf && cam) alertMark.transform.rotation = cam.transform.rotation;
    }

    // all the movement and decisions happen here, in step with the physics
    void FixedUpdate()
    {
        var lm = LevelManager.Instance;
        if (!lm || !lm.Player) return;

        // fell off the map? just remove it
        if (transform.position.y < lm.killHeight)
        {
            Destroy(gameObject);
            return;
        }

        // work out where the player is compared to us.
        // We flatten it (y = 0) so we only care about the direction along the floor
        Vector3 toPlayer = lm.Player.position - transform.position;
        float heightDiff = Mathf.Abs(toPlayer.y);
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;
        Vector3 dirToPlayer = dist > 0.01f ? toPlayer / dist : transform.forward;

        Vector3 move = Vector3.zero;   // how we want to move this step
        switch (state)
        {
            case State.Patrol:
                // spotted the player? turn to them, hop in surprise, show the "!"
                if (CanSee(lm, dist, heightDiff))
                {
                    Face(dirToPlayer);
                    rb.linearVelocity = new Vector3(0f, 3f, 0f);   // startled hop
                    if (alertMark) alertMark.SetActive(true);
                    SetState(State.Alert, 0.6f);
                    break;
                }
                // reached the wander spot? wait a moment, then pick a new random spot near home
                if (Flat(patrolTarget - transform.position).magnitude < 0.3f)
                {
                    if (Time.time >= stateUntil)
                    {
                        Vector2 r = Random.insideUnitCircle * patrolRadius;
                        patrolTarget = home + new Vector3(r.x, 0f, r.y);
                        stateUntil = Time.time + Random.Range(1f, 2.5f);
                    }
                }
                else
                {
                    // otherwise walk towards it
                    move = Flat(patrolTarget - transform.position).normalized * patrolSpeed;
                }
                break;

            case State.Alert:
                // stand still facing the player for a moment, then start chasing
                Face(dirToPlayer);
                if (Time.time >= stateUntil)
                {
                    if (alertMark) alertMark.SetActive(false);
                    state = State.Chase;
                }
                break;

            case State.Chase:
                // give up if it's too far from home, the player got away,
                // or the player is on a much higher/lower level
                if (Flat(transform.position - home).magnitude > leashRange || dist > aggroRange * 1.8f || heightDiff > maxHeightDifference * 1.5f)
                {
                    state = State.Return;
                    break;
                }
                Face(dirToPlayer);
                // close enough and ready? start the wind-up
                if (dist < attackRange && Time.time >= nextAttack)
                {
                    lungeDir = dirToPlayer;   // committed: the player can sidestep
                    SetState(State.Windup, attackWindup);
                }
                else if (dist > attackRange * 0.7f)
                {
                    // otherwise keep walking towards the player
                    move = dirToPlayer * chaseSpeed;
                }
                break;

            case State.Windup:
                // stands still (move stays zero) until the wind-up time is over, then lunges
                if (Time.time >= stateUntil)
                {
                    lungeHit = false;
                    SetState(State.Lunge, lungeTime);
                }
                break;

            case State.Lunge:
                // dash in the locked direction. It only hurts you if you're still close,
                // in front of it, and on the same level, so stepping aside dodges it
                move = lungeDir * lungeSpeed;
                if (!lungeHit && dist < attackRange * 0.9f && Vector3.Dot(lungeDir, dirToPlayer) > 0.3f && heightDiff < 1.2f)
                {
                    lungeHit = true;
                    lm.TakeDamage(damage);
                }
                if (Time.time >= stateUntil)
                {
                    nextAttack = Time.time + attackCooldown;
                    SetState(State.Recover, 0.4f);
                }
                break;

            case State.Recover:
            case State.Stunned:
                // a short pause after attacking or being hit, then back to chasing
                if (Time.time >= stateUntil) state = State.Chase;
                if (state == State.Stunned) return;   // let the knockback play out
                break;

            case State.Return:
                // walking home. If it sees you again and isn't too far from home, chase again
                if (CanSee(lm, dist, heightDiff) && Flat(transform.position - home).magnitude < leashRange * 0.7f)
                {
                    state = State.Chase;
                    break;
                }
                Vector3 back = Flat(home - transform.position);
                if (back.magnitude < 0.4f)
                {
                    // got home: go back to wandering and heal up
                    state = State.Patrol;
                    if (health) health.currentHealth = health.maxHealth;   // reset if you run away
                }
                else
                {
                    move = back.normalized * chaseSpeed;
                    Face(back.normalized);
                }
                break;
        }

        // spread out from other skeletons, and don't walk off ledges
        move += Separation();
        if (move != Vector3.zero && !GroundAhead(move.normalized)) move = Vector3.zero;

        // walk the way it's facing (the lunge keeps its committed direction)
        if (move.sqrMagnitude > 0.04f && (state == State.Patrol || state == State.Return || state == State.Chase))
            Face(move.normalized);

        // set the sideways speed but keep the up/down speed, so gravity still works
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    // Can the skeleton see the player? They have to be close enough, on roughly
    // the same level, and a ray from the skeleton's eyes to the player has to hit
    // the player first (not a wall)
    bool CanSee(LevelManager lm, float dist, float heightDiff)
    {
        if (dist > aggroRange || heightDiff > maxHeightDifference) return false;
        Vector3 eye = transform.position + Vector3.up * 1.5f;
        Vector3 target = lm.Player.position + Vector3.up;
        if (Physics.Raycast(eye, (target - eye).normalized, out RaycastHit hit, aggroRange + 2f, ~0, QueryTriggerInteraction.Ignore))
            return LevelManager.IsPlayerBody(hit.collider);
        return false;
    }

    // stop at ledges: is there floor just ahead, no deeper than maxDrop?
    // (shoots a ray straight down from a point just in front of the skeleton)
    bool GroundAhead(Vector3 dir)
    {
        Vector3 origin = transform.position + dir * 0.6f + Vector3.up * 0.5f;
        return Physics.Raycast(origin, Vector3.down, 0.5f + maxDrop, ~0, QueryTriggerInteraction.Ignore);
    }

    // don't stack up on the same spot as other skeletons:
    // finds any skeletons very close by and pushes away from them
    // (the closer they are, the harder the push)
    Vector3 Separation()
    {
        Vector3 push = Vector3.zero;
        foreach (var c in Physics.OverlapSphere(transform.position + Vector3.up, 0.9f, ~0, QueryTriggerInteraction.Ignore))
        {
            var other = c.attachedRigidbody ? c.attachedRigidbody.GetComponent<Enemy>() : null;
            if (!other || other == this) continue;
            Vector3 away = Flat(transform.position - other.transform.position);
            // if they're exactly on top of each other, push in a random direction
            push += away.sqrMagnitude > 0.001f ? away.normalized * (1.2f - Mathf.Min(away.magnitude, 1.2f)) * 2f : Random.insideUnitSphere;
        }
        return Flat(push);
    }

    // turns smoothly towards a direction (Slerp turns part of the way each step instead of snapping)
    void Face(Vector3 dir)
    {
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.fixedDeltaTime);
    }

    // switch to a state that lasts for a set time
    void SetState(State s, float duration)
    {
        state = s;
        stateUntil = Time.time + duration;
    }

    // the same vector but flat along the floor (no up/down part)
    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
}
