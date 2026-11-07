using System.Collections;
using UnityEngine;

// A floor tile or slab that crumbles when the player steps on it.
// It shakes for a moment (the warning), then falls away, and comes back
// a few seconds later so the player can try again.
// If dipOnly is ticked it only sinks down a bit and stays there instead.
// That's the harmless "fake-out" crack in the corridor before the Crumbling
// Gallery, which teaches the player that cracked floors are dangerous.
public class CrumblePlatform : MonoBehaviour
{
    public float shakeTime = 0.8f;     // seconds it shakes before falling
    public float respawnTime = 3f;     // seconds before it comes back
    public bool dipOnly;               // true = just sink a little and stay (the fake-out one)
    public float dipDistance = 0.75f;  // how far the fake-out one sinks

    Collider[] colliders;
    Renderer[] renderers;
    bool busy;   // true while it's shaking/falling/gone, so it doesn't start again

    void Start()
    {
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        var lm = LevelManager.Instance;
        if (busy || !lm || colliders.Length == 0) return;

        // Make a thin box sitting on top of the platform.
        // If the player is inside it, they're standing on the platform.
        Bounds b = colliders[0].bounds;
        foreach (var c in colliders) b.Encapsulate(c.bounds);
        var top = new Bounds(new Vector3(b.center.x, b.max.y + 0.15f, b.center.z),
                             new Vector3(b.size.x * 0.9f, 0.3f, b.size.z * 0.9f));

        if (lm.PlayerTouching(top))
            StartCoroutine(Crumble());
    }

    IEnumerator Crumble()
    {
        busy = true;
        Vector3 home = transform.position;   // remember where it belongs so it can come back

        // shake: move it to a slightly random spot every frame
        for (float t = 0; t < shakeTime; t += Time.deltaTime)
        {
            Vector2 jitter = Random.insideUnitCircle * 0.04f;
            transform.position = home + new Vector3(jitter.x, 0f, jitter.y);
            yield return null;
        }
        transform.position = home;

        // the fake-out one just sinks down and stays there
        if (dipOnly)
        {
            Vector3 to = home + Vector3.down * dipDistance;
            while (transform.position != to)
            {
                transform.position = Vector3.MoveTowards(transform.position, to, 6f * Time.deltaTime);
                yield return null;
            }
            yield break;   // stays down for good
        }

        // drop quickly for a moment...
        for (float t = 0; t < 0.4f; t += Time.deltaTime)
        {
            transform.position += Vector3.down * (8f * Time.deltaTime);
            yield return null;
        }
        // ...then disappear (no collider, so the player falls through)
        SetSolid(false);

        // wait, then put it back where it was
        yield return new WaitForSeconds(respawnTime);
        transform.position = home;
        SetSolid(true);
        busy = false;
    }

    // turns the platform's colliders and visuals on or off together
    void SetSolid(bool solid)
    {
        foreach (var c in colliders) c.enabled = solid;
        foreach (var r in renderers) r.enabled = solid;
    }
}
