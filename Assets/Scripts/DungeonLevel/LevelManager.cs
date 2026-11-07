using System;
using System.Collections;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public static event Action WellDrained;
    public static event Action BossDefeated;
    public static event Action PlayerRespawned;
    public static event Action WeaponObtained;

    [Header("Health")]
    public float healthPerHeart = 33f;
    public float invulnerableTime = 1f;

    [Header("Respawn")]
    public Transform spawnPoint;
    public float killHeight = -6f;
    public float fadeTime = 0.25f;

    [Header("Sequence")]
    public GameObject boss;
    public GameObject[] ambushEnemies;

    [Header("Overview camera")]
    public float overviewBlendTime = 1.2f;
    public float overviewFogStart = 90f;
    public float overviewFogEnd = 180f;
    public float overviewAmbientBoost = 2.2f;

    public Transform Player { get; private set; }
    public bool IsWellDrained { get; private set; }
    public bool IsBossDefeated { get; private set; }
    public bool IsComplete { get; private set; }
    public bool HasWeapon { get; private set; }
    public HealthManager PlayerHealth { get; private set; }
    public HealthManager BossHealth { get; private set; }
    public float BossMaxHealth { get; private set; } = 1f;
    public string Message { get; private set; }
    public float MessageEndTime { get; private set; }
    public float DamageFlashEndTime { get; private set; }
    public float Fade { get; private set; }
    public float DiedAt { get; private set; } = -100f;
    public int Deaths { get; private set; }
    public bool RelicFound { get; private set; }
    public float PlayTime => (IsComplete ? endTime : Time.time) - startTime;

    Rigidbody playerRb;
    PlayerMovement playerMovement;
    GameObject playerLife;
    float lastPlayerHealth;
    Vector3 respawnPoint;
    float safeUntil;
    bool respawning;

    bool hasBoss;

    Transform cam;
    Vector3 camNormalPos;
    float shakeEndTime, shakeStrength, shakeDuration;
    bool overviewOn;
    Vector3 overviewFocus;
    float overviewDistance;
    float overviewAmount;
    float normalFogStart, normalFogEnd;
    Color normalAmbient;

    float startTime, endTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p)
        {
            Player = p.transform;
            playerRb = p.GetComponent<Rigidbody>();
            playerMovement = p.GetComponent<PlayerMovement>();
            PlayerHealth = p.GetComponent<HealthManager>();
        }

        if (PlayerHealth)
        {
            CreatePlayerLife();
            lastPlayerHealth = PlayerHealth.currentHealth;
        }

        if (spawnPoint) respawnPoint = spawnPoint.position;
        else if (Player) respawnPoint = Player.position;
        startTime = Time.time;

        foreach (GameObject e in ambushEnemies)
            if (e) e.SetActive(false);
        if (boss)
        {
            hasBoss = true;
            BossHealth = boss.GetComponent<HealthManager>();
            if (BossHealth) BossMaxHealth = BossHealth.maxHealth;
            boss.SetActive(false);
        }

        if (Camera.main)
        {
            cam = Camera.main.transform;
            camNormalPos = cam.localPosition;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = RenderSettings.fog ? RenderSettings.fogColor : Color.black;
        }

        normalFogStart = RenderSettings.fogStartDistance;
        normalFogEnd = RenderSettings.fogEndDistance;
        normalAmbient = RenderSettings.ambientLight;

        gameObject.AddComponent<GameUI>();
        ShowMessage("The Drowned Well", 3f);
    }

    void CreatePlayerLife()
    {
        playerLife = new GameObject("Life");
        playerLife.transform.SetParent(Player, false);
        PlayerHealth.deathObject = playerLife;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;

        WellDrained = null;
        BossDefeated = null;
        PlayerRespawned = null;
        WeaponObtained = null;
        RenderSettings.fogStartDistance = normalFogStart;
        RenderSettings.fogEndDistance = normalFogEnd;
        RenderSettings.ambientLight = normalAmbient;
    }

    void Update()
    {
        if (!Player) return;

        if (PlayerHealth)
        {
            if (PlayerHealth.currentHealth < lastPlayerHealth)
            {
                DamageFlashEndTime = Time.time + 0.25f;
                Shake(0.12f, 0.2f);
            }
            lastPlayerHealth = PlayerHealth.currentHealth;

            if (playerLife == null && !respawning) PlayerDied();
        }

        if (Player.position.y < killHeight) Fall(0.5f, null);

        if (hasBoss && !IsBossDefeated)
        {
            bool bossDead = boss == null || (BossHealth && BossHealth.currentHealth <= 0f);
            if (bossDead)
            {
                IsBossDefeated = true;
                Debug.Log("Boss defeated");
                if (BossDefeated != null) BossDefeated();
                Shake(0.35f, 1.2f);
                ShowMessage("The guardian falls. The well is filling - climb!", 4f);
            }
        }
    }

    void LateUpdate()
    {
        if (!cam) return;

        float target = overviewOn ? 1f : 0f;
        overviewAmount = Mathf.MoveTowards(overviewAmount, target, Time.deltaTime / overviewBlendTime);
        float k = Mathf.SmoothStep(0f, 1f, overviewAmount);

        Vector3 normalPos = cam.parent ? cam.parent.TransformPoint(camNormalPos) : camNormalPos;
        Vector3 widePos = overviewFocus - cam.forward * overviewDistance;
        Vector3 pos = Vector3.Lerp(normalPos, widePos, k);

        if (Time.time < shakeEndTime)
        {
            float strength = shakeStrength * (shakeEndTime - Time.time) / shakeDuration;
            pos += UnityEngine.Random.insideUnitSphere * strength;
        }
        cam.position = pos;

        if (RenderSettings.fog)
        {
            RenderSettings.fogStartDistance = Mathf.Lerp(normalFogStart, overviewFogStart, k);
            RenderSettings.fogEndDistance = Mathf.Lerp(normalFogEnd, overviewFogEnd, k);
        }
        RenderSettings.ambientLight = normalAmbient * Mathf.Lerp(1f, overviewAmbientBoost, k);

        if (k > 0.5f) CutawayPiece.ShowAll();
        else if (Player) CutawayPiece.UpdateAll(cam.position, Player.position);
    }

    public void SetOverview(Vector3 focus, float distance)
    {
        overviewFocus = focus;
        overviewDistance = distance;
        overviewOn = true;
    }

    public void ClearOverview()
    {
        overviewOn = false;
    }

    public void GiveWeapon(WeaponItem weapon)
    {
        if (HasWeapon) return;
        HasWeapon = true;
        if (WeaponObtained != null) WeaponObtained();

        string weaponName = "Weapon";
        if (weapon && !string.IsNullOrEmpty(weapon.itemName)) weaponName = weapon.itemName;
        ShowMessage(weaponName + " acquired - press Attack", 3f);
    }

    public void TakeDamage(float hearts, bool ignoreInvulnerable = false)
    {
        if (IsComplete || respawning || !PlayerHealth) return;
        if (!ignoreInvulnerable && Time.time < safeUntil) return;

        safeUntil = Time.time + invulnerableTime;
        PlayerHealth.ChangeHealth(-hearts * healthPerHeart);
    }

    void PlayerDied()
    {
        Deaths++;
        Debug.Log("Player died");
        PlayerHealth.currentHealth = PlayerHealth.maxHealth;
        lastPlayerHealth = PlayerHealth.currentHealth;
        CreatePlayerLife();
        DiedAt = Time.time;
        StartCoroutine(Respawn(respawnPoint));
    }

    public void Fall(float damage, Transform respawnAt)
    {
        if (respawning || IsComplete) return;

        if (damage > 0f)
        {
            TakeDamage(damage, true);
            if (PlayerHealth && PlayerHealth.currentHealth <= 0f) return;
        }

        Vector3 point = respawnPoint;
        if (respawnAt) point = respawnAt.position;
        StartCoroutine(Respawn(point));
    }

    public void SetCheckpoint(Transform point, string label)
    {
        respawnPoint = point.position;
        if (PlayerHealth) PlayerHealth.currentHealth = PlayerHealth.maxHealth;
        ShowMessage(label, 2f);
    }

    public void AddMaxHealth()
    {
        RelicFound = true;
        if (PlayerHealth)
        {
            PlayerHealth.maxHealth += healthPerHeart;
            PlayerHealth.currentHealth = PlayerHealth.maxHealth;
        }
        ShowMessage("Relic found - max health up", 3f);
    }

    public void DrainWell()
    {
        if (IsWellDrained) return;
        IsWellDrained = true;

        foreach (GameObject e in ambushEnemies)
            if (e) e.SetActive(true);
        if (boss) boss.SetActive(true);

        Shake(0.2f, 1.5f);
        if (WellDrained != null) WellDrained();
        ShowMessage("The well drains... something stirs below", 3.5f);
    }

    public void CompleteLevel()
    {
        if (IsComplete) return;
        IsComplete = true;
        Debug.Log("Level complete");
        endTime = Time.time;
        if (playerMovement) playerMovement.enabled = false;
        if (playerRb) playerRb.linearVelocity = Vector3.zero;
    }

    public void MovePlayer(Vector3 amount)
    {
        if (playerRb) playerRb.position += amount;
        else if (Player) Player.position += amount;
    }

    public void Shake(float strength, float duration)
    {
        if (Time.time < shakeEndTime && strength < shakeStrength) return;
        shakeStrength = strength;
        shakeDuration = duration;
        shakeEndTime = Time.time + duration;
    }

    public bool PlayerTouching(Bounds box)
    {
        Collider[] hits = Physics.OverlapBox(box.center, box.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider c in hits)
            if (IsPlayerBody(c)) return true;
        return false;
    }

    public static bool IsPlayerBody(Collider c)
    {
        return c != null && !c.isTrigger && c.attachedRigidbody != null && c.attachedRigidbody.CompareTag("Player");
    }

    public void ShowMessage(string text, float seconds)
    {
        Message = text;
        MessageEndTime = Time.time + seconds;
    }

    IEnumerator Respawn(Vector3 point)
    {
        respawning = true;

        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            Fade = t / fadeTime;
            yield return null;
        }
        Fade = 1f;

        Player.position = point;
        if (playerRb)
        {
            playerRb.position = point;
            playerRb.linearVelocity = Vector3.zero;
        }
        if (PlayerRespawned != null) PlayerRespawned();
        yield return new WaitForSeconds(0.1f);

        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            Fade = 1f - t / fadeTime;
            yield return null;
        }
        Fade = 0f;

        respawning = false;
        safeUntil = Time.time + invulnerableTime;
    }

    public string Objective()
    {
        if (IsComplete) return "";
        if (!HasWeapon) return "Get the spear from the chest";
        if (!IsWellDrained) return "Find a way to drain the well";
        if (!IsBossDefeated) return "Defeat the Guardian";
        return "Climb out before the well floods";
    }
}
