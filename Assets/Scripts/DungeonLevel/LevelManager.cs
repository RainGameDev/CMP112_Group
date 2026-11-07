using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The "brain" of the dungeon level. There's one of these in the scene and it handles:
//  - the player's hearts, taking damage, dying and respawning at checkpoints
//  - the order of the level: get the spear -> turn the sluice wheel (drains the well)
//    -> kill the Guardian -> climb out before the water rises -> reach the exit
//  - the camera: screen shake, the wide "overview" shot at the overlook, and hiding walls
//  - the HUD drawn on screen: hearts, objective, messages, boss health bar, end screen
// It sits on the "Level Manager" object inside "Dungeon Map (generated)" in Level1TEST,
// and its settings (spawn point, boss, ambush skeletons) are filled in there.
public class LevelManager : MonoBehaviour
{
    // There's only ever one LevelManager, so any script can reach it with
    // LevelManager.Instance instead of having to search for it
    public static LevelManager Instance { get; private set; }

    // Events other scripts can "listen" to, so they react at the right moment
    // without the LevelManager needing to know about them.
    // e.g. the escape ledges listen to BossDefeated so they slide out when the boss dies
    public static event Action WellDrained;       // the sluice wheel was turned
    public static event Action BossDefeated;      // the Guardian died
    public static event Action PlayerRespawned;   // the player was sent back to a checkpoint
    public static event Action WeaponObtained;    // the spear was taken from the chest

    [Header("Hearts")]
    public float maxHearts = 3f;          // how many hearts you have in total
    public float hearts = 3f;             // how many you have right now (0.5 = half a heart)
    public float invulnerableTime = 1f;   // seconds you can't be hurt again after being hit

    [Header("Respawn")]
    public Transform spawnPoint;   // where the player starts the level
    public float killHeight = -6f; // fall below this height (off the map) and you get sent back
    public float fadeTime = 0.25f; // how long the screen takes to fade to black when respawning

    [Header("Sequence")]
    public GameObject boss;              // the Guardian, hidden until the well is drained
    public GameObject[] ambushEnemies;   // the two skeletons that appear when you turn the wheel

    // If the scene doesn't have our team's CombatManager, this script does a simple
    // attack instead so the level can still be played
    [Header("Fallback attack (only used when the scene has no CombatManager)")]
    public float attackDamage = 15f;   // replaced by the weapon's damage when it's picked up
    public float attackRange = 1.6f;   // how far in front of the player the attack reaches
    public float attackCooldown = 0.4f;// seconds between attacks

    // Read-only info other scripts can check (only the LevelManager can change them)
    public Transform Player { get; private set; }
    public bool IsWellDrained { get; private set; }
    public bool IsBossDefeated { get; private set; }
    public bool IsComplete { get; private set; }
    public bool HasWeapon { get; private set; }

    [Header("Overview camera")]
    public float overviewBlendTime = 1.2f;      // seconds to move the camera out to the wide shot
    public float overviewFogStart = 90f;        // fog is pushed far away in the wide shot so you can see everything
    public float overviewFogEnd = 180f;
    public float overviewAmbientBoost = 2.2f;   // candles are tiny from up there, so lift the whole scene

    // the player's parts we need to move them / stop them
    Rigidbody playerRb;
    PlayerMovement playerMovement;
    Vector3 respawnPoint;      // where they go back to when they die (last checkpoint)
    float invulnerableUntil;   // the time the player can be hurt again
    bool hasBoss;
    HealthManager bossHealth;
    float bossMaxHealth;

    // for the fallback attack
    PlayerInputs inputs;
    bool useFallbackAttack;
    float nextAttack;

    // feedback
    Transform cam;               // the main camera (it's a child of the player)
    Vector3 camRestPosition;     // where the camera normally sits behind the player
    float shakeUntil, shakeStrength, shakeDuration;

    // overview camera
    bool overviewActive;         // should the camera be in the wide shot?
    Vector3 overviewFocus;       // what the wide shot looks at
    float overviewDistance;      // how far back the wide shot is
    float overviewBlend;         // 0 = normal camera, 1 = fully in the wide shot
    float baseFogStart, baseFogEnd;   // the normal fog settings, so we can put them back
    Color baseAmbient;                // the normal ambient light, so we can put it back
    float damageFlashUntil;      // the red screen flash when you get hurt
    float fade;                  // how black the screen is (0 = clear, 1 = fully black)
    bool respawning;             // true during the fade, so you can't be hurt mid-respawn

    // for the HUD and the end screen
    string message;
    float messageUntil;
    float startTime, endTime;
    int deaths;
    bool relicFound;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // find the player by its "Player" tag
        var p = GameObject.FindWithTag("Player");
        if (p)
        {
            Player = p.transform;
            playerRb = p.GetComponent<Rigidbody>();
            playerMovement = p.GetComponent<PlayerMovement>();
        }

        // start by respawning at the spawn point until a checkpoint is reached
        respawnPoint = spawnPoint ? spawnPoint.position : (Player ? Player.position : Vector3.zero);
        hearts = maxHearts;
        startTime = Time.time;

        // hide the ambush skeletons and the boss until their moment comes
        foreach (var e in ambushEnemies)
            if (e) e.SetActive(false);

        hasBoss = boss != null;
        if (boss)
        {
            bossHealth = boss.GetComponent<HealthManager>();
            bossMaxHealth = bossHealth ? bossHealth.maxHealth : 1f;
            boss.SetActive(false);
        }

        // dark background that matches the fog, so the void reads as an abyss
        var mainCam = Camera.main;
        if (mainCam)
        {
            cam = mainCam.transform;
            camRestPosition = cam.localPosition;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = RenderSettings.fog ? RenderSettings.fogColor : Color.black;
        }
        // remember the normal lighting so the overview shot can change it and put it back
        baseFogStart = RenderSettings.fogStartDistance;
        baseFogEnd = RenderSettings.fogEndDistance;
        baseAmbient = RenderSettings.ambientLight;

        // only use our own attack if the team's CombatManager isn't in the scene
        useFallbackAttack = FindAnyObjectByType<CombatManager>() == null;
        if (useFallbackAttack)
        {
            inputs = new PlayerInputs();
            inputs.Enable();
        }

        ShowMessage("The Drowned Well", 3f);
    }

    void OnDestroy()
    {
        // clean up when the level ends: clear the events (so nothing keeps listening
        // to an old level) and put the lighting settings back how they were
        if (Instance != this) return;
        Instance = null;
        WellDrained = null;
        BossDefeated = null;
        PlayerRespawned = null;
        WeaponObtained = null;
        inputs?.Disable();
        RenderSettings.fogStartDistance = baseFogStart;
        RenderSettings.fogEndDistance = baseFogEnd;
        RenderSettings.ambientLight = baseAmbient;
    }

    void Update()
    {
        if (!Player) return;

        // fell off the map? take half a heart and go back to the last checkpoint
        if (Player.position.y < killHeight)
            Fall(0.5f, null);

        // Has the boss died? (boss == null covers it being removed; the health
        // check covers its death animation still playing)
        if (hasBoss && !IsBossDefeated && (boss == null || (bossHealth && bossHealth.currentHealth <= 0f)))
        {
            IsBossDefeated = true;
            BossDefeated?.Invoke();   // tells the ledges, exit door and rising water
            Shake(0.35f, 1.2f);
            ShowMessage("The guardian falls. The well is filling - climb!", 4f);
        }

        // our backup attack, only if there's no CombatManager and the player has the spear
        if (useFallbackAttack && HasWeapon && !IsComplete && inputs.Combat.Attack.WasPressedThisFrame() && Time.time >= nextAttack)
            FallbackAttack();
    }

    // LateUpdate runs after the player has moved this frame, so the camera
    // is always placed using the player's final position (no jitter)
    void LateUpdate()
    {
        if (!cam) return;

        // overviewBlend slides between 0 and 1 over time, SmoothStep makes it ease in and out
        overviewBlend = Mathf.MoveTowards(overviewBlend, overviewActive ? 1f : 0f, Time.deltaTime / overviewBlendTime);
        float k = Mathf.SmoothStep(0f, 1f, overviewBlend);

        // "follow" is the normal camera spot behind the player,
        // "wide" is the spot far back looking at the whole level.
        // We blend between the two
        Vector3 follow = cam.parent ? cam.parent.TransformPoint(camRestPosition) : camRestPosition;
        Vector3 wide = overviewFocus - cam.forward * overviewDistance;
        Vector3 position = Vector3.Lerp(follow, wide, k);

        // screen shake: move the camera by a small random amount that gets
        // smaller as the shake runs out
        if (Time.time < shakeUntil)
            position += UnityEngine.Random.insideUnitSphere * (shakeStrength * (shakeUntil - Time.time) / shakeDuration);
        cam.position = position;

        // in the wide shot, push the fog back and brighten things so the level is visible
        if (RenderSettings.fog)
        {
            RenderSettings.fogStartDistance = Mathf.Lerp(baseFogStart, overviewFogStart, k);
            RenderSettings.fogEndDistance = Mathf.Lerp(baseFogEnd, overviewFogEnd, k);
        }
        RenderSettings.ambientLight = baseAmbient * Mathf.Lerp(1f, overviewAmbientBoost, k);

        // no cutaway in the wide shot, it would cut a slice through the whole level
        if (k > 0.5f) CutawayPiece.ShowAll();
        else if (Player) CutawayPiece.UpdateAll(cam.position, Player.position);
    }

    // called by the overlook barrier when the player presses against it
    public void SetOverview(Vector3 focus, float distance)
    {
        overviewFocus = focus;
        overviewDistance = distance;
        overviewActive = true;
    }

    // called when the player steps away from the barrier
    public void ClearOverview() => overviewActive = false;

    // called by the chest when the spear is taken
    public void GiveWeapon(WeaponItem weapon)
    {
        if (HasWeapon) return;
        HasWeapon = true;
        if (weapon) attackDamage = weapon.damage;   // the fallback attack uses the spear's damage
        WeaponObtained?.Invoke();                   // opens the WeaponGate
        ShowMessage((weapon && !string.IsNullOrEmpty(weapon.itemName) ? weapon.itemName : "Weapon") + " acquired - press Attack", 3f);
    }

    // ---------- player state ----------

    // Hurts the player. Enemies, spikes and the boss all call this.
    public void TakeDamage(float amount, bool ignoreInvulnerable = false)
    {
        if (IsComplete || respawning) return;
        // after a hit you get a short moment where you can't be hurt again
        if (!ignoreInvulnerable && Time.time < invulnerableUntil) return;

        invulnerableUntil = Time.time + invulnerableTime;
        hearts -= amount;
        damageFlashUntil = Time.time + 0.25f;   // red flash on screen
        Shake(0.12f, 0.2f);

        // out of hearts: count the death, refill hearts and go back to the last checkpoint
        if (hearts <= 0f)
        {
            deaths++;
            hearts = maxHearts;
            StartCoroutine(RespawnRoutine(respawnPoint));
            ShowMessage("You died", 2f);
        }
    }

    // falling into water, a pit or off the map. respawnAt = null means last checkpoint
    public void Fall(float damage, Transform respawnAt)
    {
        if (respawning || IsComplete) return;
        if (damage > 0f) TakeDamage(damage, true);
        // TakeDamage might already have started a respawn (if that was your last heart)
        if (!respawning) StartCoroutine(RespawnRoutine(respawnAt ? respawnAt.position : respawnPoint));
    }

    // called by a Checkpoint: respawn here from now on and refill the hearts
    public void SetCheckpoint(Transform point, string label)
    {
        respawnPoint = point.position;
        hearts = maxHearts;
        ShowMessage(label, 2f);
    }

    // called by the secret relic: one extra heart for the rest of the level
    public void AddHeart()
    {
        relicFound = true;
        maxHearts += 1f;
        hearts = maxHearts;
        ShowMessage("Relic found - max hearts +1", 3f);
    }

    // called by the sluice wheel
    public void DrainWell()
    {
        if (IsWellDrained) return;
        IsWellDrained = true;

        // bring out the ambush skeletons and wake up the boss
        foreach (var e in ambushEnemies)
            if (e) e.SetActive(true);
        if (boss) boss.SetActive(true);

        Shake(0.2f, 1.5f);
        WellDrained?.Invoke();   // tells the well's water to drain
        ShowMessage("The well drains... something stirs below", 3.5f);
    }

    // called by the exit: stop the player moving and show the end screen
    public void CompleteLevel()
    {
        if (IsComplete) return;
        IsComplete = true;
        endTime = Time.time;
        if (playerMovement) playerMovement.enabled = false;
        if (playerRb) playerRb.linearVelocity = Vector3.zero;
    }

    // moves the player by a small amount (used by the raft to carry them)
    public void MovePlayer(Vector3 delta)
    {
        if (playerRb) playerRb.position += delta;
        else if (Player) Player.position += delta;
    }

    // starts a camera shake. A weaker shake won't interrupt a stronger one already happening
    public void Shake(float strength, float duration)
    {
        if (Time.time < shakeUntil && strength < shakeStrength) return;
        shakeStrength = strength;
        shakeDuration = duration;
        shakeUntil = Time.time + duration;
    }

    // true if the player's body (not its trigger colliders) is inside the box
    // (lots of scripts use this: crumbling floors, the raft, spikes, the overlook barrier)
    public bool PlayerTouching(Bounds box)
    {
        foreach (var c in Physics.OverlapBox(box.center, box.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            if (IsPlayerBody(c)) return true;
        return false;
    }

    // Is this collider the player's actual body? We check it's not a trigger
    // (the player also has trigger colliders, like the interaction range) and that
    // it belongs to the rigidbody tagged "Player"
    public static bool IsPlayerBody(Collider c)
    {
        return c && !c.isTrigger && c.attachedRigidbody && c.attachedRigidbody.CompareTag("Player");
    }

    // shows a message in the middle of the screen for a few seconds
    public void ShowMessage(string text, float seconds)
    {
        message = text;
        messageUntil = Time.time + seconds;
    }

    // fade to black, move the player, fade back in
    IEnumerator RespawnRoutine(Vector3 point)
    {
        respawning = true;
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            fade = t / fadeTime;
            yield return null;
        }
        fade = 1f;

        Teleport(point);
        yield return new WaitForSeconds(0.1f);

        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            fade = 1f - t / fadeTime;
            yield return null;
        }
        fade = 0f;
        respawning = false;
        // a moment of safety after respawning so you don't get hit straight away
        invulnerableUntil = Time.time + invulnerableTime;
    }

    // moves the player instantly and stops any movement they had (so they don't keep falling)
    void Teleport(Vector3 point)
    {
        if (!Player) return;
        Player.position = point;
        if (playerRb)
        {
            playerRb.position = point;
            playerRb.linearVelocity = Vector3.zero;
        }
        PlayerRespawned?.Invoke();
    }

    // simple melee for when the inventory/combat setup isn't in this scene
    void FallbackAttack()
    {
        nextAttack = Time.time + attackCooldown;

        // the attack happens in front of the way the player is facing
        Transform facing = playerMovement && playerMovement.rotationObject ? playerMovement.rotationObject : Player;
        Vector3 centre = Player.position + Vector3.up + facing.forward * (attackRange * 0.6f);

        // damage everything with health inside a sphere in front of the player.
        // The HashSet makes sure each enemy only gets hit once per swing,
        // even if the sphere touches several of its colliders
        var hit = new HashSet<HealthManager>();
        foreach (var c in Physics.OverlapSphere(centre, attackRange * 0.6f, ~0, QueryTriggerInteraction.Collide))
        {
            var hm = c.GetComponentInParent<HealthManager>();
            if (hm && !hm.transform.IsChildOf(Player) && hit.Add(hm))
                hm.ChangeHealth(-attackDamage);
        }
        if (hit.Count > 0) Shake(0.06f, 0.1f);   // tiny shake when you land a hit
    }

    // ---------- HUD ----------

    // the "what to do next" line under the hearts
    string Objective()
    {
        if (IsComplete) return "";
        if (!HasWeapon) return "Open the chest";
        if (!IsWellDrained) return "Find a way to drain the well";
        if (!IsBossDefeated) return "Defeat the Guardian";
        return "Climb out before the well floods";
    }

    // OnGUI draws simple on-screen UI with code (no Canvas needed)
    void OnGUI()
    {
        // damage flash + respawn fade
        if (Time.time < damageFlashUntil)
            DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.8f, 0f, 0f, 0.25f * (damageFlashUntil - Time.time) / 0.25f));
        if (fade > 0f)
            DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, fade));

        // s scales everything with the screen size, so the HUD looks the same at any resolution
        // (it was designed for a 1080 pixel tall screen)
        float s = Screen.height / 1080f;

        // hearts: a full heart, a half heart (outline) or an empty dot for each one
        string h = "";
        for (int i = 0; i < Mathf.CeilToInt(maxHearts); i++)
            h += hearts >= i + 1 ? "♥" : hearts > i ? "♡" : "·";
        Label(new Rect(28 * s, 18 * s, 600 * s, 70 * s), h, Mathf.RoundToInt(56 * s), new Color(0.85f, 0.12f, 0.12f), TextAnchor.UpperLeft);

        // objective
        string obj = Objective();
        if (obj != "")
            Label(new Rect(30 * s, 88 * s, 900 * s, 40 * s), obj, Mathf.RoundToInt(24 * s), new Color(0.92f, 0.86f, 0.7f), TextAnchor.UpperLeft);

        // boss bar, only shown during the boss fight
        if (IsWellDrained && !IsBossDefeated && bossHealth)
        {
            float w = 700 * s, x = (Screen.width - w) * 0.5f, y = Screen.height - 90 * s;
            Label(new Rect(x, y - 38 * s, w, 34 * s), "THE GUARDIAN", Mathf.RoundToInt(24 * s), new Color(0.92f, 0.86f, 0.7f), TextAnchor.LowerCenter);
            DrawRect(new Rect(x - 3, y - 3, w + 6, 22 * s + 6), new Color(0f, 0f, 0f, 0.7f));
            DrawRect(new Rect(x, y, w * Mathf.Clamp01(bossHealth.currentHealth / bossMaxHealth), 22 * s), new Color(0.65f, 0.08f, 0.08f));
        }

        // messages, they fade out during their last second
        if (!IsComplete && !string.IsNullOrEmpty(message) && Time.time < messageUntil)
        {
            float a = Mathf.Clamp01(messageUntil - Time.time);
            Label(new Rect(0, Screen.height * 0.18f, Screen.width, 60 * s), message, Mathf.RoundToInt(38 * s), new Color(1f, 0.95f, 0.85f, a), TextAnchor.MiddleCenter);
        }

        // end screen with your time, deaths and whether you found the relic
        if (IsComplete)
        {
            DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f));
            float t = endTime - startTime;
            Label(new Rect(0, Screen.height * 0.35f, Screen.width, 90 * s), "LEVEL COMPLETE", Mathf.RoundToInt(72 * s), new Color(1f, 0.85f, 0.5f), TextAnchor.MiddleCenter);
            string stats = string.Format("Time  {0}:{1:00}      Deaths  {2}      Relic  {3}",
                (int)(t / 60f), (int)(t % 60f), deaths, relicFound ? "found" : "missed");
            Label(new Rect(0, Screen.height * 0.35f + 100 * s, Screen.width, 50 * s), stats, Mathf.RoundToInt(30 * s), Color.white, TextAnchor.MiddleCenter);
        }
    }

    // draws a filled coloured rectangle on screen
    static void DrawRect(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    // text with a drop shadow so it reads over anything
    // (draws the text in black slightly offset first, then the real colour on top)
    static void Label(Rect r, string text, int size, Color color, TextAnchor anchor)
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = FontStyle.Bold, alignment = anchor, wordWrap = false };
        style.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.8f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
        style.normal.textColor = color;
        GUI.Label(r, text, style);
    }
}
