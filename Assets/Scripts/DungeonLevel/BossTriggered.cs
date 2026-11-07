using System.Collections;
using UnityEngine;

public class BossTriggered : MonoBehaviour
{
    public Vector3 offset;
    public bool reverse;
    public float duration = 1.5f;
    public bool disableCollidersWhenDone;

    Vector3 startPos;
    Vector3 endPos;

    void Start()
    {
        if (reverse)
        {
            startPos = transform.position + offset;
            endPos = transform.position;
        }
        else
        {
            startPos = transform.position;
            endPos = transform.position + offset;
        }
        transform.position = startPos;

        LevelManager.BossDefeated += OnBossDefeated;
    }

    void OnDestroy()
    {
        LevelManager.BossDefeated -= OnBossDefeated;
    }

    void OnBossDefeated()
    {
        StartCoroutine(Move());
    }

    IEnumerator Move()
    {
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        transform.position = endPos;

        if (disableCollidersWhenDone)
        {
            foreach (Collider c in GetComponentsInChildren<Collider>())
                c.enabled = false;
        }
    }
}
