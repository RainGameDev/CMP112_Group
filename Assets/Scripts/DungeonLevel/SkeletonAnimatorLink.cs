using UnityEngine;

public class SkeletonAnimatorLink : MonoBehaviour
{
    public Animator animator;

    EnemyAI ai;
    HealthManager health;
    float lastHealth;
    float lastAttackTime;
    Vector3 lastPosition;

    void Start()
    {
        ai = GetComponent<EnemyAI>();
        health = GetComponent<HealthManager>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        lastPosition = transform.position;
        if (health) lastHealth = health.currentHealth;
        if (ai) lastAttackTime = ai.lastAttackTime;
    }

    void Update()
    {
        if (!animator || Time.deltaTime <= 0f) return;

        Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        animator.SetFloat("Speed", flat.magnitude, 0.1f, Time.deltaTime);
        animator.SetBool("InAir", Mathf.Abs(velocity.y) > 1.5f);

        if (ai)
        {
            float nextAttack = ai.lastAttackTime + ai.attackCooldown;
            animator.SetBool("Windup", ai.state == EnemyAI.State.Attack && Time.time >= nextAttack - 0.45f);

            if (ai.lastAttackTime != lastAttackTime)
            {
                lastAttackTime = ai.lastAttackTime;
                animator.SetTrigger("Attack");
            }
        }

        if (health)
        {
            if (health.currentHealth < lastHealth) animator.SetTrigger("Hit");
            lastHealth = health.currentHealth;
        }
    }
}
