using UnityEngine;

public class HitFlash : MonoBehaviour
{
    public float flashTime = 0.12f;
    public Color flashColor = Color.white;
    public Transform target;

    HealthManager health;
    Renderer[] renderers;
    MaterialPropertyBlock block;
    Vector3 normalScale;
    float lastHealth;
    float flashEndTime;
    bool flashing;

    void Awake()
    {
        health = GetComponent<HealthManager>();
        if (!target) target = transform;
        renderers = target.GetComponentsInChildren<Renderer>();
        block = new();
        normalScale = target.localScale;
        if (health) lastHealth = health.currentHealth;
    }

    void Update()
    {
        if (health)
        {
            if (health.currentHealth < lastHealth) StartFlash();
            lastHealth = health.currentHealth;
        }

        if (flashing && Time.time >= flashEndTime)
        {
            flashing = false;
            foreach (Renderer r in renderers)
                if (r) r.SetPropertyBlock(null);
            target.localScale = normalScale;
        }

        if (flashing)
        {
            float amountLeft = (flashEndTime - Time.time) / flashTime;
            target.localScale = normalScale * (1f + 0.12f * amountLeft);
        }
    }

    void StartFlash()
    {
        flashing = true;
        flashEndTime = Time.time + flashTime;

        foreach (Renderer r in renderers)
        {
            if (!r) continue;
            r.GetPropertyBlock(block);
            block.SetColor("_BaseColor", flashColor);
            block.SetColor("_Color", flashColor);
            r.SetPropertyBlock(block);
        }
    }
}
