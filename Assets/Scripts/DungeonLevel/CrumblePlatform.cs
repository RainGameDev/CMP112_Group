using System.Collections;
using UnityEngine;

public class CrumblePlatform : MonoBehaviour
{
    public float shakeTime = 0.8f;
    public float respawnTime = 3f;
    public bool dipOnly;
    public float dipDistance = 0.75f;

    Collider[] colliders;
    Renderer[] renderers;
    bool busy;

    void Start()
    {
        colliders = GetComponentsInChildren<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        LevelManager lm = LevelManager.Instance;
        if (busy || !lm || colliders.Length == 0) return;

        if (lm.PlayerTouching(TopOfPlatform()))
            StartCoroutine(Crumble());
    }

    Bounds TopOfPlatform()
    {
        Bounds b = colliders[0].bounds;
        foreach (Collider c in colliders) b.Encapsulate(c.bounds);

        Vector3 centre = new Vector3(b.center.x, b.max.y + 0.15f, b.center.z);
        Vector3 size = new Vector3(b.size.x * 0.9f, 0.3f, b.size.z * 0.9f);
        return new Bounds(centre, size);
    }

    IEnumerator Crumble()
    {
        busy = true;
        Vector3 home = transform.position;

        for (float t = 0; t < shakeTime; t += Time.deltaTime)
        {
            Vector2 jitter = Random.insideUnitCircle * 0.04f;
            transform.position = home + new Vector3(jitter.x, 0f, jitter.y);
            yield return null;
        }
        transform.position = home;

        if (dipOnly)
        {
            Vector3 down = home + Vector3.down * dipDistance;
            while (transform.position != down)
            {
                transform.position = Vector3.MoveTowards(transform.position, down, 6f * Time.deltaTime);
                yield return null;
            }
            yield break;
        }

        for (float t = 0; t < 0.4f; t += Time.deltaTime)
        {
            transform.position += Vector3.down * 8f * Time.deltaTime;
            yield return null;
        }
        SetSolid(false);

        yield return new WaitForSeconds(respawnTime);
        transform.position = home;
        SetSolid(true);
        busy = false;
    }

    void SetSolid(bool solid)
    {
        foreach (Collider c in colliders) c.enabled = solid;
        foreach (Renderer r in renderers) r.enabled = solid;
    }
}
