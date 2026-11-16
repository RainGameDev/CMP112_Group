using UnityEngine;

public class TimedSpikes : MonoBehaviour
{
    public float upTime = 2f;
    public float downTime = 2f;
    public float warnTime = 0.45f;
    public float damage = 0.5f;
    public float moveSpeed = 6f;

    Vector3 upPos;
    Bounds hurtBox;
    float hideDepth;

    void Start()
    {
        upPos = transform.position;

        hurtBox = new Bounds(transform.position, Vector3.zero);
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            hurtBox.Encapsulate(r.bounds);

        hideDepth = hurtBox.size.y + 0.05f;
    }

    void Update()
    {
        float cycle = upTime + downTime;
        float t = Time.time % cycle;

        bool up = t < upTime;
        bool warning = !up && t > cycle - warnTime;

        float depth = hideDepth;
        if (up) depth = 0f;
        else if (warning) depth = hideDepth * 0.65f;

        Vector3 target = upPos + Vector3.down * depth;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        LevelManager lm = LevelManager.Instance;
        if (up && lm && lm.PlayerTouching(hurtBox))
            lm.TakeDamage(damage);
    }
}
