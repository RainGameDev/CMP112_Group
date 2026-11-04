using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum State
    {
        Patrol,
        Chase,
        Attack,
    }

    [Header("Patrol")]
    public float patrolRadius = 6f;
    public float patrolSpeed = 2f;
    public float waitTimeAtPoint = 1.5f;

    [Header("Detection")]
    public float sightRange = 10f;

    [Range(0f, 360f)]
    public float fieldOfView = 110f;
    public float eyeHeight = 1.5f;
    public LayerMask obstacleMask = ~0;
    public float loseSightTime = 3f;

    [Header("Chase")]
    public float chaseSpeed = 4.5f;

    [Header("Attack")]
    public float attackRange = 1.8f;
    public float attackCooldown = 1.2f;
    public int attackDamage = 10;

    public NavMeshAgent agent;
    public Transform player;
    public Vector3 startPosition;
    public State state = State.Patrol;

    public float waitTimer;
    public float lastSeenTimer;
    public float lastAttackTime = -999f;

    public void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void Start()
    {
        startPosition = transform.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        agent.speed = patrolSpeed;
        SetNewPatrolPoint();
    }

    public void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool canSee = CanSeePlayer(distance);

        switch (state)
        {
            case State.Patrol:
                UpdatePatrol(canSee);
                break;
            case State.Chase:
                UpdateChase(canSee, distance);
                break;
            case State.Attack:
                UpdateAttack(canSee, distance);
                break;
        }
    }

    // States
    public void UpdatePatrol(bool canSee)
    {
        if (canSee)
        {
            EnterChase();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                SetNewPatrolPoint();
                waitTimer = 0f;
            }
        }
    }

    public void UpdateChase(bool canSee, float distance)
    {
        // NOTE: rembembering could be a nice feature
        // like it looks around based off the last position and momentum
        // so it can check behind walls
        if (canSee)
            lastSeenTimer = 0f;
        else
            lastSeenTimer += Time.deltaTime;

        // return to patrolling
        if (lastSeenTimer >= loseSightTime)
        {
            EnterPatrol();
            return;
        }

        if (distance <= attackRange)
        {
            state = State.Attack;
            agent.ResetPath();
            return;
        }

        agent.SetDestination(player.position);
    }

    public void UpdateAttack(bool canSee, float distance)
    {
        // Player moved out of range
        if (distance > attackRange * 1.2f)
        {
            state = State.Chase;
            agent.speed = chaseSpeed;
            return;
        }

        FacePlayer();

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            Attack();
        }
    }

    // Transitions
    public void EnterChase()
    {
        state = State.Chase;
        agent.speed = chaseSpeed;
        lastSeenTimer = 0f;
    }

    public void EnterPatrol()
    {
        state = State.Patrol;
        agent.speed = patrolSpeed;
        waitTimer = 0f;
        SetNewPatrolPoint();
    }

    // Behaviour
    public void SetNewPatrolPoint()
    {
        Vector3 randomPoint = startPosition + Random.insideUnitSphere * patrolRadius;
        randomPoint.y = startPosition.y;

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    public bool CanSeePlayer(float distance)
    {
        if (distance > sightRange)
            return false;

        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPos = player.position + Vector3.up * eyeHeight;
        Vector3 dirToPlayer = (targetPos - eyePos).normalized;

        // Inside field of view
        if (Vector3.Angle(transform.forward, dirToPlayer) > fieldOfView * 0.5f)
            return false;

        // Line of sight not blocked
        if (
            Physics.Raycast(
                eyePos,
                dirToPlayer,
                out RaycastHit hit,
                sightRange,
                obstacleMask,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        return false;
    }

    public void FacePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
    }

    public void Attack()
    {
        Debug.Log($"{name} attacks the player for {attackDamage} damage!");

        player.GetComponent<HealthManager>().ChangeHealth(-attackDamage);

        // TODO: animations
    }

    public void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? startPosition : transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, patrolRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 left = Quaternion.Euler(0, -fieldOfView * 0.5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, fieldOfView * 0.5f, 0) * transform.forward;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, left * sightRange);
        Gizmos.DrawRay(transform.position, right * sightRange);
    }
}
