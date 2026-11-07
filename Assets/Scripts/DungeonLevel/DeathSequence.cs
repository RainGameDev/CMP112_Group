using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Plays the enemy's death instead of it just vanishing.
//
// How it knows the enemy died: our team's HealthManager destroys its
// "deathObject" when health reaches 0. Instead of letting it destroy the whole
// enemy, we point deathObject at an empty child called "Life". When "Life"
// disappears, this script knows the enemy is dead and plays the death:
//  - the PSX skeleton model topples over backwards and sinks into the floor
//  - the old primitive (blocks and spheres) body breaks apart into bones instead
// Then the enemy is removed.
public class DeathSequence : MonoBehaviour
{
    public GameObject life;          // the empty "Life" child (the HealthManager's deathObject)
    public Transform[] parts;        // body pieces that scatter (only for the primitive body)
    public Transform collapseBody;   // the skinned PSX skeleton model, which topples over instead
    public GameObject[] hideOnDeath; // things to hide straight away, like the health bar and the "!" mark
    public float floorOffset = 0.05f;   // how high above the feet the bones land
    public float removeAfter = 3f;      // seconds until the body is removed

    bool dead;   // so the death only plays once

    void Update()
    {
        // still alive while "Life" exists
        if (dead || life) return;
        dead = true;
        StartCoroutine(Die());
    }

    IEnumerator Die()
    {
        // stop everything else on the enemy (AI, hit flash, boss logic, rig animation),
        // but keep the HealthManager
        foreach (var b in GetComponentsInChildren<Behaviour>())
            if (b != this && !(b is HealthManager)) b.enabled = false;
        foreach (var line in GetComponentsInChildren<LineRenderer>()) line.enabled = false;   // the Guardian's shockwave
        // no colliders, so the corpse can't block the player or be hit again
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var h in hideOnDeath)
            if (h) h.SetActive(false);
        // kinematic = physics stops moving it, we animate it ourselves
        var rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        // the PSX skeleton: topple over (see Collapse below)
        if (collapseBody && (parts == null || parts.Length == 0))
        {
            yield return Collapse();
            Destroy(gameObject);
            yield break;
        }

        // the primitive body: give every bone a random push upwards and outwards,
        // and a random spin
        float floor = transform.position.y + floorOffset;
        var velocity = new Dictionary<Transform, Vector3>();
        var spin = new Dictionary<Transform, Vector3>();
        foreach (var p in parts)
        {
            if (!p) continue;
            p.SetParent(transform, true);
            Vector2 r = Random.insideUnitCircle * 2.5f;
            velocity[p] = new Vector3(r.x, Random.Range(2f, 4.5f), r.y);
            spin[p] = Random.insideUnitSphere * 540f;
        }

        // bones fly and bounce once to rest (simple home-made physics: gravity
        // pulls the speed down every frame, and when a bone hits the floor it
        // bounces up a little and slows down)
        for (float t = 0; t < 1.4f; t += Time.deltaTime)
        {
            foreach (var p in parts)
            {
                if (!p) continue;
                Vector3 v = velocity[p] + Physics.gravity * Time.deltaTime;
                Vector3 pos = p.position + v * Time.deltaTime;
                if (pos.y < floor)
                {
                    pos.y = floor;
                    v = new Vector3(v.x * 0.4f, -v.y * 0.25f, v.z * 0.4f);
                    spin[p] *= 0.5f;
                }
                velocity[p] = v;
                p.position = pos;
                p.Rotate(spin[p] * Time.deltaTime, Space.World);
            }
            yield return null;
        }

        // leave the bones lying there for a moment
        yield return new WaitForSeconds(Mathf.Max(0f, removeAfter - 2.4f));

        // sink into the floor
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            foreach (var p in parts)
                if (p) p.position += Vector3.down * (0.5f * Time.deltaTime);
            yield return null;
        }

        Destroy(gameObject);
    }

    // knees give, then it topples backwards with a small bounce, lies there, and sinks
    IEnumerator Collapse()
    {
        Vector3 feet = transform.position;   // it falls over around its feet
        Vector3 axis = transform.right;      // rotating around its own right side = falling backwards
        float fallen = 0f, target = -85f;    // how far it has tipped, and how far to tip (almost flat)

        // the knees give way: drop a little first
        for (float t = 0; t < 0.15f; t += Time.deltaTime)
        {
            collapseBody.position += Vector3.down * (1.2f * Time.deltaTime);
            yield return null;
        }

        // tip over backwards, getting faster each frame like something falling
        for (float t = 0; fallen > target; t += Time.deltaTime)
        {
            float step = -(60f + 900f * t) * Time.deltaTime;   // speeds up like it's falling
            step = Mathf.Max(step, target - fallen);           // don't go past flat
            collapseBody.RotateAround(feet, axis, step);
            fallen += step;
            yield return null;
        }

        // a small bounce when it hits the floor
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            collapseBody.RotateAround(feet, axis, Mathf.Sin(t / 0.2f * Mathf.PI) * 40f * Time.deltaTime);
            yield return null;
        }

        // lie there for a bit
        yield return new WaitForSeconds(Mathf.Max(0f, removeAfter - 1.5f));

        // then sink into the floor
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            collapseBody.position += Vector3.down * (0.6f * Time.deltaTime);
            yield return null;
        }
    }
}
