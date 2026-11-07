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

    Rigidbody playerRb;
    PlayerMovement playerMovement;
    HealthManager playerHealth;
    GameObject playerLife;
    float lastPlayerHealth;
    Vector3 respawnPoint;
    float safeUntil;
    bool respawning;

    bool hasBoss;
    HealthManager bossHealth;
    float bossMaxHealth = 1f;

    Transform cam;
    Vector3 camNormalPos;
    float shakeEndTime, shakeStrength, shakeDuration;
    bool overviewOn;
    Vector3 overviewFocus;
    float overviewDistance;
    float overviewAmount;
    float normalFogStart, normalFogEnd;
    Color normalAmbient;

    float damageFlashEndTime;
    float fade;
    string message;
    float messageEndTime;
    float startTime, endTime;
    int deaths;
    bool relicFound;

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
            playerHealth = p.GetComponent<HealthManager>();
        }

        if (playerHealth)
        {
            CreatePlayerLife();
            lastPlayerHealth = playerHealth.currentHealth;
        }

        if (spawnPoint) respawnPoint = spawnPoint.position;
        else if (Player) respawnPoint = Player.position;
        startTime = Time.time;

        foreach (GameObject e in ambushEnemies)
            if (e) e.SetActive(false);
        if (boss)
        {
            hasBoss = true;
            bossHealth = boss.GetComponent<HealthManager>();
            if (bossHealth) bossMaxHealth = bossHealth.maxHealth;
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

        ShowMessage("The Drowned Well", 3f);
    }

    void CreatePlayerLife()
    {
        playerLife = new GameObject("Life");
        playerLife.transform.SetParent(Player, false);
        playerHealth.deathObject = playerLife;
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

        if (playerHealth)
        {
            if (playerHealth.currentHealth < lastPlayerHealth)
            {
                damageFlashEndTime = Time.time + 0.25f;
                Shake(0.12f, 0.2f);
            }
            lastPlayerHealth = playerHealth.currentHealth;

            if (playerLife == null && !respawning) PlayerDied();
        }

        if (Player.position.y < killHeight) Fall(0.5f, null);

        if (hasBoss && !IsBossDefeated)
        {
            bool bossDead = boss == null || (bossHealth && bossHealth.currentHealth <= 0f);
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
        if (IsComplete || respawning || !playerHealth) return;
        if (!ignoreInvulnerable && Time.time < safeUntil) return;

        safeUntil = Time.time + invulnerableTime;
        playerHealth.ChangeHealth(-hearts * healthPerHeart);
    }

    void PlayerDied()
    {
        deaths++;
        Debug.Log("Player died");
        playerHealth.currentHealth = playerHealth.maxHealth;
        lastPlayerHealth = playerHealth.currentHealth;
        CreatePlayerLife();
        StartCoroutine(Respawn(respawnPoint));
        ShowMessage("You died", 2f);
    }

    public void Fall(float damage, Transform respawnAt)
    {
        if (respawning || IsComplete) return;

        if (damage > 0f)
        {
            TakeDamage(damage, true);
            if (playerHealth && playerHealth.currentHealth <= 0f) return;
        }

        Vector3 point = respawnPoint;
        if (respawnAt) point = respawnAt.position;
        StartCoroutine(Respawn(point));
    }

    public void SetCheckpoint(Transform point, string label)
    {
        respawnPoint = point.position;
        if (playerHealth) playerHealth.currentHealth = playerHealth.maxHealth;
        ShowMessage(label, 2f);
    }

    public void AddMaxHealth()
    {
        relicFound = true;
        if (playerHealth)
        {
            playerHealth.maxHealth += healthPerHeart;
            playerHealth.currentHealth = playerHealth.maxHealth;
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
        message = text;
        messageEndTime = Time.time + seconds;
    }

    IEnumerator Respawn(Vector3 point)
    {
        respawning = true;

        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            fade = t / fadeTime;
            yield return null;
        }
        fade = 1f;

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
            fade = 1f - t / fadeTime;
            yield return null;
        }
        fade = 0f;

        respawning = false;
        safeUntil = Time.time + invulnerableTime;
    }

    string Objective()
    {
        if (IsComplete) return "";
        if (!HasWeapon) return "Get the spear from the chest";
        if (!IsWellDrained) return "Find a way to drain the well";
        if (!IsBossDefeated) return "Defeat the Guardian";
        return "Climb out before the well floods";
    }

    void OnGUI()
    {
        float s = Screen.height / 1080f;
        Rect fullScreen = new Rect(0, 0, Screen.width, Screen.height);
        Color textColor = new Color(0.92f, 0.86f, 0.7f);

        if (Time.time < damageFlashEndTime)
            DrawBox(fullScreen, new Color(0.8f, 0f, 0f, damageFlashEndTime - Time.time));
        if (fade > 0f)
            DrawBox(fullScreen, new Color(0f, 0f, 0f, fade));

        if (playerHealth)
        {
            float percent = Mathf.Clamp01(playerHealth.currentHealth / playerHealth.maxHealth);
            Rect bar = new Rect(30 * s, 30 * s, 360 * s, 26 * s);
            DrawBar(bar, percent, new Color(0.75f, 0.1f, 0.1f));
            string text = Mathf.CeilToInt(Mathf.Max(0f, playerHealth.currentHealth)) + " / " + Mathf.CeilToInt(playerHealth.maxHealth);
            DrawText(bar, text, 20 * s, Color.white, TextAnchor.MiddleCenter);
        }

        DrawText(new Rect(30 * s, 70 * s, 900 * s, 40 * s), Objective(), 24 * s, textColor, TextAnchor.UpperLeft);

        if (IsWellDrained && !IsBossDefeated && bossHealth)
        {
            float w = 700 * s;
            Rect bar = new Rect((Screen.width - w) / 2f, Screen.height - 90 * s, w, 22 * s);
            DrawText(new Rect(bar.x, bar.y - 38 * s, w, 34 * s), "THE GUARDIAN", 24 * s, textColor, TextAnchor.LowerCenter);
            DrawBar(bar, Mathf.Clamp01(bossHealth.currentHealth / bossMaxHealth), new Color(0.65f, 0.08f, 0.08f));
        }

        if (!IsComplete && Time.time < messageEndTime)
        {
            float alpha = Mathf.Clamp01(messageEndTime - Time.time);
            DrawText(new Rect(0, Screen.height * 0.18f, Screen.width, 60 * s), message, 38 * s, new Color(1f, 0.95f, 0.85f, alpha), TextAnchor.MiddleCenter);
        }

        if (IsComplete)
        {
            DrawBox(fullScreen, new Color(0f, 0f, 0f, 0.6f));
            float y = Screen.height * 0.35f;
            DrawText(new Rect(0, y, Screen.width, 90 * s), "LEVEL COMPLETE", 72 * s, new Color(1f, 0.85f, 0.5f), TextAnchor.MiddleCenter);

            float time = endTime - startTime;
            int minutes = (int)(time / 60f);
            int seconds = (int)(time % 60f);
            string stats = "Time  " + minutes + ":" + seconds.ToString("00")
                         + "      Deaths  " + deaths
                         + "      Relic  " + (relicFound ? "found" : "missed");
            DrawText(new Rect(0, y + 100 * s, Screen.width, 50 * s), stats, 30 * s, Color.white, TextAnchor.MiddleCenter);
        }
    }

    static void DrawBar(Rect r, float percent, Color color)
    {
        DrawBox(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), new Color(0f, 0f, 0f, 0.7f));
        DrawBox(new Rect(r.x, r.y, r.width * percent, r.height), color);
    }

    static void DrawBox(Rect r, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    static void DrawText(Rect r, string text, float size, Color color, TextAnchor anchor)
    {
        if (string.IsNullOrEmpty(text)) return;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = Mathf.RoundToInt(size);
        style.fontStyle = FontStyle.Bold;
        style.alignment = anchor;

        style.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.8f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
        style.normal.textColor = color;
        GUI.Label(r, text, style);
    }
}
