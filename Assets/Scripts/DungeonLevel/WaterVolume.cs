using UnityEngine;

// Water that hurts the player and sends them back if they fall in.
// We use it in two places:
//  - the Flooded Cistern, where the water just stays at the same level
//  - the well (isWell = true), which is flooded at the start, drains when the
//    sluice wheel is turned (so you can fight the boss), and then slowly rises
//    again after the boss dies, so you have to climb out in time
public class WaterVolume : MonoBehaviour
{
    public Transform surface;     // the blue see-through cube that shows the water, we resize it to the water level
    public Vector2 size;          // how wide (x) and long (z) the water is in metres, centred on this object
    public float level;           // how deep the water is, in metres above this object
    public float damage = 0.5f;   // half a heart for falling in
    public Transform respawnAt;   // where to send the player. Empty = their last checkpoint

    [Header("Well only")]
    public bool isWell;              // tick this for the well so it drains and rises
    public float drainSpeed = 1f;    // metres per second it drains
    public float riseSpeed = 0.05f;  // metres per second it rises after the boss dies
    public float maxLevel = 4f;      // the highest the water rises to

    float target;   // the level the water is moving towards
    bool rising;    // true after the boss is dead

    void Start()
    {
        target = level;
        if (isWell)
        {
            // the well listens for the important moments in the level
            LevelManager.WellDrained += OnWellDrained;
            LevelManager.BossDefeated += OnBossDefeated;
            LevelManager.PlayerRespawned += OnPlayerRespawned;
        }
        UpdateSurface();
    }

    void OnDestroy()
    {
        // stop listening when removed so the events don't call a deleted object
        LevelManager.WellDrained -= OnWellDrained;
        LevelManager.BossDefeated -= OnBossDefeated;
        LevelManager.PlayerRespawned -= OnPlayerRespawned;
    }

    // the wheel was turned: drain down to nothing
    void OnWellDrained() => target = 0f;

    // the boss is dead: start rising, and from now on falling in sends you
    // to your last checkpoint instead of the old respawn spot
    void OnBossDefeated()
    {
        rising = true;
        respawnAt = null;
    }

    // dying during the climb resets the water so you get a fair second go
    void OnPlayerRespawned()
    {
        if (rising) level = 0f;
    }

    void Update()
    {
        // move the water level: up slowly if rising, otherwise towards the target (draining)
        if (rising) level = Mathf.Min(maxLevel, level + riseSpeed * Time.deltaTime);
        else level = Mathf.MoveTowards(level, target, drainSpeed * Time.deltaTime);
        UpdateSurface();

        // no water (or no player) = nothing to check
        var lm = LevelManager.Instance;
        if (!lm || !lm.Player || level <= 0.05f) return;

        // Is the player inside the water? p is the player's position relative
        // to the middle of the water. They're "in" if they're within the width and
        // length, and their feet are more than 25cm below the surface
        Vector3 p = lm.Player.position - transform.position;
        if (Mathf.Abs(p.x) < size.x * 0.5f && Mathf.Abs(p.z) < size.y * 0.5f && p.y < level - 0.25f)
            lm.Fall(damage, respawnAt);
    }

    // resizes the visible water cube to match the current level
    void UpdateSurface()
    {
        if (!surface) return;

        // hide the cube completely when there's no water
        surface.gameObject.SetActive(level > 0.05f);
        surface.localScale = new Vector3(size.x, Mathf.Max(level, 0.01f), size.y);

        // a tiny up-and-down bob so the water doesn't look frozen
        float bob = Mathf.Sin(Time.time * 1.3f) * 0.03f;
        surface.localPosition = new Vector3(0f, level * 0.5f + bob, 0f);
    }
}
