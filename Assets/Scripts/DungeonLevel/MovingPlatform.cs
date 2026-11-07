using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovingPlatform : MonoBehaviour
{
    public Vector3 travel;
    public float moveTime = 2.5f;
    public float pauseTime = 0.5f;

    Rigidbody rb;
    Collider[] colliders;
    Vector3 startPos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        colliders = GetComponentsInChildren<Collider>();
        startPos = rb.position;
    }

    void FixedUpdate()
    {
        Vector3 next = startPos + travel * Mathf.SmoothStep(0f, 1f, Progress());
        Vector3 moveAmount = next - rb.position;

        LevelManager lm = LevelManager.Instance;
        if (lm && moveAmount != Vector3.zero && colliders.Length > 0 && lm.PlayerTouching(TopOfRaft()))
            lm.MovePlayer(moveAmount);

        rb.MovePosition(next);
    }

    float Progress()
    {
        float loopTime = 2f * (pauseTime + moveTime);
        float t = Time.time % loopTime;

        if (t < pauseTime) return 0f;
        t -= pauseTime;
        if (t < moveTime) return t / moveTime;
        t -= moveTime;
        if (t < pauseTime) return 1f;
        t -= pauseTime;
        return 1f - t / moveTime;
    }

    Bounds TopOfRaft()
    {
        Bounds b = colliders[0].bounds;
        foreach (Collider c in colliders) b.Encapsulate(c.bounds);
        return new Bounds(new Vector3(b.center.x, b.max.y + 0.15f, b.center.z), new Vector3(b.size.x, 0.3f, b.size.z));
    }
}
