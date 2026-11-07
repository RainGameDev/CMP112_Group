using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    public HealthManager health;
    public Transform fill;
    public float width = 0.9f;
    public float showFor = 4f;

    float lastHealth;
    float hideTime;

    void Start()
    {
        if (!health) health = GetComponentInParent<HealthManager>();
        if (health) lastHealth = health.currentHealth;
        SetVisible(false);
    }

    void LateUpdate()
    {
        if (!health) return;

        if (health.currentHealth < lastHealth) hideTime = Time.time + showFor;
        lastHealth = health.currentHealth;

        bool visible = Time.time < hideTime && health.currentHealth > 0f;
        SetVisible(visible);
        if (!visible) return;

        if (fill)
        {
            float percent = Mathf.Clamp01(health.currentHealth / health.maxHealth);
            fill.localScale = new Vector3(width * percent, fill.localScale.y, fill.localScale.z);
            fill.localPosition = new Vector3(width * (1f - percent) / 2f, 0f, 0.01f);
        }
    }

    void SetVisible(bool visible)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            r.enabled = visible;
    }
}
