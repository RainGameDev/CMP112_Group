using UnityEngine;

// Makes an enemy flash white and quickly "pop" bigger for a split second
// whenever it gets hit, so the player can clearly see their attack landed.
// It doesn't deal with damage itself: it just watches our HealthManager and
// reacts when the health number goes down.
public class HitFlash : MonoBehaviour
{
    public float flashTime = 0.12f;          // how long the flash lasts in seconds
    public Color flashColor = Color.white;   // the colour it flashes
    public Transform target;   // what flashes and pops (the enemy's body). If empty it uses this object

    HealthManager health;
    Renderer[] renderers;           // every visible part of the body
    MaterialPropertyBlock block;    // lets us change the colour of one object without editing the shared material
    Vector3 baseScale;              // normal size, we pop around this
    float lastHealth;               // health last frame, to notice when it drops
    float flashUntil;               // the time the current flash ends
    bool wasFlashing;               // was it flashing last frame?

    void Awake()
    {
        health = GetComponent<HealthManager>();
        if (!target) target = transform;
        renderers = target.GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        baseScale = target.localScale;
        lastHealth = health ? health.currentHealth : 0f;
    }

    void Update()
    {
        // if health went down since last frame, we were hit: start a flash
        if (health && health.currentHealth < lastHealth)
            flashUntil = Time.time + flashTime;
        if (health) lastHealth = health.currentHealth;

        bool flashing = Time.time < flashUntil;
        if (flashing || wasFlashing)
        {
            foreach (var r in renderers)
            {
                if (!r) continue;
                if (flashing)
                {
                    // paint it white. _BaseColor is the colour slot in URP materials,
                    // _Color is the older name, we set both to be safe
                    r.GetPropertyBlock(block);
                    block.SetColor("_BaseColor", flashColor);
                    block.SetColor("_Color", flashColor);
                    r.SetPropertyBlock(block);
                }
                else
                {
                    // the flash just ended: go back to the normal colours.
                    // We only do this once (not every frame), otherwise it would
                    // keep wiping the skeleton's glowing eye colour
                    r.SetPropertyBlock(null);
                }
            }
        }
        wasFlashing = flashing;

        // the "pop": 12% bigger right when it's hit, shrinking back as the flash fades
        float punch = flashing ? 0.12f * Mathf.Clamp01((flashUntil - Time.time) / flashTime) : 0f;
        target.localScale = baseScale * (1f + punch);
    }
}
