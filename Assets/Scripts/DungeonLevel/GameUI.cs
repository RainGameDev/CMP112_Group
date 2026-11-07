using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    // Colours
    readonly Color white = new(0.92f, 0.86f, 0.83f);
    readonly Color grey = new(0.52f, 0.48f, 0.49f);
    readonly Color rose = new(0.82f, 0.42f, 0.5f);
    readonly Color red = new(0.64f, 0.16f, 0.27f);

    // Anchors
    readonly Vector2 topLeft = new(0, 1), top = new(0.5f, 1), topRight = new(1, 1), right = new(1, 0.5f);
    readonly Vector2 centre = new(0.5f, 0.5f), bottom = new(0.5f, 0), bottomRight = new(1, 0);

    // References
    TMP_FontAsset font;
    PlayerMovement movement;
    EquipmentManager equipment;
    ContainerUI inventory;
    InteractionManager interaction;

    // UI parts
    Image health, dash, boss, weapon, flash, fade;
    TMP_Text healthText, stats, objective, message, results;
    CanvasGroup messageBox;
    GameObject objectiveBox, bossBar, deathScreen, completeScreen;

    // Builds the UI
    void Start()
    {
        // Find player
        font = Resources.Load<TMP_FontAsset>("Fonts/PsxLain/PsxLain TMP");
        GameObject player = GameObject.FindWithTag("Player");
        movement = player.GetComponent<PlayerMovement>();
        equipment = player.GetComponent<EquipmentManager>();
        inventory = player.GetComponentInChildren<ContainerUI>(true);
        interaction = player.GetComponentInChildren<InteractionManager>();

        // Make canvas
        GameObject canvas = new("GameUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder = -1;
        canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        Transform ui = canvas.transform;

        // Damage flash
        flash = Full(ui, null);

        // Top bar
        Transform bar = Img(ui, "ui_panel", topLeft, topRight, Vector2.zero, new Vector2(0, 84)).transform;
        Transform emblem = Img(bar, "ui_slot", topLeft, new Vector2(24, -16), new Vector2(52, 52)).transform;
        Full(emblem, "ui_portrait", 6);
        health = Bar(bar, topLeft, new Vector2(96, -18), new Vector2(420, 18), "ui_bar_health");
        Txt(bar, "Wanderer", 22, white, topLeft, new Vector2(96, -42), new Vector2(200, 28));
        healthText = Txt(bar, "", 22, grey, topLeft, new Vector2(316, -42), new Vector2(200, 28), TextAlignmentOptions.Right);
        dash = Bar(bar, topLeft, new Vector2(544, -22), new Vector2(110, 10), "ui_bar_dash");
        Txt(bar, "Dash", 18, grey, topLeft, new Vector2(544, -40), new Vector2(110, 24));
        Txt(Img(bar, "ui_band", centre, Vector2.zero, new Vector2(640, 84)).transform, "The Drowned Well", 28, white).characterSpacing = 8;
        stats = Txt(bar, "", 22, grey, right, new Vector2(-28, 0), new Vector2(500, 40), TextAlignmentOptions.Right);

        // Objective card
        objectiveBox = Img(ui, "ui_panel", topLeft, new Vector2(28, -104), new Vector2(560, 96)).gameObject;
        Txt(objectiveBox.transform, "Objective", 20, rose, topLeft, new Vector2(22, -14), new Vector2(400, 26)).characterSpacing = 8;
        objective = Txt(objectiveBox.transform, "", 28, white, topLeft, new Vector2(22, -46), new Vector2(520, 36));

        // Message area
        Image messageArea = Img(ui, null, top, new Vector2(0, -280), new Vector2(1700, 150), Color.clear);
        messageBox = messageArea.gameObject.AddComponent<CanvasGroup>();
        Img(messageArea.transform, "ui_divider", top, Vector2.zero, new Vector2(400, 22));
        message = Txt(messageArea.transform, "", 52, white, centre, Vector2.zero, new Vector2(1700, 70), TextAlignmentOptions.Center);
        Img(messageArea.transform, "ui_divider", bottom, Vector2.zero, new Vector2(400, 22));

        // Boss bar
        boss = Bar(ui, bottom, new Vector2(0, 56), new Vector2(1000, 20), "ui_bar_health");
        bossBar = boss.transform.parent.gameObject;
        Txt(bossBar.transform, "The Guardian", 30, white, top, new Vector2(0, 50), new Vector2(1000, 40), TextAlignmentOptions.Center).characterSpacing = 8;

        // Hotbar slots
        Transform weaponSlot = Img(ui, "ui_slot", bottomRight, new Vector2(-28, 64), new Vector2(88, 88)).transform;
        Full(weaponSlot, "ui_slot_selected");
        weapon = Full(weaponSlot, null, 12);
        weapon.preserveAspect = true;
        Key(weaponSlot, "LMB", "ui_key");
        Transform bagSlot = Img(ui, "ui_slot", bottomRight, new Vector2(-132, 64), new Vector2(88, 88)).transform;
        Txt(bagSlot, "=", 40, grey);
        Key(bagSlot, "TAB", "ui_button_secondary");

        // Screen fade
        fade = Full(ui, null);

        // Death screen
        deathScreen = Full(ui, null, 0, new Color(0, 0, 0, 0.85f)).gameObject;
        Transform band = Img(deathScreen.transform, "ui_band_dark", centre, Vector2.zero, new Vector2(1800, 300)).transform;
        Img(band, "ui_divider", top, new Vector2(0, -30), new Vector2(400, 22));
        Txt(band, "YOU DIED", 110, red, centre, new Vector2(0, 10), new Vector2(1800, 120), TextAlignmentOptions.Center).characterSpacing = 20;
        Txt(band, "Rising at the last checkpoint...", 28, grey, centre, new Vector2(0, -80), new Vector2(1800, 40), TextAlignmentOptions.Center);

        // Complete screen
        completeScreen = Full(ui, null, 0, new Color(0, 0, 0, 0.6f)).gameObject;
        Transform panel = Img(completeScreen.transform, "ui_panel", centre, Vector2.zero, new Vector2(820, 360)).transform;
        Txt(Img(panel, "ui_header", topLeft, topRight, Vector2.zero, new Vector2(0, 64)).transform, "Level complete", 34, white).characterSpacing = 8;
        Txt(panel, "You escaped the Drowned Well", 26, grey, top, new Vector2(0, -90), new Vector2(800, 36), TextAlignmentOptions.Center);
        results = Txt(panel, "", 28, white, top, new Vector2(0, -150), new Vector2(800, 40), TextAlignmentOptions.Center);
        Button(panel, "Play again", "ui_button_primary", new Vector2(-160, 40), () => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
        Button(panel, "Quit", "ui_button_secondary", new Vector2(160, 40), Application.Quit);

        // Hide screens
        deathScreen.SetActive(false);
        completeScreen.SetActive(false);
        StyleInventory();
    }

    // Updates values
    void Update()
    {
        LevelManager lm = LevelManager.Instance;
        if (!lm || !lm.PlayerHealth) return;

        // Player stats
        health.fillAmount = lm.PlayerHealth.currentHealth / lm.PlayerHealth.maxHealth;
        healthText.text = Mathf.CeilToInt(Mathf.Max(0, lm.PlayerHealth.currentHealth)) + " / " + Mathf.CeilToInt(lm.PlayerHealth.maxHealth);
        dash.fillAmount = 1f - movement.cooldownTimeLeft / movement.dashCooldown;
        stats.text = "Relic " + (lm.RelicFound ? 1 : 0) + " / 1    Deaths " + lm.Deaths;

        // Objective and messages
        objectiveBox.SetActive(!lm.IsComplete);
        objective.text = lm.Objective();
        message.text = lm.Message;
        messageBox.alpha = lm.IsComplete ? 0 : Mathf.Clamp01(lm.MessageEndTime - Time.time);

        // Boss health
        bossBar.SetActive(lm.IsWellDrained && !lm.IsBossDefeated && lm.BossHealth);
        if (lm.BossHealth) boss.fillAmount = lm.BossHealth.currentHealth / lm.BossMaxHealth;

        // Weapon icon
        weapon.enabled = equipment.mainHandItem != null;
        if (weapon.enabled) weapon.sprite = equipment.mainHandItem.itemIcon;

        // Flash and screens
        flash.color = new Color(0.6f, 0f, 0.05f, Mathf.Clamp01(lm.DamageFlashEndTime - Time.time));
        fade.color = new Color(0f, 0f, 0f, lm.Fade);
        deathScreen.SetActive(Time.time < lm.DiedAt + 1.5f);
        completeScreen.SetActive(lm.IsComplete);
        results.text = "Time " + (int)(lm.PlayTime / 60) + ":" + ((int)(lm.PlayTime % 60)).ToString("00") + "      Deaths " + lm.Deaths + "      Relic " + (lm.RelicFound ? "found" : "missed");

        // Interact prompts
        foreach (Interactable target in interaction.interactables) StylePrompt(target);
    }

    // Inventory style
    void StyleInventory()
    {
        Image panel = inventory.containerUI.GetComponent<Image>();
        SetSprite(panel, "ui_panel");
        SetSprite(panel.transform.Find("TopBar").GetComponent<Image>(), "ui_header");
        SetFont(panel.transform);

        // Scrollbar
        Scrollbar scrollbar = panel.GetComponentInChildren<Scrollbar>(true);
        SetSprite(scrollbar.GetComponent<Image>(), "ui_bar_back");
        scrollbar.handleRect.GetComponent<Image>().color = new Color(0.24f, 0.07f, 0.11f);

        // New slots
        inventory.container.OnItemAdded += item =>
        {
            Transform slot = inventory.itemSlots[^1].transform;
            SetSprite(slot.Find("Background").GetComponent<Image>(), "ui_slot");
            SetSprite(slot.Find("EquippedIcon").GetComponent<Image>(), "ui_slot_selected");
            SetSprite(slot.Find("DeleteButton").GetComponent<Image>(), "ui_key");
            slot.Find("Foreground").GetComponent<Image>().color = Color.clear;
            RectTransform equipped = (RectTransform)slot.Find("EquippedIcon");
            equipped.anchoredPosition = Vector2.zero;
            equipped.sizeDelta = Vector2.zero;
            SetFont(slot);
        };
    }

    // Interact prompt
    void StylePrompt(Interactable target)
    {
        if (!target || !target.interactionUI) return;
        Transform prompt = target.interactionUI.transform;
        if (prompt.Find("ui_band_dark")) return;

        Full(prompt, "ui_band_dark").transform.SetAsFirstSibling();
        foreach (TMP_Text t in prompt.GetComponentsInChildren<TMP_Text>(true))
        {
            t.font = font;
            t.color = white;
            t.text = t.text.Replace("<color=Green>", "<color=#D16B80>").Replace("`", "");
        }
    }

    // Anchored image
    Image Img(Transform parent, string sprite, Vector2 anchor, Vector2 position, Vector2 size, Color? color = null)
    {
        return Img(parent, sprite, anchor, anchor, position, size, color);
    }

    // Stretched image
    Image Img(Transform parent, string sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color? color = null)
    {
        Image image = new GameObject(sprite ?? "Image", typeof(Image)).GetComponent<Image>();
        RectTransform rt = image.rectTransform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = (anchorMin + anchorMax) / 2;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        image.raycastTarget = false;
        if (sprite != null) SetSprite(image, sprite);
        image.color = color ?? Color.white;
        return image;
    }

    // Fill parent
    Image Full(Transform parent, string sprite, float padding = 0, Color? color = null)
    {
        return Img(parent, sprite, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-padding * 2, -padding * 2), color);
    }

    // Text filling parent
    TMP_Text Txt(Transform parent, string text, float size, Color color)
    {
        TMP_Text label = Txt(parent, text, size, color, centre, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        return label;
    }

    // Placed text
    TMP_Text Txt(Transform parent, string text, float size, Color color, Vector2 anchor, Vector2 position, Vector2 box, TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        TMP_Text label = new GameObject("Text", typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        RectTransform rt = label.rectTransform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = box;
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = align;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    // Bar fill
    Image Bar(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string sprite)
    {
        Image back = Img(parent, "ui_bar_back", anchor, position, size);
        Image fill = Full(back.transform, sprite, 2);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        return fill;
    }

    // Key label
    void Key(Transform slot, string text, string sprite)
    {
        Txt(Img(slot, sprite, bottom, new Vector2(0, -36), new Vector2(64, 28)).transform, text, 18, white);
    }

    // Clickable button
    void Button(Transform parent, string text, string sprite, Vector2 position, UnityAction onClick)
    {
        Image image = Img(parent, sprite, bottom, position, new Vector2(300, 60));
        image.raycastTarget = true;
        image.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
        Txt(image.transform, text, 26, white).characterSpacing = 6;
    }

    // Apply sprite
    void SetSprite(Image image, string sprite)
    {
        image.sprite = Resources.Load<Sprite>("UI/" + sprite);
        image.type = sprite == "ui_panel" ? Image.Type.Tiled : Image.Type.Sliced;
        image.color = Color.white;
    }

    // Apply font
    void SetFont(Transform parent)
    {
        foreach (TMP_Text t in parent.GetComponentsInChildren<TMP_Text>(true))
        {
            t.font = font;
            t.color = white;
        }
    }
}
