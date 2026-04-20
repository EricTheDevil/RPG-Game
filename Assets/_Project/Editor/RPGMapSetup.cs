#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using RPG.Data;
using RPG.Core;

namespace RPG.Editor
{
    /// <summary>
    /// One-click setup for the two-layer map system.
    ///
    /// Menu items:
    ///   RPG > Map > Create Config Assets
    ///   RPG > Map > Create Map Prefabs
    ///   RPG > Map > Create WorldMap Scene
    ///   RPG > Map > Create AreaMap Scene
    ///   RPG > Map > Create ClassSelect Scene
    ///   RPG > Map > Wire WorldMapConfig to GameManager
    ///   RPG > Map > Add Map Scenes to Build Settings
    /// </summary>
    public static class RPGMapSetup
    {
        private static readonly Color DarkBG    = new Color(0.08f, 0.08f, 0.12f);
        private static readonly Color PanelBG   = new Color(0.12f, 0.12f, 0.18f, 0.9f);
        private static readonly Color AccentGold = new Color(1f, 0.84f, 0f);

        // ── Config Assets ─────────────────────────────────────────────────────

        [MenuItem("RPG/Map/Create Config Assets")]
        public static void CreateConfigAssets()
        {
            string dir = "Assets/_Project/ScriptableObjects/MapConfig";
            if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets/_Project", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "MapConfig");

            // World Map Config
            var worldConfig = ScriptableObject.CreateInstance<WorldMapConfigSO>();
            worldConfig.TotalLayers        = 10;
            worldConfig.MinNodesPerLayer   = 2;
            worldConfig.MaxNodesPerLayer   = 4;
            worldConfig.HostileWeight      = 0.45f;
            worldConfig.SafeWeight         = 0.30f;
            worldConfig.RandomWeight       = 0.25f;
            worldConfig.HostileRampPerLayer = 0.03f;
            worldConfig.BossName           = "Demon Lord";
            AssetDatabase.CreateAsset(worldConfig, $"{dir}/WorldMapConfig.asset");

            // Hostile Area Config
            var hostile = ScriptableObject.CreateInstance<AreaMapConfigSO>();
            hostile.MinEvents      = 4;
            hostile.MaxEvents      = 6;
            hostile.ThreatLimit    = 3;
            hostile.CombatWeight   = 0.45f;
            hostile.EliteWeight    = 0.10f;
            hostile.DilemmaWeight  = 0.10f;
            hostile.ShopWeight     = 0.05f;
            hostile.HealWeight     = 0.10f;
            hostile.RestWeight     = 0.05f;
            hostile.TreasureWeight = 0.10f;
            hostile.ShrineWeight   = 0.02f;
            hostile.LevelUpWeight  = 0.03f;
            AssetDatabase.CreateAsset(hostile, $"{dir}/AreaMapConfig_Hostile.asset");

            // Safe Area Config
            var safe = ScriptableObject.CreateInstance<AreaMapConfigSO>();
            safe.MinEvents      = 5;
            safe.MaxEvents      = 7;
            safe.ThreatLimit    = 5;
            safe.CombatWeight   = 0.10f;
            safe.EliteWeight    = 0.00f;
            safe.DilemmaWeight  = 0.25f;
            safe.ShopWeight     = 0.20f;
            safe.HealWeight     = 0.15f;
            safe.RestWeight     = 0.15f;
            safe.TreasureWeight = 0.10f;
            safe.ShrineWeight   = 0.05f;
            safe.LevelUpWeight  = 0.00f;
            AssetDatabase.CreateAsset(safe, $"{dir}/AreaMapConfig_Safe.asset");

            // Random Area Config
            var random = ScriptableObject.CreateInstance<AreaMapConfigSO>();
            random.MinEvents      = 4;
            random.MaxEvents      = 7;
            random.ThreatLimit    = 4;
            random.CombatWeight   = 0.28f;
            random.EliteWeight    = 0.05f;
            random.DilemmaWeight  = 0.20f;
            random.ShopWeight     = 0.13f;
            random.HealWeight     = 0.12f;
            random.RestWeight     = 0.08f;
            random.TreasureWeight = 0.08f;
            random.ShrineWeight   = 0.04f;
            random.LevelUpWeight  = 0.02f;
            AssetDatabase.CreateAsset(random, $"{dir}/AreaMapConfig_Random.asset");

            // Wire area configs into world config
            worldConfig.HostileAreaConfig = hostile;
            worldConfig.SafeAreaConfig    = safe;
            worldConfig.RandomAreaConfig  = random;
            EditorUtility.SetDirty(worldConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Auto-populate SectorEventSO pools
            WireSectorEventsIntoConfigs(hostile, safe, random);

            Debug.Log("[RPGMapSetup] Config assets created in " + dir);
        }

        private static void WireSectorEventsIntoConfigs(AreaMapConfigSO hostile, AreaMapConfigSO safe, AreaMapConfigSO random)
        {
            var guids = AssetDatabase.FindAssets("t:SectorEventSO");

            var combatList   = new System.Collections.Generic.List<SectorEventSO>();
            var dilemmaList  = new System.Collections.Generic.List<SectorEventSO>();
            var shopList     = new System.Collections.Generic.List<SectorEventSO>();
            var healList     = new System.Collections.Generic.List<SectorEventSO>();
            var restList     = new System.Collections.Generic.List<SectorEventSO>();
            var treasureList = new System.Collections.Generic.List<SectorEventSO>();
            var shrineList   = new System.Collections.Generic.List<SectorEventSO>();
            var levelUpList  = new System.Collections.Generic.List<SectorEventSO>();

            foreach (var guid in guids)
            {
                var ev = AssetDatabase.LoadAssetAtPath<SectorEventSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (ev == null) continue;
                switch (ev.Category)
                {
                    case SectorEventCategory.Combat:   combatList.Add(ev);   break;
                    case SectorEventCategory.Dilemma:  dilemmaList.Add(ev);  break;
                    case SectorEventCategory.Shop:     shopList.Add(ev);     break;
                    case SectorEventCategory.Rest:     restList.Add(ev);     break;
                    case SectorEventCategory.Treasure: treasureList.Add(ev); break;
                    case SectorEventCategory.Shrine:   shrineList.Add(ev);   break;
                    case SectorEventCategory.LevelUp:  levelUpList.Add(ev);  break;
                }
            }

            var configs = new[] { hostile, safe, random };
            foreach (var cfg in configs)
            {
                cfg.CombatEvents   = combatList.ToArray();
                cfg.DilemmaEvents  = dilemmaList.ToArray();
                cfg.ShopEvents     = shopList.ToArray();
                cfg.HealEvents     = healList.ToArray();
                cfg.RestEvents     = restList.ToArray();
                cfg.TreasureEvents = treasureList.ToArray();
                cfg.ShrineEvents   = shrineList.ToArray();
                cfg.LevelUpEvents  = levelUpList.ToArray();
                EditorUtility.SetDirty(cfg);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[RPGMapSetup] Wired {guids.Length} SectorEventSO assets into area configs.");
        }

        // ── Map Prefabs ───────────────────────────────────────────────────────

        [MenuItem("RPG/Map/Create Map Prefabs")]
        public static void CreateMapPrefabs()
        {
            string dir = "Assets/_Project/Prefabs/Map";
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Map");

            CreateWorldNodePrefab(dir);
            CreateAreaTilePrefab(dir);
            CreateConnectionPrefab(dir);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RPGMapSetup] Map prefabs created in " + dir);
        }

        private static void CreateWorldNodePrefab(string dir)
        {
            var go = new GameObject("WorldMapNode");
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(140, 80);

            var bgGO = CreateChildRect("BG", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bgGO.AddComponent<Image>().color = PanelBG;

            var borderGO = CreateChildRect("Border", go.transform, Vector2.zero, Vector2.one,
                new Vector2(-2, -2), new Vector2(2, 2));
            borderGO.AddComponent<Image>().color = Color.white;
            borderGO.transform.SetAsFirstSibling();

            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(go.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(0, 10); iconRect.sizeDelta = new Vector2(30, 30);
            var iconImg = iconGO.AddComponent<Image>();

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(0, -18); labelRect.sizeDelta = new Vector2(130, 30);
            var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "Node"; labelTmp.fontSize = 12;
            labelTmp.alignment = TextAlignmentOptions.Center; labelTmp.color = Color.white;

            var visitedGO = CreateChildRect("VisitedOverlay", go.transform, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            visitedGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            visitedGO.SetActive(false);

            var bossGO = new GameObject("BossIcon");
            bossGO.transform.SetParent(go.transform, false);
            bossGO.AddComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
            bossGO.AddComponent<Image>().color = new Color(0.85f, 0.15f, 0.15f);
            bossGO.SetActive(false);

            var comp = go.AddComponent<RPG.UI.WorldMapNodeUI>();
            comp.NodeIcon       = iconImg;
            comp.NodeBorder     = borderGO.GetComponent<Image>();
            comp.NodeLabel      = labelTmp;
            comp.VisitedOverlay = visitedGO;
            comp.BossIcon       = bossGO;
            go.AddComponent<CanvasGroup>();

            PrefabUtility.SaveAsPrefabAsset(go, $"{dir}/WorldMapNode.prefab");
            Object.DestroyImmediate(go);
        }

        private static void CreateAreaTilePrefab(string dir)
        {
            var go = new GameObject("AreaTile");
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(120, 100);

            // Background
            var bgGO = CreateChildRect("Background", go.transform, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.20f);

            // Border (separate so it can be colored differently)
            var borderGO = CreateChildRect("Border", go.transform, Vector2.zero, Vector2.one,
                new Vector2(-2, -2), new Vector2(2, 2));
            var borderImg = borderGO.AddComponent<Image>();
            borderImg.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
            borderGO.transform.SetAsFirstSibling();

            // Icon
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(go.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(0, 15); iconRect.sizeDelta = new Vector2(28, 28);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.color = Color.white;

            // Tile label (event name / type)
            var labelGO = new GameObject("TileLabel");
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(0, -22); labelRect.sizeDelta = new Vector2(110, 36);
            var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
            labelTmp.text = ""; labelTmp.fontSize = 9;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = Color.white;
            labelTmp.overflowMode = TextOverflowModes.Truncate;

            // Player marker
            var playerGO = new GameObject("PlayerMarker");
            playerGO.transform.SetParent(go.transform, false);
            var pmRect = playerGO.AddComponent<RectTransform>();
            pmRect.anchoredPosition = new Vector2(40, 35); pmRect.sizeDelta = new Vector2(16, 16);
            playerGO.AddComponent<Image>().color = Color.white;
            playerGO.SetActive(false);

            // Locked overlay
            var lockedGO = CreateChildRect("LockedOverlay", go.transform, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            lockedGO.AddComponent<Image>().color = new Color(0.5f, 0.0f, 0.0f, 0.5f);
            lockedGO.SetActive(false);

            // Visited overlay
            var visitedGO = CreateChildRect("VisitedOverlay", go.transform, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            visitedGO.AddComponent<Image>().color = new Color(0.0f, 0.0f, 0.0f, 0.4f);
            visitedGO.SetActive(false);

            // Wire AreaTileUI component
            var comp = go.AddComponent<RPG.UI.AreaTileUI>();
            comp.Background    = bgImg;
            comp.Border        = borderImg;
            comp.IconImage     = iconImg;
            comp.TileLabel     = labelTmp;
            comp.PlayerMarker  = playerGO;
            comp.LockedOverlay = lockedGO;
            comp.VisitedOverlay = visitedGO;

            go.AddComponent<CanvasGroup>();

            PrefabUtility.SaveAsPrefabAsset(go, $"{dir}/AreaTile.prefab");
            Object.DestroyImmediate(go);
        }

        private static void CreateConnectionPrefab(string dir)
        {
            var go = new GameObject("ConnectionLine");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 3);
            rect.pivot = new Vector2(0.5f, 0.5f);
            go.AddComponent<Image>().color = new Color(0.6f, 0.6f, 0.6f, 0.7f);

            PrefabUtility.SaveAsPrefabAsset(go, $"{dir}/ConnectionLine.prefab");
            Object.DestroyImmediate(go);
        }

        // ── WorldMap Scene ────────────────────────────────────────────────────

        [MenuItem("RPG/Map/Create WorldMap Scene")]
        public static void CreateWorldMapScene()
        {
            EnsureScenesFolder();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            AddCamera(DarkBG);
            AddEventSystem();

            var canvas = CreateCanvas("UICanvas");
            var mapUI  = canvas.AddComponent<RPG.UI.WorldMapUI>();

            // Top bar
            var topBar = CreatePanel("TopBar", canvas.transform,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), Vector2.zero);
            topBar.AddComponent<Image>().color = PanelBG;

            mapUI.GoldText     = CreateTMP("GoldText",     topBar.transform, new Vector2(60,  -30), "Gold  0");
            mapUI.ScrapText    = CreateTMP("ScrapText",    topBar.transform, new Vector2(220, -30), "Scrap  0");
            mapUI.RationsText  = CreateTMP("RationsText",  topBar.transform, new Vector2(380, -30), "Rations  3/10");
            mapUI.MoraleText   = CreateTMP("MoraleText",   topBar.transform, new Vector2(560, -30), "Morale  5/10");
            mapUI.ProgressText = CreateTMP("ProgressText", topBar.transform, new Vector2(900, -30), "Layer 1/10");

            // Scroll area
            var scrollGO = new GameObject("MapScroll");
            scrollGO.transform.SetParent(canvas.transform, false);
            var scrollRect = scrollGO.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0.08f);
            scrollRect.anchorMax = new Vector2(1, 0.94f);
            scrollRect.offsetMin = new Vector2(20, 0);
            scrollRect.offsetMax = new Vector2(-20, 0);
            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scrollGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            scrollGO.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("MapContainer");
            content.transform.SetParent(scrollGO.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0);
            contentRect.anchorMax = new Vector2(0.5f, 0);
            contentRect.pivot     = new Vector2(0.5f, 0);
            contentRect.sizeDelta = new Vector2(900, 2200);
            scroll.content        = contentRect;
            mapUI.MapContainer    = contentRect;
            mapUI.MapScroll       = scroll;

            // Bottom bar
            var botBar = CreatePanel("BottomBar", canvas.transform,
                new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 50));
            botBar.AddComponent<Image>().color = PanelBG;
            mapUI.MainMenuButton = CreateButton("MainMenuButton", botBar.transform, new Vector2(100, 25), "Main Menu");

            // Banner
            mapUI.Banner     = CreateBanner("Banner", canvas.transform, out var bannerTmp);
            mapUI.BannerText = bannerTmp;

            // Load prefabs
            var nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Map/WorldMapNode.prefab");
            var connPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Map/ConnectionLine.prefab");
            mapUI.NodePrefab       = nodePrefab;
            mapUI.ConnectionPrefab = connPrefab;

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/WorldMap.unity");
            Debug.Log("[RPGMapSetup] WorldMap scene created.");
        }

        // ── AreaMap Scene ─────────────────────────────────────────────────────

        [MenuItem("RPG/Map/Create AreaMap Scene")]
        public static void CreateAreaMapScene()
        {
            EnsureScenesFolder();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            AddCamera(DarkBG);
            AddEventSystem();

            var canvas = CreateCanvas("UICanvas");
            var areaUI = canvas.AddComponent<RPG.UI.AreaMapUI>();

            // Top bar (resources + threat)
            var topBar = CreatePanel("TopBar", canvas.transform,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -90), Vector2.zero);
            topBar.AddComponent<Image>().color = PanelBG;

            areaUI.GoldText    = CreateTMP("GoldText",    topBar.transform, new Vector2(60,  -22), "Gold  0");
            areaUI.ScrapText   = CreateTMP("ScrapText",   topBar.transform, new Vector2(220, -22), "Scrap  0");
            areaUI.RationsText = CreateTMP("RationsText", topBar.transform, new Vector2(380, -22), "Rations  3/10");
            areaUI.MoraleText  = CreateTMP("MoraleText",  topBar.transform, new Vector2(560, -22), "Morale  5/10");
            areaUI.AreaNameText = CreateTMP("AreaName",   topBar.transform, new Vector2(900, -22), "Dark Forest");

            // Threat bar strip inside top bar
            var threatBG = CreatePanel("ThreatBar", topBar.transform,
                new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 18));
            threatBG.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f);

            var tfillGO = CreateChildRect("ThreatFill", threatBG.transform, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            var tfillImg = tfillGO.AddComponent<Image>();
            tfillImg.color = new Color(0.25f, 0.75f, 0.30f);
            tfillImg.type  = Image.Type.Filled;
            tfillImg.fillMethod = Image.FillMethod.Horizontal;
            tfillImg.fillAmount = 0f;
            areaUI.ThreatFill = tfillImg;

            areaUI.ThreatText = CreateTMP("ThreatText", threatBG.transform, new Vector2(10, 0), "Threat  0/3");
            areaUI.ThreatText.fontSize = 12;
            areaUI.ThreatText.alignment = TextAlignmentOptions.MidlineLeft;

            // Starvation badge (top-right corner)
            var starvGO = new GameObject("StarvationBadge");
            starvGO.transform.SetParent(canvas.transform, false);
            var starvRect = starvGO.AddComponent<RectTransform>();
            starvRect.anchorMin = new Vector2(1, 1); starvRect.anchorMax = new Vector2(1, 1);
            starvRect.pivot = new Vector2(1, 1);
            starvRect.anchoredPosition = new Vector2(-10, -10);
            starvRect.sizeDelta = new Vector2(160, 40);
            starvGO.AddComponent<Image>().color = new Color(0.8f, 0.1f, 0.1f, 0.9f);
            var starvTmp = new GameObject("Text");
            starvTmp.transform.SetParent(starvGO.transform, false);
            var starvTmpRect = starvTmp.AddComponent<RectTransform>();
            starvTmpRect.anchorMin = Vector2.zero; starvTmpRect.anchorMax = Vector2.one;
            starvTmpRect.offsetMin = Vector2.zero; starvTmpRect.offsetMax = Vector2.zero;
            var starvText = starvTmp.AddComponent<TextMeshProUGUI>();
            starvText.text = "STARVING"; starvText.fontSize = 14;
            starvText.alignment = TextAlignmentOptions.Center; starvText.color = Color.white;
            starvGO.SetActive(false);
            areaUI.StarvationBadge = starvGO;

            // Grid container (center area)
            var gridGO = CreatePanel("GridContainer", canvas.transform,
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.88f), Vector2.zero, Vector2.zero);
            areaUI.GridContainer = gridGO.GetComponent<RectTransform>();

            // Bottom bar
            var botBar = CreatePanel("BottomBar", canvas.transform,
                new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 50));
            botBar.AddComponent<Image>().color = PanelBG;
            areaUI.LeaveButton = CreateButton("LeaveButton", botBar.transform,
                new Vector2(960, 25), "Leave Area  \u2192");
            areaUI.LeaveButtonLabel = areaUI.LeaveButton.GetComponentInChildren<TextMeshProUGUI>();

            // Banner
            areaUI.Banner     = CreateBanner("Banner", canvas.transform, out var bannerTmp);
            areaUI.BannerText = bannerTmp;

            // SectorEventPanel overlay (Field Decisions)
            var sepGO = CreatePanel("SectorEventPanel", canvas.transform,
                new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.9f), Vector2.zero, Vector2.zero);
            sepGO.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.14f, 0.97f);
            var sepCG = sepGO.AddComponent<CanvasGroup>();
            sepCG.alpha = 0f; sepCG.interactable = false; sepCG.blocksRaycasts = false;

            var sectorUI = sepGO.AddComponent<RPG.UI.SectorEventUI>();
            sectorUI.RootGroup = sepCG;

            sectorUI.EventNameText = CreateTMP("EventName", sepGO.transform, new Vector2(0, -40), "Event Title");
            sectorUI.EventNameText.fontSize = 24;
            sectorUI.EventNameText.alignment = TextAlignmentOptions.Center;
            var enRect = sectorUI.EventNameText.GetComponent<RectTransform>();
            enRect.anchorMin = new Vector2(0.05f, 1); enRect.anchorMax = new Vector2(0.95f, 1);
            enRect.pivot = new Vector2(0.5f, 1); enRect.sizeDelta = new Vector2(0, 40);

            sectorUI.PromptText = CreateTMP("Prompt", sepGO.transform, new Vector2(0, -100), "Event description...");
            sectorUI.PromptText.fontSize = 14;
            sectorUI.PromptText.alignment = TextAlignmentOptions.Center;
            var prRect = sectorUI.PromptText.GetComponent<RectTransform>();
            prRect.anchorMin = new Vector2(0.05f, 1); prRect.anchorMax = new Vector2(0.95f, 1);
            prRect.pivot = new Vector2(0.5f, 1); prRect.sizeDelta = new Vector2(0, 80);

            // Choice container
            var choiceContainerGO = CreatePanel("ChoiceContainer", sepGO.transform,
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.65f), Vector2.zero, Vector2.zero);
            var vlg = choiceContainerGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;
            choiceContainerGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sectorUI.ChoiceContainer = choiceContainerGO.transform;

            // Skip button
            var skipBtn = CreateButton("SkipButton", sepGO.transform, new Vector2(0, 30), "Skip");
            var skipRect = skipBtn.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(0.5f, 0); skipRect.anchorMax = new Vector2(0.5f, 0);
            skipRect.pivot = new Vector2(0.5f, 0); skipRect.sizeDelta = new Vector2(160, 40);
            skipRect.anchoredPosition = new Vector2(0, 30);
            sectorUI.SkipButton = skipBtn;

            areaUI.SectorEventPanel = sectorUI;

            // Load tile prefab
            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Map/AreaTile.prefab");
            areaUI.TilePrefab = tilePrefab;

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/AreaMap.unity");
            Debug.Log("[RPGMapSetup] AreaMap scene created.");
        }

        // ── ClassSelect Scene ─────────────────────────────────────────────────

        [MenuItem("RPG/Map/Create ClassSelect Scene")]
        public static void CreateClassSelectScene()
        {
            EnsureScenesFolder();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            AddCamera(DarkBG);
            AddEventSystem();

            var canvas = CreateCanvas("UICanvas");
            var csUI = canvas.AddComponent<RPG.UI.ClassSelectUI>();

            // Commander bar
            var cmdBar = CreatePanel("CommanderBar", canvas.transform,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), Vector2.zero);
            cmdBar.AddComponent<Image>().color = PanelBG;
            csUI.CommanderLevelText = CreateTMP("CmdLevel", cmdBar.transform, new Vector2(60, -30), "Commander  Lv.0");
            csUI.CommanderNameText  = CreateTMP("CmdName",  cmdBar.transform, new Vector2(300, -30), "Commander");

            var cmdXPBG = CreateChildRect("CommanderXPBG", cmdBar.transform,
                new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 10));
            cmdXPBG.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);
            var cmdXPFill = CreateChildRect("CommanderXPFill", cmdXPBG.transform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var cmdXPImg = cmdXPFill.AddComponent<Image>();
            cmdXPImg.color = AccentGold;
            cmdXPImg.type  = Image.Type.Filled;
            cmdXPImg.fillMethod = Image.FillMethod.Horizontal;
            cmdXPImg.fillAmount = 0f;
            csUI.CommanderXPFill = cmdXPImg;

            // Class card container
            var cardContainerGO = CreatePanel("CardContainer", canvas.transform,
                new Vector2(0.05f, 0.15f), new Vector2(0.65f, 0.88f), Vector2.zero, Vector2.zero);
            var hlg = cardContainerGO.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(10, 10, 10, 10);
            csUI.CardContainer = cardContainerGO.transform;

            // Description panel (right)
            var descPanel = CreatePanel("DescPanel", canvas.transform,
                new Vector2(0.67f, 0.15f), new Vector2(0.95f, 0.88f), Vector2.zero, Vector2.zero);
            descPanel.AddComponent<Image>().color = PanelBG;
            csUI.SelectedClassNameText = CreateTMP("SelClassName", descPanel.transform,
                new Vector2(10, -30), "Select a Class");
            csUI.SelectedClassNameText.fontSize = 22;
            csUI.SelectedClassDescText = CreateTMP("SelClassDesc", descPanel.transform,
                new Vector2(10, -80), "");
            csUI.SelectedClassDescText.fontSize = 13;
            csUI.EvolutionTaskText = CreateTMP("EvolutionTask", descPanel.transform,
                new Vector2(10, -160), "");
            csUI.EvolutionTaskText.fontSize = 12;

            // Confirm button
            var confirmBtn = CreateButton("ConfirmButton", canvas.transform, new Vector2(960, 50), "Select a Class");
            var cbRect = confirmBtn.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.5f, 0); cbRect.anchorMax = new Vector2(0.5f, 0);
            cbRect.pivot     = new Vector2(0.5f, 0); cbRect.sizeDelta = new Vector2(300, 50);
            cbRect.anchoredPosition = new Vector2(0, 20);
            confirmBtn.interactable = false;
            csUI.ConfirmButton = confirmBtn;
            csUI.ConfirmButtonLabel = confirmBtn.GetComponentInChildren<TextMeshProUGUI>();

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/ClassSelect.unity");
            Debug.Log("[RPGMapSetup] ClassSelect scene created.");
        }

        // ── Wire Config to GameManager ────────────────────────────────────────

        [MenuItem("RPG/Map/Wire WorldMapConfig to GameManager")]
        public static void WireConfigToGameManager()
        {
            var config = AssetDatabase.LoadAssetAtPath<WorldMapConfigSO>(
                "Assets/_Project/ScriptableObjects/MapConfig/WorldMapConfig.asset");
            if (config == null)
            {
                Debug.LogError("[RPGMapSetup] WorldMapConfig.asset not found. Run 'Create Config Assets' first.");
                return;
            }

            var gmPrefab = Resources.Load<GameManager>("GameManager");
            if (gmPrefab == null)
            {
                Debug.LogError("[RPGMapSetup] GameManager prefab not found in Resources/.");
                return;
            }

            gmPrefab.WorldMapConfig = config;
            EditorUtility.SetDirty(gmPrefab);
            AssetDatabase.SaveAssets();
            Debug.Log("[RPGMapSetup] WorldMapConfig wired to GameManager prefab.");
        }

        // ── Build Settings ────────────────────────────────────────────────────

        /// <summary>
        /// Adds all map-system scenes to Build Settings, preserving existing entries.
        /// Run this after creating the scenes.
        /// </summary>
        [MenuItem("RPG/Map/Add Map Scenes to Build Settings")]
        public static void AddScenesToBuildSettings()
        {
            // Scene paths we need in the build
            var required = new[]
            {
                "Assets/_Project/Scenes/ClassSelect.unity",
                "Assets/_Project/Scenes/WorldMap.unity",
                "Assets/_Project/Scenes/AreaMap.unity",
            };

            var existing = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            bool changed = false;
            foreach (var path in required)
            {
                // Skip if already present (enabled or disabled)
                bool found = false;
                foreach (var s in existing)
                    if (s.path == path) { found = true; break; }

                if (!found)
                {
                    if (!System.IO.File.Exists(path))
                    {
                        Debug.LogWarning($"[RPGMapSetup] Scene not found at '{path}' — create it first.");
                        continue;
                    }
                    existing.Add(new EditorBuildSettingsScene(path, enabled: true));
                    Debug.Log($"[RPGMapSetup] Added to Build Settings: {path}");
                    changed = true;
                }
            }

            if (changed)
            {
                EditorBuildSettings.scenes = existing.ToArray();
                Debug.Log("[RPGMapSetup] Build Settings updated.");
            }
            else
            {
                Debug.Log("[RPGMapSetup] All map scenes already in Build Settings.");
            }
        }

        // ── Utilities ─────────────────────────────────────────────────────────

        private static void EnsureScenesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes"))
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
        }

        private static void AddCamera(Color bg)
        {
            var cam = new GameObject("Main Camera");
            var c   = cam.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = bg;
            cam.AddComponent<AudioListener>();
            cam.tag = "MainCamera";
        }

        private static void AddEventSystem()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private static GameObject CreateCanvas(string name)
        {
            var go = new GameObject(name);
            var c  = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 1;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static GameObject CreatePanel(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = anchorMin; r.anchorMax = anchorMax;
            r.offsetMin = offsetMin; r.offsetMax = offsetMax;
            return go;
        }

        private static GameObject CreateChildRect(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = anchorMin; r.anchorMax = anchorMax;
            r.offsetMin = offsetMin; r.offsetMax = offsetMax;
            return go;
        }

        private static TextMeshProUGUI CreateTMP(string name, Transform parent, Vector2 pos, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchoredPosition = pos; r.sizeDelta = new Vector2(200, 30);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 16;
            tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.MidlineLeft;
            return tmp;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 pos, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchoredPosition = pos; r.sizeDelta = new Vector2(200, 44);
            go.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.32f);
            var btn = go.AddComponent<Button>();

            var lGO = new GameObject("Label");
            lGO.transform.SetParent(go.transform, false);
            var lR = lGO.AddComponent<RectTransform>();
            lR.anchorMin = Vector2.zero; lR.anchorMax = Vector2.one;
            lR.offsetMin = Vector2.zero; lR.offsetMax = Vector2.zero;
            var tmp = lGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 16;
            tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private static GameObject CreateBanner(string name, Transform parent, out TextMeshProUGUI textOut)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0.2f, 0.45f); r.anchorMax = new Vector2(0.8f, 0.55f);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.14f, 0.95f);

            var tGO = new GameObject("Text");
            tGO.transform.SetParent(go.transform, false);
            var tR = tGO.AddComponent<RectTransform>();
            tR.anchorMin = Vector2.zero; tR.anchorMax = Vector2.one;
            tR.offsetMin = new Vector2(10, 0); tR.offsetMax = new Vector2(-10, 0);
            var tmp = tGO.AddComponent<TextMeshProUGUI>();
            tmp.text = ""; tmp.fontSize = 20;
            tmp.color = AccentGold; tmp.alignment = TextAlignmentOptions.Center;

            go.SetActive(false);
            textOut = tmp;
            return go;
        }
    }
}
#endif
