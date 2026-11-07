using System.Collections;
using UnityEngine;

// Moves an object somewhere else once the Guardian (the boss) is dead.
// We use it for two things:
//  - the escape ledges in the well: they start hidden inside the wall and
//    slide out when the boss dies, so you can climb out (reverse = true)
//  - the exit door: it starts closed and sinks into the floor when the boss dies
public class BossTriggered : MonoBehaviour
{
    // how far it moves (in metres, in world directions)
    public Vector3 offset;

    // false: starts where it was placed and moves by offset (the door sinking)
    // true: starts at "placed + offset" and moves back to where it was placed
    //       (the ledges start pushed into the wall and slide out)
    public bool reverse;

    public float duration = 1.5f;   // how long the move takes in seconds

    // turn off its colliders when it's finished moving
    // (so the sunk door can't block you)
    public bool disableCollidersWhenDone;

    Vector3 from, to;   // where the move starts and ends

    void Start()
    {
        // work out the start and end of the move from where the object was placed
        Vector3 placed = transform.position;
        from = reverse ? placed + offset : placed;
        to = reverse ? placed : placed + offset;
        transform.position = from;

        // wait for the boss to die
        LevelManager.BossDefeated += OnBossDefeated;
    }

    void OnDestroy()
    {
        // stop listening for the event when this object is removed
        LevelManager.BossDefeated -= OnBossDefeated;
    }

    void OnBossDefeated() => StartCoroutine(Move());

    // slides the object from "from" to "to" over "duration" seconds
    IEnumerator Move()
    {
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            // SmoothStep makes it start slow, speed up and slow down at the end,
            // which looks nicer than moving at a constant speed
            transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;   // wait one frame
        }
        transform.position = to;

        if (disableCollidersWhenDone)
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
    }
}
