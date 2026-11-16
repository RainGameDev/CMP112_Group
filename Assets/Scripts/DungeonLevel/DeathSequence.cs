using System.Collections;
using UnityEngine;

public class DeathSequence : MonoBehaviour
{
    public GameObject life;
    public Transform collapseBody;
    public GameObject[] hideOnDeath;
    public float removeAfter = 3f;

    bool dead;

    void Update()
    {
        if (dead || life) return;
        dead = true;
        StartCoroutine(Die());
    }

    IEnumerator Die()
    {
        foreach (Behaviour b in GetComponentsInChildren<Behaviour>())
            if (b != this && !(b is HealthManager)) b.enabled = false;
        foreach (LineRenderer line in GetComponentsInChildren<LineRenderer>())
            line.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;
        foreach (GameObject h in hideOnDeath)
            if (h) h.SetActive(false);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        if (collapseBody) yield return Collapse();
        else yield return new WaitForSeconds(removeAfter);

        Destroy(gameObject);
    }

    IEnumerator Collapse()
    {
        Vector3 feet = transform.position;
        Vector3 axis = transform.right;

        for (float t = 0; t < 0.15f; t += Time.deltaTime)
        {
            collapseBody.position += Vector3.down * 1.2f * Time.deltaTime;
            yield return null;
        }

        float fallen = 0f;
        for (float t = 0; fallen < 85f; t += Time.deltaTime)
        {
            float step = (60f + 900f * t) * Time.deltaTime;
            step = Mathf.Min(step, 85f - fallen);
            collapseBody.RotateAround(feet, axis, -step);
            fallen += step;
            yield return null;
        }

        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            collapseBody.RotateAround(feet, axis, Mathf.Sin(t / 0.2f * Mathf.PI) * 40f * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, removeAfter - 1.5f));
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            collapseBody.position += Vector3.down * 0.6f * Time.deltaTime;
            yield return null;
        }
    }
}
