using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RPG.Data;
using RPG.Map;
using RPG.UI;
using RPG.Core;

/// <summary>
/// Builds the MapSelector and RewardScene, creates LevelData assets,
/// and wires up the full game loop.
/// Run via  RPG > Setup Game Loop
/// </summary>
public class RPGGameLoopSetup
{
    [MenuItem("RPG/Setup Game Loop")]
    public static void Execute()
    {
        EnsureFolders();
        CreateLevelAssets();
        CreateMapSelectorScene();
        CreateRewardScene();
        PatchMainMenuButton();
        AddEventSystems();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RPGGameLoopSetup] ✅ Game loop setup complete.");
    }

    // ────────────────────────────────────────────────────────────────────────────
    static void EnsureFolders()
    {
        var paths = new[]
        {
            "Assets/_Project/ScriptableObjects/Levels",
            "Assets/_Project/ScriptableObjects/Rewards",
            "Assets/_Project/Prefabs/Map",
        };
        foreach (var p in paths)
        {
            var parts = p.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  LEVEL + REWARD ASSETS
    // ────────────────────────────────────────────────────────────────────────────
    static LevelDataSO _level0, _level1, _level2;

    static void CreateLevelAssets()
    {
        // ── Reward for Level 0 ──────────────────────────────────────────
        var r0 = LoadOrCreate<LevelRewardSO>("Assets/_Project/ScriptableObjects/Rewards/Reward_L0.asset");
        r0.XPReward    = 80;
        r0.GoldReward  = 30;
        r0.FlavorText  = "A dark presence vanquished.\nThe ruin falls silent.";
        r0.AccentColor = new Color(1f, 0.84f, 0f);
        EditorUtility.SetDirty(r0);

        // ── Reward for Level 1 ──────────────────────────────────────────
        var r1 = LoadOrCreate<LevelRewardSO>("Assets/_Project/ScriptableObjects/Rewards/Reward_L1.asset");
        r1.XPReward       = 150;
        r1.GoldReward     = 70;
        r1.ItemName       = "Shadow Shard";
        r1.ItemDescription= "A crystallised fragment of dark energy.\nIncreases Magic Attack by 5.";
        r1.FlavorText     = "The cursed knight's essence shatters.\nPower flows into your hands.";
        r1.AccentColor    = new Color(0.6f, 0.3f, 1f);
        EditorUtility.SetDirty(r1);

        // ── Reward for Level 2 (placeholder boss) ──────────────────────
        var r2 = LoadOrCreate<LevelRewardSO>("Assets/_Project/ScriptableObjects/Rewards/Reward_L2.asset");
        r2.XPReward       = 300;
        r2.GoldReward     = 150;
        r2.ItemName       = "Voidheart Crown";
        r2.ItemDescription= "A relic torn from the void itself.\nAll stats permanently increased.";
        r2.FlavorText     = "The void collapses.\nThe realm is saved — for now.";
        r2.AccentColor    = new Color(1f, 0.4f, 0.1f);
        EditorUtility.SetDirty(r2);

        // ── Level 0: Dark Encounter ─────────────────────────────────────
        _level0 = LoadOrCreate<LevelDataSO>("Assets/_Project/ScriptableObjects/Levels/Level0.asset");
        _level0.LevelIndex        = 0;
        _level0.LevelName         = "The Dark Encounter";
        _level0.Description       = "A lone Hero faces a fearsome Dark Knight amid ancient ruins.";
        _level0.DifficultyLabel   = "Beginner";
        _level0.DifficultyColor   = new Color(0.3f, 0.9f, 0.3f);
        _level0.CombatSceneName   = "CombatStage";
        _level0.Reward            = r0;
        _level0.UnlockedByDefault = true;
        _level0.UnlocksLevels     = new[] { 1 };
        _level0.MapPosition       = new Vector2(-4f, 0f);
        EditorUtility.SetDirty(_level0);

        // ── Level 1: Cursed Depths ──────────────────────────────────────
        _level1 = LoadOrCreate<LevelDataSO>("Assets/_Project/ScriptableObjects/Levels/Level1.asset");
        _level1.LevelIndex        = 1;
        _level1.LevelName         = "The Cursed Depths";
        _level1.Description       = "Deeper in the ruin, a more powerful evil stirs.\nSteel your resolve.";
        _level1.DifficultyLabel   = "Intermediate";
        _level1.DifficultyColor   = new Color(1f, 0.7f, 0.1f);
        _level1.CombatSceneName   = "CombatStage";
        _level1.Reward            = r1;
        _level1.UnlockedByDefault = false;
        _level1.UnlocksLevels     = new[] { 2 };
        _level1.MapPosition       = new Vector2(0f, 0.3f);
        EditorUtility.SetDirty(_level1);

        // ── Level 2: Void Boss (locked placeholder) ─────────────────────
        _level2 = LoadOrCreate<LevelDataSO>("Assets/_Project/ScriptableObjects/Levels/Level2.asset");
        _level2.LevelIndex        = 2;
        _level2.LevelName         = "The Void Gate";
        _level2.Description       = "The gate between worlds tears open.\nOnly the strongest may pass.";
        _level2.DifficultyLabel   = "Boss";
        _level2.DifficultyColor   = new Color(0.9f, 0.2f, 0.2f);
        _level2.CombatSceneName   = "CombatStage";
        _level2.Reward            = r2;
        _level2.UnlockedByDefault = false;
        _level2.UnlocksLevels     = new int[0];
        _level2.MapPosition       = new Vector2(4f, 0f);
        EditorUtility.SetDirty(_level2);

        // ── LevelRegistry ───────────────────────────────────────────────
        var registry = LoadOrCreate<RPG.Data.LevelRegistry>(
            "Assets/_Project/ScriptableObjects/Levels/LevelRegistry.asset");
        registry.Levels = new[] { _level0, _level1, _level2 };
        EditorUtility.SetDirty(registry);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  MAP SELECTOR SCENE
    // ────────────────────────────────────────────────────────────────────────────
    static void CreateMapSelectorScene()
    {
        string scenePath = "Assets/_Project/Scenes/MapSelector.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Lighting ───────────────────────────────────────────────────
        var light = new GameObject("DirLight").AddComponent<Light>();
        light.type      = LightType.Directional;
        light.color     = new Color(0.7f, 0.65f, 0.9f);
        light.intensity = 0.5f;
        light.gameObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        RenderSettings.ambientSkyColor     = new Color(0.04f, 0.03f, 0.08f);
        RenderSettings.ambientEquatorColor = new Color(0.03f, 0.03f, 0.06f);
        RenderSettings.ambientGroundColor  = new Color(0.02f, 0.02f, 0.04f);

        // ── Camera ─────────────────────────────────────────────────────
        var camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.03f, 0.09f);
        cam.fieldOfView     = 50f;
        camGo.transform.position = new Vector3(0f, 3f, -10f);
        camGo.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

        // ── Map root (nodes parented here at runtime) ──────────────────
        var mapRoot = new GameObject("MapRoot");

        // ── Build reusable prefabs ─────────────────────────────────────
        var nodePrefab       = CreateNodePrefab();
        var connectionPrefab = CreateConnectionPrefab();

        // ── MapSelectorUI ──────────────────────────────────────────────
        var registry = AssetDatabase.LoadAssetAtPath<RPG.Data.LevelRegistry>(
            "Assets/_Project/ScriptableObjects/Levels/LevelRegistry.asset");

        var (canvas, ui) = CreateMapUI(scene);
        ui.Registry         = registry;
        ui.NodePrefab       = nodePrefab;
        ui.ConnectionPrefab = connectionPrefab;
        ui.MapRoot          = mapRoot.transform;

        // ── EventSystem ────────────────────────────────────────────────
        AddEventSystem();

        EditorSceneManager.SaveScene(scene, scenePath);
        AddToBuild(scenePath);
    }

    /// <summary>
    /// Saves a reusable NodePrefab to Assets/_Project/Prefabs/Map/NodePrefab.prefab.
    /// MapSelectorUI instantiates one of these per level at runtime.
    /// </summary>
    static GameObject CreateNodePrefab()
    {
        const string prefabPath = "Assets/_Project/Prefabs/Map/NodePrefab.prefab";

        // Build the object in the scene temporarily, then save as prefab
        var root = new GameObject("NodePrefab");

        // Crystal body
        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Crystal";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.6f, 0.85f, 0.6f);
        Object.DestroyImmediate(body.GetComponent<SphereCollider>());
        var mat = CreateGlowMat("NodeCrystalMat", new Color(0.1f, 0.3f, 0.6f), new Color(0.2f, 0.6f, 1f), 4f);
        body.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // Inner glow
        var inner = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        inner.name = "InnerGlow";
        inner.transform.SetParent(root.transform, false);
        inner.transform.localScale = Vector3.one * 0.35f;
        Object.DestroyImmediate(inner.GetComponent<SphereCollider>());
        var innerMat = CreateGlowMat("NodeInnerMat", new Color(0.05f, 0.15f, 0.3f), new Color(0.2f, 0.6f, 1f), 8f);
        inner.GetComponent<MeshRenderer>().sharedMaterial = innerMat;

        // Point light
        var lightGo = new GameObject("NodeLight");
        lightGo.transform.SetParent(root.transform, false);
        var pointLight = lightGo.AddComponent<Light>();
        pointLight.type      = LightType.Point;
        pointLight.color     = new Color(0.2f, 0.6f, 1f);
        pointLight.intensity = 1.2f;
        pointLight.range     = 3f;

        // Sphere collider on root for click detection
        root.AddComponent<SphereCollider>().radius = 0.6f;

        // World-space label
        var labelCanvas = new GameObject("LabelCanvas");
        labelCanvas.transform.SetParent(root.transform, false);
        labelCanvas.transform.localPosition = new Vector3(0f, -0.85f, 0f);
        var lc = labelCanvas.AddComponent<Canvas>();
        lc.renderMode = RenderMode.WorldSpace;
        var lcRect = labelCanvas.GetComponent<RectTransform>();
        lcRect.sizeDelta  = new Vector2(2f, 0.5f);
        lcRect.localScale = Vector3.one * 0.008f;
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(labelCanvas.transform, false);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = "Level";
        tmp.fontSize  = 120f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = new Color(0.5f, 0.8f, 1f);
        tmp.fontStyle = FontStyles.Bold;
        labelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 0.5f);

        // MapNode component — data assigned at runtime by MapSelectorUI
        var node = root.AddComponent<MapNode>();
        node.CrystalRenderer = body.GetComponent<MeshRenderer>();
        node.NodeLight       = pointLight;

        // Save as prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    /// <summary>
    /// Saves a reusable ConnectionPrefab (LineRenderer) to Prefabs/Map.
    /// MapSelectorUI instantiates one per unlock edge.
    /// </summary>
    static GameObject CreateConnectionPrefab()
    {
        const string prefabPath = "Assets/_Project/Prefabs/Map/ConnectionPrefab.prefab";

        var go = new GameObject("ConnectionPrefab");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth    = 0.04f;
        lr.endWidth      = 0.04f;
        lr.useWorldSpace = true;

        var lineMat = new Material(Shader.Find("HDRP/Unlit") ?? Shader.Find("Unlit/Color"));
        if (lineMat.HasProperty("_UnlitColor")) lineMat.SetColor("_UnlitColor", new Color(0.3f, 0.4f, 0.7f, 0.5f));
        else if (lineMat.HasProperty("_Color")) lineMat.SetColor("_Color",       new Color(0.3f, 0.4f, 0.7f, 0.5f));
        lr.sharedMaterial = lineMat;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject CreateNodeObject(string name, Vector3 pos, LevelDataSO data,
        Color glowColor, int sortIndex)
    {
        var root = new GameObject(name);
        root.transform.position = pos;

        // Crystal body (gem-like using scaled sphere)
        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Crystal";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.6f, 0.85f, 0.6f);
        body.transform.localPosition = Vector3.zero;
        Object.DestroyImmediate(body.GetComponent<SphereCollider>());

        // Crystal material
        var mat = CreateGlowMat($"NodeMat_{name}", glowColor * 0.4f, glowColor, 4f);
        body.GetComponent<MeshRenderer>().material = mat;

        // Inner glow sphere (slightly smaller, more emissive)
        var inner = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        inner.name = "InnerGlow";
        inner.transform.SetParent(root.transform, false);
        inner.transform.localScale = Vector3.one * 0.35f;
        inner.transform.localPosition = Vector3.zero;
        Object.DestroyImmediate(inner.GetComponent<SphereCollider>());
        var innerMat = CreateGlowMat($"NodeInnerMat_{name}", glowColor * 0.2f, glowColor, 8f);
        inner.GetComponent<MeshRenderer>().material = innerMat;

        // Point light
        var lightGo = new GameObject("NodeLight");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = Vector3.zero;
        var pointLight = lightGo.AddComponent<Light>();
        pointLight.type      = LightType.Point;
        pointLight.color     = glowColor;
        pointLight.intensity = 1.2f;
        pointLight.range     = 3f;

        // Sphere collider on root for click detection
        var col = root.AddComponent<SphereCollider>();
        col.radius = 0.6f;

        // Label (world-space text on a Canvas)
        var labelCanvas = new GameObject("LabelCanvas");
        labelCanvas.transform.SetParent(root.transform, false);
        labelCanvas.transform.localPosition = new Vector3(0f, -0.85f, 0f);
        var lc = labelCanvas.AddComponent<Canvas>();
        lc.renderMode = RenderMode.WorldSpace;
        var lcRect = labelCanvas.GetComponent<RectTransform>();
        lcRect.sizeDelta   = new Vector2(2f, 0.5f);
        lcRect.localScale  = Vector3.one * 0.008f;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(labelCanvas.transform, false);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = data?.LevelName ?? name;
        tmp.fontSize  = 120f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = glowColor * 1.5f;
        tmp.fontStyle = FontStyles.Bold;
        labelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(2f, 0.5f);

        // MapNode component
        var node = root.AddComponent<MapNode>();
        node.LevelData       = data;
        node.CrystalRenderer = body.GetComponent<MeshRenderer>();
        node.NodeLight       = pointLight;

        return root;
    }

    static Material CreateGlowMat(string name, Color albedo, Color emissive, float emissiveMult)
    {
        string path = $"Assets/_Project/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            var shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.HasProperty("_BaseColor"))    mat.SetColor("_BaseColor",    albedo);
        if (mat.HasProperty("_Smoothness"))   mat.SetFloat("_Smoothness",   0.7f);
        if (mat.HasProperty("_Metallic"))     mat.SetFloat("_Metallic",     0.0f);
        if (mat.HasProperty("_EmissiveColor")) mat.SetColor("_EmissiveColor", emissive * emissiveMult);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void CreateConnectionLine(Vector3 from, Vector3 to)
    {
        var go = new GameObject("PathLine");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, from + Vector3.up * 0.05f);
        lr.SetPosition(1, to   + Vector3.up * 0.05f);
        lr.startWidth = 0.04f;
        lr.endWidth   = 0.04f;
        lr.useWorldSpace = true;

        var mat = new Material(Shader.Find("HDRP/Unlit") ?? Shader.Find("Unlit/Color"));
        if (mat.HasProperty("_UnlitColor")) mat.SetColor("_UnlitColor", new Color(0.3f, 0.4f, 0.7f, 0.5f));
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color",       new Color(0.3f, 0.4f, 0.7f, 0.5f));
        lr.material = mat;
    }

    // ── Map UI ────────────────────────────────────────────────────────────────
    static (GameObject canvas, MapSelectorUI ui) CreateMapUI(UnityEngine.SceneManagement.Scene scene)
    {
        var canvasGo = new GameObject("UICanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Title
        MakeText(canvasGo.transform, "TitleText", "SELECT YOUR PATH",
            42f, new Color(1f, 0.84f, 0f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -30f), new Vector2(700f, 60f));

        // Session bar (top right)
        var sessionBar = MakePanel(canvasGo.transform, "SessionBar",
            new Color(0.05f, 0.04f, 0.09f, 0.85f),
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-10f, -10f), new Vector2(260f, 60f), new Vector2(1f, 1f));

        var goldTmp  = MakeText(sessionBar.transform, "GoldText", "Gold: 0",
            18f, new Color(1f, 0.85f, 0.2f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 12f), new Vector2(240f, 26f));
        var xpTmp    = MakeText(sessionBar.transform, "XPText", "XP: 0",
            18f, new Color(0.4f, 0.8f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -12f), new Vector2(240f, 26f));

        // Preview panel (right side)
        var preview = MakePanel(canvasGo.transform, "PreviewPanel",
            new Color(0.06f, 0.05f, 0.12f, 0.93f),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-10f, 0f), new Vector2(350f, 420f), new Vector2(1f, 0.5f));
        preview.SetActive(false);

        var levelNameTmp = MakeText(preview.transform, "LevelName", "Level Name",
            28f, new Color(1f, 0.85f, 0.3f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -18f), new Vector2(320f, 42f));
        var diffTmp = MakeText(preview.transform, "Difficulty", "Normal",
            17f, new Color(0.3f, 0.9f, 0.3f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -62f), new Vector2(320f, 28f));
        var descTmp = MakeText(preview.transform, "Desc", "",
            16f, new Color(0.78f, 0.78f, 0.90f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -120f), new Vector2(320f, 90f));
        var xpReward = MakeText(preview.transform, "XPReward", "+100 XP",
            17f, new Color(0.4f, 0.85f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -220f), new Vector2(320f, 28f));
        var goldReward = MakeText(preview.transform, "GoldReward", "+50 Gold",
            17f, new Color(1f, 0.85f, 0.2f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -250f), new Vector2(320f, 28f));

        var enterBtn = MakeButton(preview.transform, "EnterBtn", "ENTER BATTLE",
            new Color(0.65f, 0.40f, 0.02f), Color.white,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 18f), new Vector2(290f, 52f), new Vector2(0.5f, 0f));

        // Main menu button (top left)
        var mmBtn = MakeButton(canvasGo.transform, "MainMenuBtn", "← Main Menu",
            new Color(0.25f, 0.25f, 0.35f), Color.white,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -10f), new Vector2(180f, 44f), new Vector2(0f, 1f));

        // Wire UI controller (Registry/NodePrefab/ConnectionPrefab/MapRoot set by caller)
        var ui = canvasGo.AddComponent<MapSelectorUI>();
        ui.PreviewPanel   = preview;
        ui.LevelNameText  = levelNameTmp;
        ui.DifficultyText = diffTmp;
        ui.LevelDescText  = descTmp;
        ui.RewardXPText   = xpReward;
        ui.RewardGoldText = goldReward;
        ui.EnterButton    = enterBtn;
        ui.GoldText       = goldTmp;
        ui.XPText         = xpTmp;
        ui.MainMenuButton = mmBtn;

        return (canvasGo, ui);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  REWARD SCENE
    // ────────────────────────────────────────────────────────────────────────────
    static void CreateRewardScene()
    {
        string scenePath = "Assets/_Project/Scenes/RewardScene.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.03f, 0.07f);
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        // Light
        var dirLight = new GameObject("DirLight").AddComponent<Light>();
        dirLight.type      = LightType.Directional;
        dirLight.intensity = 0.4f;
        dirLight.color     = new Color(0.7f, 0.7f, 0.9f);
        dirLight.gameObject.transform.rotation = Quaternion.Euler(40f, -20f, 0f);

        // Ambient particles (floating motes)
        CreateAmbientParticles();

        // UI
        CreateRewardUI();

        // EventSystem
        AddEventSystem();

        EditorSceneManager.SaveScene(scene, scenePath);
        AddToBuild(scenePath);
    }

    static void CreateAmbientParticles()
    {
        var go = new GameObject("AmbientParticles");
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startColor    = new ParticleSystem.MinMaxGradient(
            new Color(0.4f, 0.3f, 0.9f, 0.3f), new Color(1f, 0.8f, 0.2f, 0.5f));
        main.startSize     = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.loop          = true;
        main.playOnAwake   = true;
        main.maxParticles  = 120;

        var emission = ps.emission;
        emission.rateOverTime = 15f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(14f, 8f, 2f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.6f, 0.2f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        ps.Play();
    }

    static void CreateRewardUI()
    {
        var canvasGo = new GameObject("UICanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var cg = canvasGo.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        // ── Background glow panel ─────────────────────────────────────
        MakePanel(canvasGo.transform, "BGPanel",
            new Color(0.04f, 0.03f, 0.08f, 1f),
            Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

        // ── Victory title ─────────────────────────────────────────────
        var victoryTmp = MakeText(canvasGo.transform, "VictoryText", "VICTORY!",
            88f, new Color(1f, 0.84f, 0f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 230f), new Vector2(800f, 120f));

        var flavorTmp = MakeText(canvasGo.transform, "FlavorText", "",
            24f, new Color(0.80f, 0.78f, 0.95f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 155f), new Vector2(700f, 70f));

        // ── Divider ───────────────────────────────────────────────────
        MakePanel(canvasGo.transform, "Divider",
            new Color(0.5f, 0.4f, 0.1f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 100f), new Vector2(600f, 2f), new Vector2(0.5f, 0.5f));

        // ── XP row ───────────────────────────────────────────────────
        var xpLabel = MakeText(canvasGo.transform, "XPLabel", "Experience Gained",
            22f, new Color(0.6f, 0.85f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-80f, 50f), new Vector2(340f, 36f));
        xpLabel.alignment = TextAlignmentOptions.Right;

        var xpValue = MakeText(canvasGo.transform, "XPValue", "0",
            28f, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(120f, 50f), new Vector2(200f, 36f));
        xpValue.alignment = TextAlignmentOptions.Left;

        // ── Gold row ─────────────────────────────────────────────────
        var goldLabel = MakeText(canvasGo.transform, "GoldLabel", "Gold Found",
            22f, new Color(1f, 0.85f, 0.2f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-80f, 5f), new Vector2(340f, 36f));
        goldLabel.alignment = TextAlignmentOptions.Right;

        var goldValue = MakeText(canvasGo.transform, "GoldValue", "0",
            28f, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(120f, 5f), new Vector2(200f, 36f));
        goldValue.alignment = TextAlignmentOptions.Left;

        // ── Session totals ────────────────────────────────────────────
        var sessXP = MakeText(canvasGo.transform, "SessionXP", "",
            17f, new Color(0.5f, 0.7f, 0.9f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-60f, -45f), new Vector2(380f, 28f));
        sessXP.alignment = TextAlignmentOptions.Right;

        var sessGold = MakeText(canvasGo.transform, "SessionGold", "",
            17f, new Color(0.9f, 0.75f, 0.2f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-60f, -75f), new Vector2(380f, 28f));
        sessGold.alignment = TextAlignmentOptions.Right;

        // ── Item card ─────────────────────────────────────────────────
        var itemCard = MakePanel(canvasGo.transform, "ItemCard",
            new Color(0.12f, 0.08f, 0.20f, 0.90f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -155f), new Vector2(480f, 100f), new Vector2(0.5f, 0.5f));

        MakeText(itemCard.transform, "DropLabel", "ITEM FOUND",
            13f, new Color(0.9f, 0.6f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -8f), new Vector2(440f, 22f));

        var itemNameTmp = MakeText(itemCard.transform, "ItemName", "",
            22f, new Color(1f, 0.9f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(20f, 10f), new Vector2(380f, 32f));
        var itemDescTmp = MakeText(itemCard.transform, "ItemDesc", "",
            15f, new Color(0.75f, 0.75f, 0.88f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(20f, -20f), new Vector2(380f, 36f));
        itemCard.SetActive(false);

        // ── Continue button ───────────────────────────────────────────
        var continueBtn = MakeButton(canvasGo.transform, "ContinueBtn", "CONTINUE",
            new Color(0.55f, 0.38f, 0.02f), Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -280f), new Vector2(260f, 60f), new Vector2(0.5f, 0.5f));

        // ── Wire RewardUI ─────────────────────────────────────────────
        var rui = canvasGo.AddComponent<RewardUI>();
        rui.RootGroup       = cg;
        rui.VictoryText     = victoryTmp;
        rui.FlavorText      = flavorTmp;
        rui.XPLabel         = xpLabel;
        rui.XPValue         = xpValue;
        rui.GoldLabel       = goldLabel;
        rui.GoldValue       = goldValue;
        rui.SessionXPText   = sessXP;
        rui.SessionGoldText = sessGold;
        rui.ItemCard        = itemCard;
        rui.ItemName        = itemNameTmp;
        rui.ItemDesc        = itemDescTmp;
        rui.ContinueButton  = continueBtn;
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  PATCH MAIN MENU — "Start" now goes to MapSelector
    // ────────────────────────────────────────────────────────────────────────────
    static void PatchMainMenuButton()
    {
        // MainMenuUI already calls GameManager.StartCombat() which now routes to GoToMapSelector()
        // Nothing extra needed — the GameManager rewrite handles it.
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  EVENT SYSTEMS
    // ────────────────────────────────────────────────────────────────────────────
    static void AddEventSystems()
    {
        string[] scenes = {
            "Assets/_Project/Scenes/MapSelector.unity",
            "Assets/_Project/Scenes/RewardScene.unity",
        };
        foreach (var path in scenes)
        {
            var s = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            AddEventSystem();
            EditorSceneManager.SaveScene(s, path);
        }
    }

    static void AddEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();

        System.Type moduleType = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            moduleType = asm.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
            if (moduleType != null) break;
        }
        if (moduleType != null) go.AddComponent(moduleType);
        else                    go.AddComponent<StandaloneInputModule>();
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  UI HELPERS
    // ────────────────────────────────────────────────────────────────────────────
    static GameObject MakePanel(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size,
        Vector2 pivot)
    {
        var go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        var img  = go.AddComponent<Image>();
        img.color = color;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.pivot = pivot;
        r.anchoredPosition = anchoredPos; r.sizeDelta = size;
        return go;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text,
        float size, Color color,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = anchoredPos; r.sizeDelta = sizeDelta;
        return tmp;
    }

    static Button MakeButton(Transform parent, string name, string label,
        Color bg, Color textColor,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 size, Vector2 pivot)
    {
        var go   = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        var img  = go.AddComponent<Image>(); img.color = bg;
        var btn  = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = bg * 1.25f;
        colors.pressedColor     = bg * 0.70f;
        btn.colors = colors;
        btn.targetGraphic = img;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.pivot = pivot;
        r.anchoredPosition = anchoredPos; r.sizeDelta = size;

        var lgo = new GameObject("Label");
        lgo.AddComponent<RectTransform>();
        lgo.transform.SetParent(go.transform, false);
        var tmp = lgo.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 20f; tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        var lr = lgo.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;
        return btn;
    }

    static void AddToBuild(string path)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == path) return;
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
        return a;
    }
}
