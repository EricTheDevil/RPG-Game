using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using RPG.Data;
using RPG.UI;
using RPG.Core;

/// <summary>
/// One-click setup: creates all EventCardSO assets, DropTable assets,
/// wires the GameManager prefab, and builds the EventDeck scene.
///
/// Run via  RPG > Create → Event Cards + Deck Scene
/// </summary>
public class CreateEventCards
{
    const string CardsDir     = "Assets/_Project/ScriptableObjects/Cards";
    const string TablesDir    = "Assets/_Project/ScriptableObjects/DropTables";
    const string ScenesDir    = "Assets/_Project/Scenes";

    [MenuItem("RPG/Create → Event Cards + Deck Scene")]
    public static void Execute()
    {
        EnsureFolders();
        var cards = CreateAllCards();
        var tables = CreateDropTables(cards);
        BuildEventDeckScene(cards);
        PatchGameManagerPrefab(cards, tables);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CreateEventCards] ✅ Cards, drop tables, and EventDeck scene ready.");
    }

    // ── Folder setup ─────────────────────────────────────────────────────────
    static void EnsureFolders()
    {
        CreateFolder("Assets/_Project/ScriptableObjects", "Cards");
        CreateFolder("Assets/_Project/ScriptableObjects", "DropTables");
    }

    static void CreateFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
            AssetDatabase.CreateFolder(parent, name);
    }

    // ── Card creation ─────────────────────────────────────────────────────────
    static Dictionary<string, EventCardSO> CreateAllCards()
    {
        var d = new Dictionary<string, EventCardSO>();

        // ── COMBAT cards (Common – most frequent) ──────────────────────────
        d["combat_skirmish"] = Card("Card_Combat_Skirmish",
            "Skirmish",
            "A routine patrol crosses your path. Quick fight, modest reward.",
            CardType.Combat, CardRarity.Common,
            typeLabel: "COMBAT",
            accent: new Color(0.85f, 0.3f, 0.15f),
            grant: new ResourceDelta { Gold = 15, Scrap = 5 });

        d["combat_ambush"] = Card("Card_Combat_Ambush",
            "Ambush",
            "You stumble into an enemy trap. Fight your way out.",
            CardType.Combat, CardRarity.Common,
            typeLabel: "COMBAT",
            accent: new Color(0.9f, 0.25f, 0.1f),
            grant: new ResourceDelta { Gold = 20, Scrap = 8 },
            difficultyScale: 1.15f);

        d["combat_bounty"] = Card("Card_Combat_Bounty",
            "Bounty Hunt",
            "A marked target nearby. Defeat them for extra gold.",
            CardType.Combat, CardRarity.Uncommon,
            typeLabel: "COMBAT",
            accent: new Color(1f, 0.6f, 0.1f),
            grant: new ResourceDelta { Gold = 40, Scrap = 10 },
            difficultyScale: 1.25f);

        // ── ELITE cards (Rare – hard fight, great reward) ──────────────────
        d["elite_knight"] = Card("Card_Elite_DarkKnight",
            "Dark Champion",
            "A veteran Dark Knight challenges you. Victory yields rare spoils.",
            CardType.Elite, CardRarity.Rare,
            typeLabel: "ELITE",
            accent: new Color(0.5f, 0.1f, 0.8f),
            grant: new ResourceDelta { Gold = 80, Scrap = 25, Morale = 1 },
            difficultyScale: 1.6f);

        d["elite_warband"] = Card("Card_Elite_Warband",
            "Warband",
            "Three enemies attack at once. Overwhelming odds — but glorious loot.",
            CardType.Elite, CardRarity.Rare,
            typeLabel: "ELITE",
            accent: new Color(0.7f, 0.1f, 0.6f),
            grant: new ResourceDelta { Gold = 100, Scrap = 30 },
            difficultyScale: 1.8f);

        // ── HEAL cards (Common–Uncommon) ───────────────────────────────────
        d["heal_bandage"] = Card("Card_Heal_Bandage",
            "Field Dressing",
            "Tend to your wounds. Recover 20% of max HP.",
            CardType.Heal, CardRarity.Common,
            typeLabel: "HEAL",
            accent: new Color(0.2f, 0.85f, 0.4f),
            healFraction: 0.20f);

        d["heal_rations"] = Card("Card_Heal_Rations",
            "Hot Meal",
            "A warm meal restores body and spirit. Recover 35% HP, gain 1 Ration.",
            CardType.Heal, CardRarity.Common,
            typeLabel: "HEAL",
            accent: new Color(0.3f, 0.9f, 0.5f),
            healFraction: 0.35f,
            cost: new ResourceDelta { Rations = -1 },
            grant: new ResourceDelta { Rations = 1 });

        d["heal_potion"] = Card("Card_Heal_Potion",
            "Healing Potion",
            "A rare alchemical brew. Fully restore HP.",
            CardType.Heal, CardRarity.Uncommon,
            typeLabel: "HEAL",
            accent: new Color(0.1f, 1f, 0.5f),
            healFraction: 1.0f,
            cost: new ResourceDelta { Gold = -30 });

        // ── REST cards (Common) ────────────────────────────────────────────
        d["rest_campfire"] = Card("Card_Rest_Campfire",
            "Campfire",
            "Rest around a fire. Recover 15% HP and gain +1 Morale.",
            CardType.Rest, CardRarity.Common,
            typeLabel: "REST",
            accent: new Color(1f, 0.6f, 0.2f),
            healFraction: 0.15f,
            moraleBonus: 1);

        d["rest_meditation"] = Card("Card_Rest_Meditation",
            "Meditation",
            "Clear your mind. Gain +2 Morale. No HP recovery.",
            CardType.Rest, CardRarity.Uncommon,
            typeLabel: "REST",
            accent: new Color(0.6f, 0.7f, 1f),
            moraleBonus: 2);

        // ── SHOP cards (Uncommon) ──────────────────────────────────────────
        d["shop_merchant"] = Card("Card_Shop_Merchant",
            "Wandering Merchant",
            "A trader appears. Browse 3 buffs for sale.",
            CardType.Shop, CardRarity.Uncommon,
            typeLabel: "SHOP",
            accent: new Color(1f, 0.8f, 0.1f),
            shopOfferCount: 3);

        d["shop_blackmarket"] = Card("Card_Shop_BlackMarket",
            "Black Market",
            "Questionable goods at steep prices. Browse 4 rare buffs.",
            CardType.Shop, CardRarity.Rare,
            typeLabel: "SHOP",
            accent: new Color(0.7f, 0.6f, 0.1f),
            shopOfferCount: 4,
            cost: new ResourceDelta { Morale = -1 });

        // ── TREASURE cards (Rare) ──────────────────────────────────────────
        d["treasure_cache"] = Card("Card_Treasure_Cache",
            "Supply Cache",
            "Stumble upon an abandoned supply cache. Gain Gold, Scrap, and Rations.",
            CardType.Treasure, CardRarity.Rare,
            typeLabel: "TREASURE",
            accent: new Color(1f, 0.85f, 0.1f),
            grant: new ResourceDelta { Gold = 50, Scrap = 20, Rations = 2 });

        d["treasure_relic"] = Card("Card_Treasure_Relic",
            "Ancient Relic",
            "A powerful artefact of unknown origin. Sell for great wealth.",
            CardType.Treasure, CardRarity.Epic,
            typeLabel: "TREASURE",
            accent: new Color(0.9f, 0.75f, 0.1f),
            grant: new ResourceDelta { Gold = 120, Morale = 1 });

        // ── LEVEL-UP cards (Epic / Legendary — very rare) ─────────────────
        d["levelup_power"] = Card("Card_LevelUp_Power",
            "Power Surge",
            "A surge of ancient power flows through you. Gain +1 stat point.",
            CardType.LevelUp, CardRarity.Epic,
            typeLabel: "LEVEL UP",
            accent: new Color(0.6f, 0.2f, 1f),
            statPointGrant: 1);

        d["levelup_ascend"] = Card("Card_LevelUp_Ascend",
            "Ascension",
            "Your potential awakens fully. Gain +3 stat points.",
            CardType.LevelUp, CardRarity.Legendary,
            typeLabel: "LEVEL UP",
            accent: new Color(1f, 0.9f, 0.2f),
            statPointGrant: 3,
            canDuplicate: false);

        // ── CURSE cards (can duplicate — forced into deck by Elite defeats) ─
        d["curse_haunted"] = Card("Card_Curse_Haunted",
            "Haunted",
            "A dark presence saps your will. -1 Morale each time drawn.",
            CardType.Curse, CardRarity.Uncommon,
            typeLabel: "CURSE",
            accent: new Color(0.4f, 0.1f, 0.5f),
            cost: new ResourceDelta { Morale = -1 },
            canDuplicate: true,
            singleUse: false);

        d["curse_debt"] = Card("Card_Curse_Debt",
            "Blood Debt",
            "You owe a dangerous sum. -20 Gold when played.",
            CardType.Curse, CardRarity.Uncommon,
            typeLabel: "CURSE",
            accent: new Color(0.6f, 0.1f, 0.3f),
            cost: new ResourceDelta { Gold = -20 },
            canDuplicate: true,
            singleUse: true);

        return d;
    }

    static EventCardSO Card(
        string assetName,
        string cardName,
        string desc,
        CardType type,
        CardRarity rarity,
        string typeLabel        = "",
        Color  accent           = default,
        ResourceDelta cost      = null,
        ResourceDelta grant     = null,
        float healFraction      = 0f,
        int   moraleBonus       = 0,
        int   shopOfferCount    = 3,
        int   statPointGrant    = 0,
        float difficultyScale   = 1f,
        bool  canDuplicate      = false,
        bool  singleUse         = true)
    {
        string path = $"{CardsDir}/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<EventCardSO>(path);
        if (existing != null) return existing;   // idempotent

        var so = ScriptableObject.CreateInstance<EventCardSO>();
        so.CardName       = cardName;
        so.Description    = desc;
        so.Type           = type;
        so.Rarity         = rarity;
        so.TypeLabel      = string.IsNullOrEmpty(typeLabel) ? type.ToString().ToUpper() : typeLabel;
        so.AccentColor    = accent == default ? Color.white : accent;
        so.ResourceCost   = cost  ?? new ResourceDelta();
        so.ResourceGrant  = grant ?? new ResourceDelta();
        so.HealFraction   = healFraction;
        so.MoraleBonus    = moraleBonus;
        so.ShopOfferCount = shopOfferCount;
        so.StatPointGrant = statPointGrant;
        so.DifficultyScale= difficultyScale;
        so.CanDuplicate   = canDuplicate;
        so.SingleUse      = singleUse;

        AssetDatabase.CreateAsset(so, path);
        EditorUtility.SetDirty(so);
        return so;
    }

    // ── Drop tables ───────────────────────────────────────────────────────────
    static Dictionary<string, DropTableSO> CreateDropTables(Dictionary<string, EventCardSO> cards)
    {
        var tables = new Dictionary<string, DropTableSO>();

        // ── Tier 1 — standard encounters (early game) ──────────────────────
        tables["tier1"] = Table("DropTable_Tier1", cardsPerRoll: 2, entries: new[]
        {
            Entry(cards["combat_skirmish"],  weightOverride: 0),   // weight from rarity (100)
            Entry(cards["combat_ambush"],    weightOverride: 0),   // 100
            Entry(cards["heal_bandage"],     weightOverride: 60),
            Entry(cards["heal_rations"],     weightOverride: 50),
            Entry(cards["rest_campfire"],    weightOverride: 55),
            Entry(cards["shop_merchant"],    weightOverride: 25),  // Uncommon base = 40, nudge down
            Entry(cards["combat_bounty"],    weightOverride: 0),   // 40
            Entry(cards["treasure_cache"],   weightOverride: 8),   // Rare base = 12, nudge down
            Entry(cards["elite_knight"],     weightOverride: 4),   // Rare, harder
            Entry(cards["levelup_power"],    weightOverride: 2),   // Epic, very rare
        });

        // ── Elite table — rolls after defeating an Elite enemy ────────────
        tables["elite"] = Table("DropTable_Elite", cardsPerRoll: 3, entries: new[]
        {
            Entry(cards["elite_knight"],     weightOverride: 0),
            Entry(cards["elite_warband"],    weightOverride: 0),
            Entry(cards["treasure_cache"],   weightOverride: 30),
            Entry(cards["treasure_relic"],   weightOverride: 10),
            Entry(cards["heal_potion"],      weightOverride: 20),
            Entry(cards["shop_blackmarket"], weightOverride: 15),
            Entry(cards["rest_meditation"],  weightOverride: 20),
            Entry(cards["levelup_power"],    weightOverride: 8),
            Entry(cards["levelup_ascend"],   weightOverride: 1),  // Legendary
        });

        // ── Starter hand table — what the player begins a run with ────────
        // (Not rolled; used by GameManager.StarterCards array directly)

        return tables;
    }

    static DropTableSO Table(string assetName, int cardsPerRoll, (EventCardSO card, int weight, int guaranteed)[] entries)
    {
        string path = $"{TablesDir}/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<DropTableSO>(path);
        if (existing != null) return existing;

        var so = ScriptableObject.CreateInstance<DropTableSO>();
        so.CardsPerRoll = cardsPerRoll;
        so.Entries      = new System.Collections.Generic.List<DropEntry>();

        foreach (var (card, weight, guaranteed) in entries)
        {
            if (card == null) continue;
            so.Entries.Add(new DropEntry
            {
                Card           = card,
                WeightOverride = weight,
                MinGuaranteed  = guaranteed,
            });
        }

        AssetDatabase.CreateAsset(so, path);
        EditorUtility.SetDirty(so);
        return so;
    }

    static (EventCardSO card, int weight, int guaranteed) Entry(EventCardSO card, int weightOverride = 0, int guaranteed = 0)
        => (card, weightOverride, guaranteed);

    // ── EventDeck scene ───────────────────────────────────────────────────────
    static void BuildEventDeckScene(Dictionary<string, EventCardSO> cards)
    {
        string scenePath = $"{ScenesDir}/EventDeck.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ─────────────────────────────────────────────────────────
        var camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        var cam   = camGo.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.04f, 0.09f);
        camGo.transform.position = new Vector3(0, 0, -10);

        // ── Event System ───────────────────────────────────────────────────
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ── SceneBootstrap ─────────────────────────────────────────────────
        var bootstrapGo   = new GameObject("SceneBootstrap");
        var bootstrap     = bootstrapGo.AddComponent<SceneBootstrap>();
        var gmPrefab      = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/GameManager.prefab");
        if (gmPrefab != null)
        {
            var so = new SerializedObject(bootstrap);
            var prop = so.FindProperty("GameManagerPrefab");
            if (prop != null) { prop.objectReferenceValue = gmPrefab.GetComponent<GameManager>(); so.ApplyModifiedProperties(); }
        }

        // ── Root Canvas ────────────────────────────────────────────────────
        var canvasGo = new GameObject("UICanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Background ─────────────────────────────────────────────────────
        MakePanel(canvasGo.transform, "Background",
            new Color(0.05f, 0.04f, 0.09f, 1f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // ── Title ──────────────────────────────────────────────────────────
        var titleGo  = new GameObject("Title");
        titleGo.transform.SetParent(canvasGo.transform, false);
        var titleTMP = titleGo.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "YOUR HAND";
        titleTMP.fontSize  = 42f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = new Color(1f, 0.84f, 0.1f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f); titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot     = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);
        titleRect.sizeDelta = new Vector2(0f, 55f);

        // ── Resource Bar ───────────────────────────────────────────────────
        var resBar = MakePanel(canvasGo.transform, "ResourceBar",
            new Color(0.08f, 0.07f, 0.14f, 0.95f),
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -75f), new Vector2(0f, 44f));
        resBar.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);

        var resLayout = resBar.AddComponent<HorizontalLayoutGroup>();
        resLayout.spacing = 0f;
        resLayout.childForceExpandWidth = true;
        resLayout.childForceExpandHeight = true;
        resLayout.padding = new RectOffset(24, 24, 6, 6);

        var goldTMP    = MakeResLabel(resBar.transform, "GoldText",    "Gold  0",    new Color(1f, 0.84f, 0.1f));
        var scrapTMP   = MakeResLabel(resBar.transform, "ScrapText",   "Scrap  0",   new Color(0.7f, 0.7f, 0.7f));
        var rationsTMP = MakeResLabel(resBar.transform, "RationsText", "Rations  3/10", new Color(0.4f, 1f, 0.5f));
        var moraleTMP  = MakeResLabel(resBar.transform, "MoraleText",  "Morale  5/10",  new Color(0.4f, 0.7f, 1f));

        // ── Drop Banner ────────────────────────────────────────────────────
        var dropBannerGo = new GameObject("DropBanner");
        dropBannerGo.transform.SetParent(canvasGo.transform, false);
        var dbRect = dropBannerGo.AddComponent<RectTransform>();
        dbRect.anchorMin = new Vector2(0.5f, 1f); dbRect.anchorMax = new Vector2(0.5f, 1f);
        dbRect.pivot     = new Vector2(0.5f, 1f);
        dbRect.anchoredPosition = new Vector2(0f, -120f);
        dbRect.sizeDelta = new Vector2(800f, 50f);
        var dbBg = dropBannerGo.AddComponent<Image>();
        dbBg.color = new Color(0.1f, 0.35f, 0.1f, 0.95f);
        var dbTMP = MakeTextFull(dropBannerGo.transform, "DropBannerText",
            "Cards acquired!", 22f, new Color(0.5f, 1f, 0.5f));
        dropBannerGo.SetActive(false);

        // ── Card Container (scrollable horizontal) ─────────────────────────
        var scrollGo = new GameObject("HandScroll");
        scrollGo.transform.SetParent(canvasGo.transform, false);
        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical   = false;
        var scrollRectT = scrollGo.GetComponent<RectTransform>();
        scrollRectT.anchorMin = new Vector2(0f, 0.1f); scrollRectT.anchorMax = new Vector2(0.72f, 0.88f);
        scrollRectT.offsetMin = new Vector2(16f, 0f);  scrollRectT.offsetMax = new Vector2(-8f, -120f);

        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var viewport  = viewportGo.AddComponent<RectTransform>();
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero; viewport.offsetMax = Vector2.zero;
        viewportGo.AddComponent<Image>().color = new Color(0,0,0,0.01f);
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewport;

        var contentGo = new GameObject("CardContainer");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f); contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot     = new Vector2(0f, 0.5f);
        contentRect.offsetMin = Vector2.zero; contentRect.offsetMax = Vector2.zero;
        var hlg = contentGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 18f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(16, 16, 0, 0);
        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRect;

        // ── Card Prefab ────────────────────────────────────────────────────
        var cardPrefab = BuildCardPrefab();

        // ── Detail Panel (right side) ──────────────────────────────────────
        var detailPanel = MakePanel(canvasGo.transform, "DetailPanel",
            new Color(0.08f, 0.07f, 0.14f, 0.97f),
            new Vector2(0.72f, 0.1f), new Vector2(1f, 0.88f),
            new Vector2(0f, -60f), new Vector2(0f, 0f));
        detailPanel.GetComponent<RectTransform>().offsetMin = new Vector2(8f, 0f);
        detailPanel.GetComponent<RectTransform>().offsetMax = new Vector2(-16f, -120f);
        detailPanel.SetActive(false);

        var detailName   = MakeText(detailPanel.transform, "DetailName",   "Card Name", 32f, Color.white,             new Vector2(0f, 110f),  new Vector2(320f, 42f));
        var detailRarity = MakeText(detailPanel.transform, "DetailRarity", "COMMON",    18f, new Color(0.6f,0.6f,0.7f), new Vector2(0f, 78f),   new Vector2(320f, 28f));
        var detailDesc   = MakeText(detailPanel.transform, "DetailDesc",   "",          20f, new Color(0.85f,0.85f,0.9f), new Vector2(0f, 10f),  new Vector2(300f, 120f));
        detailDesc.alignment = TextAlignmentOptions.TopLeft;
        detailDesc.textWrappingMode = TMPro.TextWrappingModes.Normal;
        var detailCost   = MakeText(detailPanel.transform, "DetailCost",   "",          16f, new Color(1f,0.5f,0.4f),   new Vector2(0f, -70f),  new Vector2(300f, 30f));
        var detailGrant  = MakeText(detailPanel.transform, "DetailGrant",  "",          16f, new Color(0.4f,1f,0.6f),   new Vector2(0f, -100f), new Vector2(300f, 30f));

        // ── Empty hand message ─────────────────────────────────────────────
        var emptyMsgGo = new GameObject("EmptyHandMessage");
        emptyMsgGo.transform.SetParent(canvasGo.transform, false);
        var emptyRect = emptyMsgGo.AddComponent<RectTransform>();
        emptyRect.anchorMin = new Vector2(0f, 0.1f); emptyRect.anchorMax = new Vector2(0.72f, 0.88f);
        emptyRect.offsetMin = new Vector2(16f, 0f);  emptyRect.offsetMax = new Vector2(-8f, -120f);
        var emptyTMP = emptyMsgGo.AddComponent<TextMeshProUGUI>();
        emptyTMP.text      = "Your hand is empty.\nPlay a card to begin your next encounter.";
        emptyTMP.fontSize  = 28f;
        emptyTMP.color     = new Color(0.5f, 0.5f, 0.6f);
        emptyTMP.alignment = TextAlignmentOptions.Center;
        emptyMsgGo.SetActive(false);

        // ── Bottom buttons ─────────────────────────────────────────────────
        var playBtn    = MakeButton(canvasGo.transform, "PlayButton",    "Play Selected Card",
            new Color(0.5f, 0.35f, 0.05f), Color.white,
            new Vector2(-220f, 36f), new Vector2(320f, 55f),
            anchor: new Vector2(0.5f, 0f));
        playBtn.interactable = false;

        var discardBtn = MakeButton(canvasGo.transform, "DiscardButton", "Discard",
            new Color(0.35f, 0.15f, 0.15f), Color.white,
            new Vector2(90f, 36f), new Vector2(160f, 55f),
            anchor: new Vector2(0.5f, 0f));

        var menuBtn = MakeButton(canvasGo.transform, "MainMenuButton", "Main Menu",
            new Color(0.18f, 0.15f, 0.25f), new Color(0.7f, 0.7f, 0.8f),
            new Vector2(280f, 36f), new Vector2(160f, 55f),
            anchor: new Vector2(0.5f, 0f));

        // ── EventDeckUI component ──────────────────────────────────────────
        var deckUI              = canvasGo.AddComponent<EventDeckUI>();
        deckUI.CardContainer    = contentRect;
        deckUI.CardPrefab       = cardPrefab;
        deckUI.HandScrollRect   = scrollRect;
        deckUI.GoldText         = goldTMP;
        deckUI.ScrapText        = scrapTMP;
        deckUI.RationsText      = rationsTMP;
        deckUI.MoraleText       = moraleTMP;
        deckUI.DetailPanel      = detailPanel;
        deckUI.DetailName       = detailName;
        deckUI.DetailDesc       = detailDesc;
        deckUI.DetailRarity     = detailRarity;
        deckUI.DetailCost       = detailCost;
        deckUI.DetailGrant      = detailGrant;
        deckUI.DropBanner       = dropBannerGo;
        deckUI.DropBannerText   = dbTMP;
        deckUI.PlayButton       = playBtn;
        deckUI.PlayButtonLabel  = playBtn.GetComponentInChildren<TextMeshProUGUI>();
        deckUI.MainMenuButton   = menuBtn;
        deckUI.DiscardButton    = discardBtn;
        deckUI.EmptyHandMessage = emptyMsgGo;

        AddSceneToBuild(scenePath);
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[CreateEventCards] EventDeck scene built.");
    }

    static GameObject BuildCardPrefab()
    {
        string prefabPath = "Assets/_Project/Prefabs/UI/EventCard.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null) return existing;

        // Card root — 200 × 300
        var root = new GameObject("EventCard");
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(200f, 300f);

        // Background
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.09f, 0.17f, 0.97f);

        var rootCG = root.AddComponent<CanvasGroup>();

        // Frame (coloured border — rarity tint applied at runtime)
        var frameGo = new GameObject("Frame");
        frameGo.transform.SetParent(root.transform, false);
        var frameImg = frameGo.AddComponent<Image>();
        frameImg.color = new Color(0.55f, 0.55f, 0.60f);
        var frameRect = frameGo.GetComponent<RectTransform>();
        frameRect.anchorMin = Vector2.zero; frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = new Vector2(-3f, -3f); frameRect.offsetMax = new Vector2(3f, 3f);

        // Art area (top 45%)
        var artGo = new GameObject("CardArt");
        artGo.transform.SetParent(root.transform, false);
        var artImg = artGo.AddComponent<Image>();
        artImg.color = new Color(0.15f, 0.13f, 0.22f);
        artImg.preserveAspect = true;
        var artRect = artGo.GetComponent<RectTransform>();
        artRect.anchorMin = new Vector2(0f, 0.5f); artRect.anchorMax = Vector2.one;
        artRect.offsetMin = new Vector2(6f, 4f);   artRect.offsetMax = new Vector2(-6f, -28f);

        // Type badge (top-right corner)
        var badgeGo = new GameObject("TypeBadge");
        badgeGo.transform.SetParent(root.transform, false);
        var badgeBg = badgeGo.AddComponent<Image>();
        badgeBg.color = new Color(0.12f, 0.10f, 0.20f, 0.92f);
        var badgeRect = badgeGo.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1f, 1f); badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot     = new Vector2(1f, 1f);
        badgeRect.anchoredPosition = new Vector2(-4f, -4f);
        badgeRect.sizeDelta = new Vector2(90f, 22f);
        var typeLabelTMP = MakeTextFull(badgeGo.transform, "TypeLabel", "COMBAT", 11f, new Color(1f, 0.8f, 0.3f));
        typeLabelTMP.alignment = TextAlignmentOptions.Center;
        typeLabelTMP.fontStyle = FontStyles.Bold;

        // Card name
        var nameTMP = MakeTextFull(root.transform, "CardName", "Card Name", 18f, Color.white);
        var nameRect = nameTMP.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.46f); nameRect.anchorMax = new Vector2(1f, 0.55f);
        nameRect.offsetMin = new Vector2(8f, 0f);    nameRect.offsetMax = new Vector2(-8f, 0f);
        nameTMP.alignment  = TextAlignmentOptions.Center;
        nameTMP.fontStyle  = FontStyles.Bold;

        // Description
        var descTMP = MakeTextFull(root.transform, "Description", "Card description here.", 13f, new Color(0.8f, 0.8f, 0.9f));
        var descRect = descTMP.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 0.12f); descRect.anchorMax = new Vector2(1f, 0.46f);
        descRect.offsetMin = new Vector2(10f, 4f);   descRect.offsetMax = new Vector2(-10f, -4f);
        descTMP.alignment  = TextAlignmentOptions.TopLeft;
        descTMP.textWrappingMode = TMPro.TextWrappingModes.Normal;
        descTMP.overflowMode = TextOverflowModes.Truncate;

        // Rarity label (bottom)
        var rarityTMP = MakeTextFull(root.transform, "RarityLabel", "COMMON", 11f, new Color(0.55f, 0.55f, 0.60f));
        var rarityRect = rarityTMP.GetComponent<RectTransform>();
        rarityRect.anchorMin = new Vector2(0f, 0f); rarityRect.anchorMax = new Vector2(0.5f, 0.13f);
        rarityRect.offsetMin = new Vector2(8f, 4f); rarityRect.offsetMax = new Vector2(0f, -2f);
        rarityTMP.alignment  = TextAlignmentOptions.BottomLeft;
        rarityTMP.fontStyle  = FontStyles.Bold;

        // Cost label (bottom right)
        var costTMP = MakeTextFull(root.transform, "CostLabel", "", 11f, new Color(1f, 0.5f, 0.4f));
        var costRect = costTMP.GetComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0.5f, 0f); costRect.anchorMax = new Vector2(1f, 0.13f);
        costRect.offsetMin = new Vector2(0f, 4f);   costRect.offsetMax = new Vector2(-8f, -2f);
        costTMP.alignment  = TextAlignmentOptions.BottomRight;

        // Wire EventCardUI
        var cardUI       = root.AddComponent<EventCardUI>();
        cardUI.CardArt   = artImg;
        cardUI.CardFrame = frameImg;
        cardUI.TypeLabel = typeLabelTMP;
        cardUI.CardNameText    = nameTMP;
        cardUI.DescriptionText = descTMP;
        cardUI.RarityLabel     = rarityTMP;
        cardUI.CostLabel       = costTMP;
        cardUI.CanvasGroup     = rootCG;

        // Save prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        Debug.Log("[CreateEventCards] EventCard prefab saved.");
        return prefab;
    }

    // ── Patch GameManager prefab ──────────────────────────────────────────────
    static void PatchGameManagerPrefab(Dictionary<string, EventCardSO> cards,
                                       Dictionary<string, DropTableSO> tables)
    {
        var prefabPath = "Assets/_Project/Prefabs/GameManager.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogWarning("[CreateEventCards] GameManager prefab not found."); return; }

        var gm = prefab.GetComponent<GameManager>();
        if (gm == null) { Debug.LogWarning("[CreateEventCards] GameManager component not found on prefab."); return; }

        // Starter hand: 2 Combat + 1 Heal + 1 Rest
        gm.StarterCards = new[]
        {
            cards["combat_skirmish"],
            cards["combat_ambush"],
            cards["heal_bandage"],
            cards["rest_campfire"],
        };

        gm.DefaultDropTable  = tables["tier1"];
        gm.EventDeckScene    = "EventDeck";
        gm.MainMenuScene     = "MainMenu";
        gm.CombatScene       = "CombatStage";

        // Create or update EventCardRegistry asset
        const string registryPath = "Assets/_Project/ScriptableObjects/EventCardRegistry.asset";
        var registry = AssetDatabase.LoadAssetAtPath<RPG.Data.EventCardRegistry>(registryPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<RPG.Data.EventCardRegistry>();
            AssetDatabase.CreateAsset(registry, registryPath);
        }
        registry.Cards = new RPG.Data.EventCardSO[cards.Count];
        int i = 0;
        foreach (var kv in cards) registry.Cards[i++] = kv.Value;
        EditorUtility.SetDirty(registry);

        gm.CardRegistry = registry;

        EditorUtility.SetDirty(prefab);
        PrefabUtility.SavePrefabAsset(prefab);
        Debug.Log("[CreateEventCards] GameManager prefab patched with starter cards + drop table + card registry.");
    }

    // ── UI helpers ────────────────────────────────────────────────────────────
    static GameObject MakePanel(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax;
        r.pivot     = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = anchoredPos; r.sizeDelta = size;
        return go;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text,
        float size, Color color, Vector2 pos, Vector2 sizeDelta)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0.5f); r.anchorMax = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = sizeDelta;
        return tmp;
    }

    static TextMeshProUGUI MakeTextFull(Transform parent, string name, string text,
        float size, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        return tmp;
    }

    static TextMeshProUGUI MakeResLabel(Transform parent, string name, string text, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 20f; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        go.AddComponent<LayoutElement>().flexibleWidth = 1f;
        return tmp;
    }

    static Button MakeButton(Transform parent, string name, string label,
        Color bg, Color textColor, Vector2 pos, Vector2 size,
        Vector2 anchor = default)
    {
        if (anchor == default) anchor = new Vector2(0.5f, 0.5f);
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>(); img.color = bg;
        var btn = go.AddComponent<Button>();
        var cols = btn.colors;
        cols.highlightedColor = bg * 1.25f; cols.pressedColor = bg * 0.7f;
        btn.colors = cols; btn.targetGraphic = img;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = anchor; r.anchorMax = anchor;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;
        var lgo  = new GameObject("Label");
        lgo.transform.SetParent(go.transform, false);
        var tmp  = lgo.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 20f; tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center; tmp.fontStyle = FontStyles.Bold;
        var lr = lgo.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;
        return btn;
    }

    static void AddSceneToBuild(string path)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes) if (s.path == path) return;
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            { new EditorBuildSettingsScene(path, true) };
        EditorBuildSettings.scenes = list.ToArray();
    }
}
