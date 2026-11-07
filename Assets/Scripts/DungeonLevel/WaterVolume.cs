using UnityEngine;

public class WaterVolume : MonoBehaviour
{
    public Transform surface;
    public Vector2 size;
    public float level;
    public float damage = 0.5f;
    public Transform respawnAt;

    [Header("Well only")]
    public bool isWell;
    public float drainSpeed = 1f;
    public float riseSpeed = 0.05f;
    public float maxLevel = 4f;

    float targetLevel;
    bool rising;

    void Start()
    {
        targetLevel = level;
        if (isWell)
        {
            LevelManager.WellDrained += OnWellDrained;
            LevelManager.BossDefeated += OnBossDefeated;
            LevelManager.PlayerRespawned += OnPlayerRespawned;
        }
        UpdateSurface();
    }

    void OnDestroy()
    {
        LevelManager.WellDrained -= OnWellDrained;
        LevelManager.BossDefeated -= OnBossDefeated;
        LevelManager.PlayerRespawned -= OnPlayerRespawned;
    }

    void OnWellDrained()
    {
        targetLevel = 0f;
    }

    void OnBossDefeated()
    {
        rising = true;
        respawnAt = null;
    }

    void OnPlayerRespawned()
    {
        if (rising) level = 0f;
    }

    void Update()
    {
        if (rising) level = Mathf.Min(maxLevel, level + riseSpeed * Time.deltaTime);
        else level = Mathf.MoveTowards(level, targetLevel, drainSpeed * Time.deltaTime);
        UpdateSurface();

        LevelManager lm = LevelManager.Instance;
        if (!lm || !lm.Player || level <= 0.05f) return;

        Vector3 p = lm.Player.position - transform.position;
        bool insideWidth = Mathf.Abs(p.x) < size.x / 2f && Mathf.Abs(p.z) < size.y / 2f;
        bool underWater = p.y < level - 0.25f;
        if (insideWidth && underWater) lm.Fall(damage, respawnAt);
    }

    void UpdateSurface()
    {
        if (!surface) return;

        surface.gameObject.SetActive(level > 0.05f);
        surface.localScale = new Vector3(size.x, Mathf.Max(level, 0.01f), size.y);

        float bob = Mathf.Sin(Time.time * 1.3f) * 0.03f;
        surface.localPosition = new Vector3(0f, level / 2f + bob, 0f);
    }
}
