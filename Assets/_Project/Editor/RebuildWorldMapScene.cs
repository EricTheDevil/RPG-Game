#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using RPG.Core;
using RPG.Data;

namespace RPG.Editor
{
    /// <summary>
    /// Rebuilds the WorldMap scene with a clean, working layout:
    ///
    ///   ┌─────────────────────────────────────────────────┐
    ///   │  TOP BAR  — Gold / Scrap / Rations / Morale / Layer  │
    ///   ├─────────────────────────────────────────────────┤
    ///   │                                                 │
    ///   │   scrollable map area (nodes + connections)     │
    ///   │   bottom = layer 0, top = Demon Lord            │
    ///   │                                                 │
    ///   ├─────────────────────────────────────────────────┤
    ///   │  BOTTOM BAR — Main Menu                         │
    ///   └─────────────────────────────────────────────────┘
    ///
    /// Key fixes over the old scene:
    ///   • Mask image uses full alpha (required for clipping)
    ///   • MapContainer anchored 0,0/1,1 so it fills scroll rect width
    ///   • ContentSizeFitter drives height; no hard-coded sizeDelta
    ///   • WorldMapUI.MapContainer set to the VerticalLayoutGroup child
    /// </summary>
    public static class RebuildWorldMapScene
    {
        private static readonly Color Dark      = new Color(0.06f, 0.06f, 0.10f);
        private static readonly Color Panel     = new Color(0.10f, 0.10f, 0.16f, 0.95f);
        private static readonly Color Gold      = new Color(1f, 0.84f, 0f);
        private static readonly Color Subtle    = new Color(1f, 1f, 1f, 0.7f);

        [MenuItem("RPG/Map/Rebuild WorldMap Scene")]
        public static void Rebuild()
        {
            EnsureFolder("Assets/_Project/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGO = new GameObject("Main Camera");
            var cam   = camGO.AddComponent<Camera>();
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = Dark;
            cam.orthographic     = true;
            camGO.AddComponent<AudioListener>();
            camGO.tag = "MainCamera";

            // EventSystem
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Root canvas
            var canvasGO  = new GameObject("UICanvas");
            var canvas    = canvasGO.AddComponent<Canvas>();
            canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder  = 0;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1920, 1080);
            scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight   = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            var canvasRT = canvasGO.GetComponent<RectTransform>();

            // ── Top bar ───────────────────────────────────────────────────────
            var topBar = MakeRect("TopBar", canvasRT, 0, 1, 0, 1, 0, -64, 0, 0);
            topBar.gameObject.AddComponent<Image>().color = Panel;

            var worldMapUI = canvasGO.AddComponent<RPG.UI.WorldMapUI>();

            worldMapUI.GoldText    = MakeTMP("GoldText",    topBar, new Vector2(120,  -32), "Gold  0",       14);
            worldMapUI.ScrapText   = MakeTMP("ScrapText",   topBar, new Vector2(310,  -32), "Scrap  0",      14);
            worldMapUI.RationsText = MakeTMP("RationsText", topBar, new Vector2(510,  -32), "Rations  3/10", 14);
            worldMapUI.MoraleText  = MakeTMP("MoraleText",  topBar, new Vector2(720,  -32), "Morale  5/10",  14);
            worldMapUI.ProgressText= MakeTMP("Progress",    topBar, new Vector2(960,  -32), "Layer 1/10  —  Journey to the Demon Lord", 13);
            worldMapUI.ProgressText.color = Gold;
            worldMapUI.ProgressText.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 30);
            worldMapUI.ProgressText.alignment = TextAlignmentOptions.Center;

            // ── Bottom bar ────────────────────────────────────────────────────
            var botBar = MakeRect("BottomBar", canvasRT, 0, 0, 0, 0, 0, 0, 0, 56);
            botBar.gameObject.AddComponent<Image>().color = Panel;

            worldMapUI.MainMenuButton = MakeButton("MainMenuButton", botBar,
                new Vector2(80, 28), new Vector2(160, 40), "← Menu");

            // Abandon run button (right side)
            MakeButton("AbandonButton", botBar,
                new Vector2(1840, 28), new Vector2(160, 40), "Abandon Run");

            // ── Map body (fills between bars) ─────────────────────────────────
            var mapBodyGO = new GameObject("MapBody");
            mapBodyGO.transform.SetParent(canvasGO.transform, false);
            var mapBodyRT = mapBodyGO.AddComponent<RectTransform>();
            mapBodyRT.anchorMin = new Vector2(0f, 0f);
            mapBodyRT.anchorMax = new Vector2(1f, 1f);
            mapBodyRT.offsetMin = new Vector2(0f,  56f);  // clear bottom bar
            mapBodyRT.offsetMax = new Vector2(0f, -64f);  // clear top bar

            // ── Map container — centred, scaled at runtime to fit ──────────────
            var contentGO = new GameObject("MapContainer");
            contentGO.transform.SetParent(mapBodyGO.transform, false);
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin        = new Vector2(0.5f, 0.5f);
            contentRT.anchorMax        = new Vector2(0.5f, 0.5f);
            contentRT.pivot            = new Vector2(0.5f, 0.5f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta        = new Vector2(900f, 1600f); // overwritten at runtime

            worldMapUI.MapBody      = mapBodyRT;
            worldMapUI.MapContainer = contentRT;

            // Load prefabs
            var nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Map/WorldMapNode.prefab");
            var connPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Map/ConnectionLine.prefab");
            worldMapUI.NodePrefab       = nodePrefab;
            worldMapUI.ConnectionPrefab = connPrefab;

            worldMapUI.LayerSpacing    = 140f;
            worldMapUI.NodeSpacing     = 220f;
            worldMapUI.VerticalPadding = 80f;

            // ── Banner overlay ────────────────────────────────────────────────
            var bannerGO = new GameObject("Banner");
            bannerGO.transform.SetParent(canvasGO.transform, false);
            var bannerRT = bannerGO.AddComponent<RectTransform>();
            bannerRT.anchorMin = new Vector2(0.25f, 0.44f);
            bannerRT.anchorMax = new Vector2(0.75f, 0.56f);
            bannerRT.offsetMin = Vector2.zero; bannerRT.offsetMax = Vector2.zero;
            var bannerImg = bannerGO.AddComponent<Image>();
            bannerImg.color = new Color(0.05f, 0.05f, 0.10f, 0.97f);
            bannerGO.SetActive(false);

            var bannerTextGO = new GameObject("Text");
            bannerTextGO.transform.SetParent(bannerGO.transform, false);
            var btRT = bannerTextGO.AddComponent<RectTransform>();
            btRT.anchorMin = Vector2.zero; btRT.anchorMax = Vector2.one;
            btRT.offsetMin = new Vector2(16, 0); btRT.offsetMax = new Vector2(-16, 0);
            var bannerTMP = bannerTextGO.AddComponent<TextMeshProUGUI>();
            bannerTMP.text      = "";
            bannerTMP.fontSize  = 22;
            bannerTMP.color     = Gold;
            bannerTMP.alignment = TextAlignmentOptions.Center;

            worldMapUI.Banner     = bannerGO;
            worldMapUI.BannerText = bannerTMP;

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/WorldMap.unity");
            AssetDatabase.Refresh();

            Debug.Log("[RebuildWorldMapScene] WorldMap scene rebuilt.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static RectTransform MakeRect(string name, RectTransform parent,
            float ancMinX, float ancMinY, float ancMaxX, float ancMaxY,
            float offMinX, float offMinY, float offMaxX, float offMaxY)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(ancMinX, ancMinY);
            rt.anchorMax = new Vector2(ancMaxX, ancMaxY);
            rt.offsetMin = new Vector2(offMinX, offMinY);
            rt.offsetMax = new Vector2(offMaxX, offMaxY);
            return rt;
        }

        private static TextMeshProUGUI MakeTMP(string name, RectTransform parent,
            Vector2 pos, string text, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(220, 30);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = size;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            return tmp;
        }

        private static Button MakeButton(string name, RectTransform parent,
            Vector2 pos, Vector2 size, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.18f, 0.28f);
            var btn = go.AddComponent<Button>();

            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 14;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif
