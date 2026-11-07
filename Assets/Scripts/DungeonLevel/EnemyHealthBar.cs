using UnityEngine;

// The small red health bar that floats above a skeleton's head.
// It stays hidden until you hit the skeleton, then shows for a few seconds
// so you can see how much health it has left. It always turns to face the
// camera so it never looks squashed or side-on.
public class EnemyHealthBar : MonoBehaviour
{
    public HealthManager health;    // the enemy's health (our team's HealthManager)
    public Transform fill;          // the red part, it gets shorter as health goes down
    public float width = 0.9f;      // full width of the bar in metres
    public float showFor = 4f;      // seconds after the last hit before it hides again

    float lastHealth;     // health last frame, so we can tell when it was hit
    float visibleUntil;   // the time it should hide again

    void Start()
    {
        // if nobody set the health in the inspector, look for it on the enemy
        if (!health) health = GetComponentInParent<HealthManager>();
        lastHealth = health ? health.currentHealth : 0f;
        SetVisible(false);
    }

    // LateUpdate runs after everything has moved this frame,
    // so the bar doesn't lag behind the enemy or the camera
    void LateUpdate()
    {
        if (!health) return;

        // got hit? show the bar for a few seconds
        if (health.currentHealth < lastHealth) visibleUntil = Time.time + showFor;
        lastHealth = health.currentHealth;

        bool visible = Time.time < visibleUntil && health.currentHealth > 0f;
        SetVisible(visible);
        if (!visible) return;

        // face the camera
        var cam = Camera.main;
        if (cam) transform.rotation = cam.transform.rotation;

        // pct is the fraction of health left (1 = full, 0.5 = half...)
        float pct = Mathf.Clamp01(health.currentHealth / Mathf.Max(health.maxHealth, 0.01f));
        if (fill)
        {
            // shrink the red bar and slide it left, so it shrinks
            // towards the left edge instead of towards the middle
            Vector3 s = fill.localScale;
            fill.localScale = new Vector3(width * pct, s.y, s.z);
            fill.localPosition = new Vector3(-width * (1f - pct) * 0.5f, 0f, -0.01f);
        }
    }

    // hides or shows the bar's pieces (the dark back and the red fill)
    void SetVisible(bool on)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.enabled = on;
    }
}
