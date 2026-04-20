using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System.IO;
using TMPro;
using RPG.Data;
using RPG.Grid;
using RPG.Units;
using RPG.Combat;
using RPG.VFX;
using RPG.UI;
using RPG.Core;

/// <summary>
/// One-click setup: creates scenes, materials, prefabs, and ScriptableObjects
/// for the FFT-style tactical RPG demo.
/// </summary>
public class RPGSetup
{
    // ────────────────────────────────────────────────────────────────────────────
    //  ENTRY POINT
    // ────────────────────────────────────────────────────────────────────────────
    [MenuItem("RPG/Setup Full Demo")]
    public static void Execute()
    {
        EnsureFolders();
        CreateMaterials();
        CreateVFXPrefabs();
        CreateScriptableObjects();
        CreateTilePrefab();
        CreateUnitPrefabs();
        CreateFloatingTextPrefab();
        CreateMainMenuScene();
        CreateCombatStageScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RPGSetup] ✅ Full demo setup complete!");
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  FOLDER STRUCTURE
    // ────────────────────────────────────────────────────────────────────────────
    static void EnsureFolders()
    {
        string[] folders =
        {
            "Assets/_Project/Scenes",
            "Assets/_Project/Materials",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Prefabs/VFX",
            "Assets/_Project/Prefabs/UI",
            "Assets/_Project/ScriptableObjects/Stats",
            "Assets/_Project/ScriptableObjects/Abilities",
        };
        foreach (var f in folders)
        {
            var parts = f.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  MATERIALS  (HDRP Lit with emissive highlight colours)
    // ────────────────────────────────────────────────────────────────────────────
    static Material _tileMat, _moveMat, _attackMat, _selectedMat;
    static Material _heroMat, _enemyMat;

    static void CreateMaterials()
    {
        _tileMat     = MakeMat("TileDefault",  new Color(0.35f, 0.28f, 0.20f), Color.black,               0f);
        _moveMat     = MakeMat("TileMove",     new Color(0.18f, 0.35f, 0.60f), new Color(0f, 0.4f, 1f),   1.2f);
        _attackMat   = MakeMat("TileAttack",   new Color(0.55f, 0.12f, 0.10f), new Color(1f, 0.1f, 0.05f),1.2f);
        _selectedMat = MakeMat("TileSelected", new Color(0.55f, 0.50f, 0.10f), new Color(1f, 0.85f, 0f),  1.5f);
        _heroMat     = MakeMat("HeroUnit",     new Color(0.15f, 0.40f, 0.85f), new Color(0.2f, 0.6f, 1f), 0.6f);
        _enemyMat    = MakeMat("EnemyUnit",    new Color(0.70f, 0.10f, 0.10f), new Color(1f, 0.15f, 0.1f),0.6f);
    }

    static Material MakeMat(string name, Color albedo, Color emissive, float emissiveIntensity)
    {
        string path = $"Assets/_Project/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            // Try HDRP Lit, fall back to Standard
            Shader shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor",      albedo);
        mat.SetColor("_EmissiveColor",  emissive * Mathf.Pow(2, emissiveIntensity));

        // HDRP emissive enable
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  VFX PREFABS  (ParticleSystem-based, HDRP-compatible)
    // ────────────────────────────────────────────────────────────────────────────
    static ParticleSystem _attackVFX, _magicVFX, _specialVFX, _healVFX, _defendVFX;

    static void CreateVFXPrefabs()
    {
        _attackVFX  = MakeParticlePrefab("VFX_Attack",  new Color(1f, 0.6f, 0.1f),  24, 0.6f, 0.8f, false);
        _magicVFX   = MakeParticlePrefab("VFX_Magic",   new Color(0.3f, 0.5f, 1f),  32, 0.8f, 1.0f, false);
        _specialVFX = MakeParticlePrefab("VFX_Special", new Color(0.9f, 0.2f, 1f),  48, 1.2f, 1.5f, true);
        _healVFX    = MakeParticlePrefab("VFX_Heal",    new Color(0.2f, 1f, 0.4f),  20, 0.5f, 0.8f, false);
        _defendVFX  = MakeParticlePrefab("VFX_Defend",  new Color(0.4f, 0.8f, 1f),  16, 0.4f, 0.6f, false);
    }

    static ParticleSystem MakeParticlePrefab(string name, Color color,
        int count, float size, float speed, bool isBig)
    {
        string path = $"Assets/_Project/Prefabs/VFX/{name}.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing.GetComponent<ParticleSystem>();

        var go = new GameObject(name);
        var ps = go.AddComponent<ParticleSystem>();
        go.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.Billboard;

        var main = ps.main;
        main.startColor    = color;
        main.startSize     = new ParticleSystem.MinMaxCurve(size * 0.5f, size * 1.5f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed * 2f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.duration      = 0.4f;
        main.loop          = false;
        main.playOnAwake   = false;

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        emission.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = isBig ? 0.4f : 0.2f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.white, 0.3f), new GradientColorKey(color * 0.4f, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        var sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f));
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Add a glow child (bigger, fewer particles)
        if (isBig)
        {
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(go.transform, false);
            var glowPs = glowGo.AddComponent<ParticleSystem>();
            var gMain  = glowPs.main;
            gMain.startColor    = new Color(color.r, color.g, color.b, 0.4f);
            gMain.startSize     = new ParticleSystem.MinMaxCurve(size * 2f, size * 3f);
            gMain.startSpeed    = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            gMain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            gMain.duration      = 0.5f;
            gMain.loop          = false;
            gMain.playOnAwake   = false;
            var gEmit = glowPs.emission;
            gEmit.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
            gEmit.rateOverTime = 0;
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<ParticleSystem>();
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  SCRIPTABLE OBJECTS  (Stats + Abilities)
    // ────────────────────────────────────────────────────────────────────────────
    static UnitStatsSO _heroStats, _enemyStats;
    static AbilitySO _atkAbility, _defAbility, _spcAbility;
    static AbilitySO _enemyAtk, _enemySpc;

    static void CreateScriptableObjects()
    {
        // ── Hero Stats ──────────────────────────────────────────────────
        _heroStats = LoadOrCreate<UnitStatsSO>("Assets/_Project/ScriptableObjects/Stats/HeroStats.asset");
        _heroStats.UnitName    = "Hero";
        _heroStats.ClassName   = "Hero";
        _heroStats.UnitColor   = new Color(0.2f, 0.6f, 1f);
        _heroStats.MaxHP       = 120;
        _heroStats.MaxMP       = 60;
        _heroStats.Attack      = 14;
        _heroStats.Defense     = 8;
        _heroStats.MagicAttack = 12;
        _heroStats.MagicDefense= 8;
        _heroStats.Speed       = 9;
        _heroStats.Movement    = 4;
        EditorUtility.SetDirty(_heroStats);

        // ── Enemy Stats ─────────────────────────────────────────────────
        _enemyStats = LoadOrCreate<UnitStatsSO>("Assets/_Project/ScriptableObjects/Stats/DarkKnightStats.asset");
        _enemyStats.UnitName    = "Dark Knight";
        _enemyStats.ClassName   = "Dark Knight";
        _enemyStats.UnitColor   = new Color(0.9f, 0.15f, 0.1f);
        _enemyStats.MaxHP       = 160;
        _enemyStats.MaxMP       = 40;
        _enemyStats.Attack      = 18;
        _enemyStats.Defense     = 12;
        _enemyStats.MagicAttack = 10;
        _enemyStats.MagicDefense= 6;
        _enemyStats.Speed       = 7;
        _enemyStats.Movement    = 3;
        EditorUtility.SetDirty(_enemyStats);

        // ── Hero: Attack ────────────────────────────────────────────────
        _atkAbility = LoadOrCreate<AbilitySO>("Assets/_Project/ScriptableObjects/Abilities/Attack.asset");
        _atkAbility.AbilityName       = "Strike";
        _atkAbility.Description       = "A swift physical blow.";
        _atkAbility.Type              = AbilityType.Physical;
        _atkAbility.Target            = AbilityTarget.SingleEnemy;
        _atkAbility.Range             = 1;
        _atkAbility.MPCost            = 0;
        _atkAbility.DamageMultiplier  = 1.2f;
        _atkAbility.FlatDamage        = 2;
        _atkAbility.VFXKey            = "attack";
        _atkAbility.EffectColor       = new Color(1f, 0.65f, 0.1f);
        EditorUtility.SetDirty(_atkAbility);

        // ── Hero: Defend ────────────────────────────────────────────────
        _defAbility = LoadOrCreate<AbilitySO>("Assets/_Project/ScriptableObjects/Abilities/Defend.asset");
        _defAbility.AbilityName       = "Guard";
        _defAbility.Description       = "Take a defensive stance. DEF x2 until next turn.";
        _defAbility.Type              = AbilityType.Support;
        _defAbility.Target            = AbilityTarget.Self;
        _defAbility.Range             = 0;
        _defAbility.MPCost            = 0;
        _defAbility.ApplyDefendBuff   = true;
        _defAbility.VFXKey            = "defend";
        _defAbility.EffectColor       = new Color(0.4f, 0.8f, 1f);
        EditorUtility.SetDirty(_defAbility);

        // ── Hero: Special ───────────────────────────────────────────────
        _spcAbility = LoadOrCreate<AbilitySO>("Assets/_Project/ScriptableObjects/Abilities/Special.asset");
        _spcAbility.AbilityName       = "Nova Burst";
        _spcAbility.Description       = "Unleash a burst of magical energy. AOE radius 1. Costs 20 MP.";
        _spcAbility.Type              = AbilityType.Magical;
        _spcAbility.Target            = AbilityTarget.SingleEnemy;
        _spcAbility.Range             = 3;
        _spcAbility.AOERadius         = 1;
        _spcAbility.MPCost            = 20;
        _spcAbility.DamageMultiplier  = 1.8f;
        _spcAbility.FlatDamage        = 5;
        _spcAbility.VFXKey            = "special";
        _spcAbility.EffectColor       = new Color(0.85f, 0.2f, 1f);
        EditorUtility.SetDirty(_spcAbility);

        // ── Enemy: Attack ───────────────────────────────────────────────
        _enemyAtk = LoadOrCreate<AbilitySO>("Assets/_Project/ScriptableObjects/Abilities/EnemyAttack.asset");
        _enemyAtk.AbilityName       = "Dark Slash";
        _enemyAtk.Description       = "A powerful dark-infused slash.";
        _enemyAtk.Type              = AbilityType.Physical;
        _enemyAtk.Target            = AbilityTarget.SingleEnemy;
        _enemyAtk.Range             = 1;
        _enemyAtk.MPCost            = 0;
        _enemyAtk.DamageMultiplier  = 1.3f;
        _enemyAtk.FlatDamage        = 3;
        _enemyAtk.VFXKey            = "attack";
        _enemyAtk.EffectColor       = new Color(0.8f, 0.1f, 0.1f);
        EditorUtility.SetDirty(_enemyAtk);

        // ── Enemy: Special ──────────────────────────────────────────────
        _enemySpc = LoadOrCreate<AbilitySO>("Assets/_Project/ScriptableObjects/Abilities/EnemySpecial.asset");
        _enemySpc.AbilityName       = "Soul Crush";
        _enemySpc.Description       = "Channels dark power for a devastating strike.";
        _enemySpc.Type              = AbilityType.Magical;
        _enemySpc.Target            = AbilityTarget.SingleEnemy;
        _enemySpc.Range             = 2;
        _enemySpc.MPCost            = 15;
        _enemySpc.DamageMultiplier  = 2.2f;
        _enemySpc.FlatDamage        = 8;
        _enemySpc.VFXKey            = "special";
        _enemySpc.EffectColor       = new Color(0.4f, 0f, 0.6f);
        EditorUtility.SetDirty(_enemySpc);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  TILE PREFAB
    // ────────────────────────────────────────────────────────────────────────────
    static GameObject _tilePrefab;

    static void CreateTilePrefab()
    {
        string path = "Assets/_Project/Prefabs/GridTile.prefab";

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "GridTile";

        var tile = go.AddComponent<GridTile>();
        tile.TileRenderer = go.GetComponent<MeshRenderer>();
        tile.TileRenderer.material = _tileMat;

        // Add collider for mouse interaction
        go.GetComponent<BoxCollider>(); // already exists on cube

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        _tilePrefab = prefab;
        Object.DestroyImmediate(go);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  UNIT PREFABS  (Capsule with material + WorldHealthBar)
    // ────────────────────────────────────────────────────────────────────────────
    static GameObject _heroPrefab, _enemyPrefab;

    static void CreateUnitPrefabs()
    {
        _heroPrefab  = MakeUnitPrefab("HeroPrefab",  _heroMat,  true);
        _enemyPrefab = MakeUnitPrefab("EnemyPrefab", _enemyMat, false);
    }

    static GameObject MakeUnitPrefab(string name, Material mat, bool isHero)
    {
        string path = $"Assets/_Project/Prefabs/{name}.prefab";

        // Body
        var root = new GameObject(name);

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
        body.GetComponent<MeshRenderer>().material = mat;
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

        // Head indicator (small sphere on top)
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        head.transform.localScale    = Vector3.one * 0.3f;
        head.GetComponent<MeshRenderer>().material = mat;
        Object.DestroyImmediate(head.GetComponent<SphereCollider>());

        // Direction indicator (forward cube)
        var dirIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dirIndicator.name = "DirectionDot";
        dirIndicator.transform.SetParent(root.transform, false);
        dirIndicator.transform.localPosition = new Vector3(0f, 0.3f, 0.32f);
        dirIndicator.transform.localScale    = new Vector3(0.15f, 0.15f, 0.15f);
        dirIndicator.GetComponent<MeshRenderer>().material = mat;
        Object.DestroyImmediate(dirIndicator.GetComponent<BoxCollider>());

        // Unit component
        Unit unit;
        if (isHero)
            unit = root.AddComponent<HeroUnit>();
        else
            unit = root.AddComponent<EnemyUnit>();
        unit.UnitRenderer = body.GetComponent<MeshRenderer>();

        // World health bar (Canvas — world space)
        var canvasGo = new GameObject("HealthBarCanvas");
        canvasGo.transform.SetParent(root.transform, false);
        canvasGo.transform.localPosition = new Vector3(0f, 1.3f, 0f);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var cr = canvasGo.GetComponent<RectTransform>();
        cr.sizeDelta = new Vector2(1f, 0.25f);
        cr.localScale = Vector3.one * 0.012f;

        // HP bar
        var hpGo   = new GameObject("HPBar");
        hpGo.transform.SetParent(canvasGo.transform, false);
        var hpSlider = hpGo.AddComponent<Slider>();
        hpSlider.value = 1f;
        var hpRect = hpGo.GetComponent<RectTransform>();
        hpRect.sizeDelta        = new Vector2(80f, 10f);
        hpRect.anchoredPosition = Vector2.zero;

        // BG
        var hpBg = new GameObject("BG");
        hpBg.transform.SetParent(hpGo.transform, false);
        var hpBgImg = hpBg.AddComponent<Image>();
        hpBgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        var hpBgRect = hpBg.GetComponent<RectTransform>();
        hpBgRect.anchorMin = Vector2.zero; hpBgRect.anchorMax = Vector2.one;
        hpBgRect.offsetMin = Vector2.zero; hpBgRect.offsetMax = Vector2.zero;
        hpSlider.targetGraphic = hpBgImg;

        // Fill area
        var fillAreaGo = new GameObject("FillArea");
        fillAreaGo.transform.SetParent(hpGo.transform, false);
        var faRect = fillAreaGo.AddComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero; faRect.anchorMax = Vector2.one;
        faRect.offsetMin = new Vector2(2f, 2f); faRect.offsetMax = new Vector2(-2f, -2f);
        hpSlider.fillRect = faRect;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = isHero ? new Color(0.2f, 0.85f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
        hpSlider.fillRect = fillRect;

        // WorldHealthBar component — drives Image.fillAmount, not Slider
        var whb = canvasGo.AddComponent<WorldHealthBar>();
        whb.HPFill = fillImg;

        // Save prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        if (isHero) _heroPrefab = prefab;
        else        _enemyPrefab = prefab;

        return prefab;
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  FLOATING TEXT PREFAB
    // ────────────────────────────────────────────────────────────────────────────
    static GameObject _floatingTextPrefab;

    static void CreateFloatingTextPrefab()
    {
        string path = "Assets/_Project/Prefabs/UI/FloatingText.prefab";

        var go = new GameObject("FloatingText");
        go.AddComponent<RectTransform>();           // must exist before TMP
        var cg = go.AddComponent<CanvasGroup>();

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200f, 60f);

        var label = go.AddComponent<TextMeshProUGUI>();
        label.text      = "0";
        label.fontSize  = 40f;
        label.alignment = TextAlignmentOptions.Center;
        label.color     = Color.white;

        var ft = go.AddComponent<FloatingText>();
        ft.Label = label;
        ft.Group = cg;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        _floatingTextPrefab = prefab;
        Object.DestroyImmediate(go);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  MAIN MENU SCENE
    // ────────────────────────────────────────────────────────────────────────────
    static void CreateMainMenuScene()
    {
        string scenePath = "Assets/_Project/Scenes/MainMenu.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Bootstrap ──────────────────────────────────────────────────
        var bootstrap = new GameObject("Bootstrap");
        bootstrap.AddComponent<SceneBootstrap>();

        // ── Background Directional Light ───────────────────────────────
        var lightGo = new GameObject("DirectionalLight");
        var light   = lightGo.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1f;
        light.color     = new Color(0.95f, 0.9f, 0.85f);
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // ── Camera ─────────────────────────────────────────────────────
        var camGo  = new GameObject("MainCamera");
        var cam    = camGo.AddComponent<Camera>();
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.05f, 0.04f, 0.08f);
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        // ── UI Canvas ──────────────────────────────────────────────────
        var canvasGo = new GameObject("UICanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Background panel
        var bgPanel = MakePanel(canvasGo.transform, "BackgroundPanel",
            new Color(0.06f, 0.04f, 0.10f, 1f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // ── Title ──────────────────────────────────────────────────────
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(canvasGo.transform, false);
        var titleTMP = titleGo.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "REALM OF TACTICS";
        titleTMP.fontSize  = 72f;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color     = new Color(1f, 0.84f, 0f);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 220f);
        titleRect.sizeDelta = new Vector2(900f, 100f);

        var subtitleGo = new GameObject("Subtitle");
        subtitleGo.transform.SetParent(canvasGo.transform, false);
        var subTMP = subtitleGo.AddComponent<TextMeshProUGUI>();
        subTMP.text      = "Card-Driven Tactical RPG";
        subTMP.fontSize  = 28f;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color     = new Color(0.7f, 0.7f, 0.9f);
        var subRect = subtitleGo.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 0.5f);
        subRect.anchorMax = new Vector2(0.5f, 0.5f);
        subRect.anchoredPosition = new Vector2(0f, 155f);
        subRect.sizeDelta = new Vector2(600f, 50f);

        // ── Tagline ────────────────────────────────────────────────────
        MakeText(canvasGo.transform, "Tagline",
            "Build your deck. Fight your way. Survive.",
            22f, new Color(0.55f, 0.55f, 0.75f), new Vector2(0f, 100f), new Vector2(700f, 40f));

        // ── New Run Button ─────────────────────────────────────────────
        var newRunBtn = MakeButton(canvasGo.transform, "NewRunButton", "NEW RUN",
            new Color(0.8f, 0.6f, 0.05f), new Color(0.05f, 0.04f, 0.02f),
            new Vector2(0f, 0f), new Vector2(280f, 60f));

        // ── Continue Button (hidden until save exists — MainMenuUI handles this) ───
        var continueBtn = MakeButton(canvasGo.transform, "ContinueButton", "CONTINUE",
            new Color(0.18f, 0.36f, 0.55f), new Color(0.85f, 0.9f, 1f),
            new Vector2(0f, -80f), new Vector2(280f, 60f));

        // ── Quit Button ────────────────────────────────────────────────
        var quitBtn = MakeButton(canvasGo.transform, "QuitButton", "QUIT",
            new Color(0.22f, 0.10f, 0.10f), new Color(0.9f, 0.6f, 0.6f),
            new Vector2(0f, -160f), new Vector2(180f, 44f));

        // ── MainMenuUI component ───────────────────────────────────────
        var mainMenuUI = canvasGo.AddComponent<MainMenuUI>();
        mainMenuUI.NewRunButton    = newRunBtn;
        mainMenuUI.ContinueButton  = continueBtn;
        mainMenuUI.QuitButton      = quitBtn;
        if (titleTMP) mainMenuUI.TitleText    = titleTMP;
        if (subTMP)   mainMenuUI.SubtitleText = subTMP;

        EditorSceneManager.SaveScene(scene, scenePath);
        AddSceneToBuild(scenePath);
        Debug.Log("[RPGSetup] MainMenu scene created.");
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  COMBAT STAGE SCENE
    // ────────────────────────────────────────────────────────────────────────────
    static void CreateCombatStageScene()
    {
        string scenePath = "Assets/_Project/Scenes/CombatStage.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Lighting ───────────────────────────────────────────────────
        SetupCombatLighting();

        // ── Camera ─────────────────────────────────────────────────────
        var camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView     = 45f;
        cam.nearClipPlane   = 0.1f;
        cam.farClipPlane    = 100f;
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.05f, 0.10f);
        camGo.transform.position = new Vector3(5f, 14f, -6f);
        camGo.transform.rotation = Quaternion.Euler(52f, 0f, 0f);

        var camCtrl = camGo.AddComponent<CameraController>();

        // ── Grid ───────────────────────────────────────────────────────
        var gridGo = new GameObject("BattleGrid");
        var grid   = gridGo.AddComponent<BattleGrid>();
        grid.TilePrefab    = _tilePrefab != null
            ? _tilePrefab.GetComponent<GridTile>()
            : null;
        grid.DefaultTileMat = _tileMat;
        grid.MoveTileMat    = _moveMat;
        grid.AttackTileMat  = _attackMat;
        grid.SelectedTileMat= _selectedMat;
        grid.Width  = 8;
        grid.Depth  = 8;
        grid.TileSize = 1.2f;

        // ── Trait System ───────────────────────────────────────────────
        var traitGo = new GameObject("TraitSystem");
        var traitSys = traitGo.AddComponent<TraitSystem>();

        // ── VFX Manager ────────────────────────────────────────────────
        var vfxGo = new GameObject("CombatVFXManager");
        var vfx   = vfxGo.AddComponent<CombatVFXManager>();
        vfx.AttackVFXPrefab  = _attackVFX;
        vfx.MagicVFXPrefab   = _magicVFX;
        vfx.SpecialVFXPrefab = _specialVFX;
        vfx.HealVFXPrefab    = _healVFX;
        vfx.DefendVFXPrefab  = _defendVFX;

        // ── Combat Manager ─────────────────────────────────────────────
        var cmGo = new GameObject("CombatManager");
        var cm   = cmGo.AddComponent<CombatManager>();
        cm.Grid        = grid;
        cm.TraitSystem = traitSys;
        cm.VFXManager  = vfx;

        cm.HeroStats  = _heroStats;
        cm.EnemyStats = _enemyStats;

        cm.AttackAbility = _atkAbility;
        cm.DefendAbility = _defAbility;
        cm.SpecialAbility= _spcAbility;
        cm.EnemyAttackAbility  = _enemyAtk;
        cm.EnemySpecialAbility = _enemySpc;

        // Spawn positions are now arrays on CombatManager
        if (cm.PlayerSpawns == null || cm.PlayerSpawns.Length == 0)
            cm.PlayerSpawns = new[] { new Vector2Int(1, 3) };
        if (cm.EnemySpawns == null || cm.EnemySpawns.Length == 0)
            cm.EnemySpawns  = new[] { new Vector2Int(6, 4) };

        // Prefab references
        if (_heroPrefab  != null) cm.HeroPrefab  = _heroPrefab.GetComponent<HeroUnit>();
        if (_enemyPrefab != null) cm.EnemyPrefab = _enemyPrefab.GetComponent<EnemyUnit>();

        // ── UI ─────────────────────────────────────────────────────────
        var (hud, heroPanel, enemyPanel, resultScreen) = CreateCombatUI(vfx);
        vfx.WorldCanvas = hud.GetComponent<Canvas>() != null ? hud.GetComponent<Canvas>() :
                          hud.GetComponentInParent<Canvas>();

        // ── Camera target → grid center ────────────────────────────────
        var camTarget = new GameObject("CameraTarget");
        camTarget.transform.position = new Vector3(
            (grid.Width  - 1) * grid.TileSize * 0.5f,
            0f,
            (grid.Depth  - 1) * grid.TileSize * 0.5f
        );
        camCtrl.Target = camTarget.transform;
        camCtrl.Distance  = 16f;
        camCtrl.Elevation = 52f;
        camCtrl.Azimuth   = 35f;

        EditorSceneManager.SaveScene(scene, scenePath);
        AddSceneToBuild(scenePath);
        Debug.Log("[RPGSetup] CombatStage scene created.");
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  COMBAT UI
    // ────────────────────────────────────────────────────────────────────────────
    static (GameObject hud, UnitStatusPanel hp, UnitStatusPanel ep, ResultScreenUI rs)
        CreateCombatUI(CombatVFXManager vfxMgr)
    {
        // ── Root Canvas ────────────────────────────────────────────────
        var canvasGo = new GameObject("UICanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Assign to VFX manager
        vfxMgr.WorldCanvas = canvas;

        // Floating text prefab ref
        if (_floatingTextPrefab != null)
            vfxMgr.FloatingTextPrefab = _floatingTextPrefab.GetComponent<FloatingText>();

        // ── Flash Panel ────────────────────────────────────────────────
        var flashGo = new GameObject("FlashPanel");
        flashGo.transform.SetParent(canvasGo.transform, false);
        var flashImg = flashGo.AddComponent<Image>();
        flashImg.color = new Color(1f, 1f, 1f, 0f);
        var flashRect = flashGo.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero; flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero; flashRect.offsetMax = Vector2.zero;
        flashGo.SetActive(false);
        vfxMgr.FlashPanel = flashImg;

        // ── Combat Log ─────────────────────────────────────────────────
        var logPanel = MakePanel(canvasGo.transform, "CombatLog",
            new Color(0.05f, 0.04f, 0.08f, 0.80f),
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(10f, 10f), new Vector2(460f, 150f));

        var logText = MakeText(logPanel.transform, "LogText", "",
            16f, new Color(0.85f, 0.85f, 1f), new Vector2(0f, 0f), new Vector2(440f, 140f));
        // logText is already TextMeshProUGUI
        logText.alignment = TextAlignmentOptions.BottomLeft;

        // ── Hero Status Panel (bottom left) ────────────────────────────
        var heroPanel = CreateStatusPanel(canvasGo.transform, "HeroStatus",
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(10f, 170f), new Vector2(300f, 130f),
            new Color(0.08f, 0.12f, 0.22f, 0.92f));

        // ── Enemy Status Panel (bottom right) ─────────────────────────
        var enemyPanel = CreateStatusPanel(canvasGo.transform, "EnemyStatus",
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-10f, 170f), new Vector2(300f, 130f),
            new Color(0.22f, 0.08f, 0.08f, 0.92f));
        var epRect = enemyPanel.GetComponent<RectTransform>();
        epRect.pivot = new Vector2(1f, 0f);

        // ── Phase Banner (center top) ──────────────────────────────────
        var bannerGo = new GameObject("PhaseBanner");
        bannerGo.transform.SetParent(canvasGo.transform, false);
        var bannerRect = bannerGo.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 1f);
        bannerRect.anchorMax = new Vector2(0.5f, 1f);
        bannerRect.pivot     = new Vector2(0.5f, 1f);
        bannerRect.anchoredPosition = new Vector2(0f, -20f);
        bannerRect.sizeDelta = new Vector2(600f, 70f);
        var bannerBg = bannerGo.AddComponent<Image>();
        bannerBg.color = new Color(0.05f, 0.04f, 0.10f, 0.85f);

        var bannerTMP = MakeText(bannerGo.transform, "BannerText", "Battle Start!",
            36f, new Color(1f, 0.9f, 0.3f), Vector2.zero, new Vector2(580f, 60f));
        bannerTMP.alignment = TextAlignmentOptions.Center;
        bannerGo.SetActive(false);

        // ── Result Screen ──────────────────────────────────────────────
        var resultGo = new GameObject("ResultScreen");
        resultGo.AddComponent<RectTransform>();
        resultGo.transform.SetParent(canvasGo.transform, false);
        var resultCg = resultGo.AddComponent<CanvasGroup>();
        var resultRect = resultGo.GetComponent<RectTransform>();
        resultRect.anchorMin = Vector2.zero;
        resultRect.anchorMax = Vector2.one;
        resultRect.offsetMin = Vector2.zero;
        resultRect.offsetMax = Vector2.zero;

        var resultBg = resultGo.AddComponent<Image>();
        resultBg.color = new Color(0.04f, 0.03f, 0.08f, 0.92f);

        var resultTMP = MakeText(resultGo.transform, "ResultText", "VICTORY!",
            90f, new Color(1f, 0.84f, 0f), new Vector2(0f, 80f), new Vector2(800f, 120f));
        resultTMP.alignment = TextAlignmentOptions.Center;

        var flavorTMP = MakeText(resultGo.transform, "FlavorText", "The Hero triumphs!",
            28f, new Color(0.8f, 0.8f, 0.9f), new Vector2(0f, -10f), new Vector2(700f, 100f));
        flavorTMP.alignment = TextAlignmentOptions.Center;

        var continueBtn = MakeButton(resultGo.transform, "ContinueBtn", "Collect Rewards",
            new Color(0.5f, 0.35f, 0.02f), Color.white,
            new Vector2(0f, -110f), new Vector2(240f, 55f));
        var retryBtn = MakeButton(resultGo.transform, "RetryBtn", "Try Again",
            new Color(0.2f, 0.4f, 0.2f), Color.white,
            new Vector2(-120f, -110f), new Vector2(200f, 55f));
        var retreatBtn = MakeButton(resultGo.transform, "RetreatBtn", "Retreat to Map",
            new Color(0.35f, 0.15f, 0.15f), Color.white,
            new Vector2(120f, -110f), new Vector2(200f, 55f));

        var rs = resultGo.AddComponent<ResultScreenUI>();
        rs.CanvasGroup    = resultCg;
        rs.ResultText     = resultTMP;
        rs.FlavorText     = flavorTMP;
        rs.ContinueButton = continueBtn;
        rs.RetryButton    = retryBtn;
        rs.AbandonButton  = retreatBtn;
        resultGo.SetActive(false);

        // ── HUD Root ───────────────────────────────────────────────────
        var hud = canvasGo.AddComponent<CombatHUD>();
        hud.PlayerPanels = new[] { heroPanel.GetComponent<UnitStatusPanel>() };
        hud.EnemyPanels  = new[] { enemyPanel.GetComponent<UnitStatusPanel>() };
        hud.LogText      = logText;
        hud.BannerRect   = bannerGo.GetComponent<RectTransform>();
        hud.BannerText   = bannerTMP;
        hud.ResultScreen = rs;

        return (canvasGo, heroPanel.GetComponent<UnitStatusPanel>(),
                enemyPanel.GetComponent<UnitStatusPanel>(), rs);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  STATUS PANEL HELPER
    // ────────────────────────────────────────────────────────────────────────────
    static GameObject CreateStatusPanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Color bgColor)
    {
        GameObject panel = MakePanel(parent, name, bgColor, anchorMin, anchorMax, anchoredPos, size);
        var sp    = panel.AddComponent<UnitStatusPanel>();

        var nameText  = MakeText(panel.transform, "NameText",  "Unit Name",  22f, Color.white,        new Vector2(0f, 42f),  new Vector2(280f, 30f));
        var classText = MakeText(panel.transform, "ClassText", "Class",      14f, new Color(0.7f,0.7f,0.9f), new Vector2(0f, 16f), new Vector2(280f, 22f));

        // HP bar — plain Image with fillAmount (no Slider, avoids world-space distortion)
        var hpFill = MakeFillBar(panel.transform, "HPBar", new Color(0.2f,0.85f,0.3f),
            new Vector2(0f, -12f), new Vector2(260f, 14f));
        var hpValText = MakeText(panel.transform, "HPValue", "100/100", 12f, Color.white,
            new Vector2(0f, -28f), new Vector2(260f, 18f));

        // MP bar
        var mpFill = MakeFillBar(panel.transform, "MPBar", new Color(0.25f, 0.55f, 1f),
            new Vector2(0f, -46f), new Vector2(260f, 10f));
        var mpValText = MakeText(panel.transform, "MPValue", "60/60", 11f, new Color(0.5f,0.65f,1f),
            new Vector2(0f, -60f), new Vector2(260f, 16f));

        sp.NameText    = nameText;
        sp.ClassText   = classText;
        sp.HPBarFill   = hpFill;
        sp.HPValueText = hpValText;
        sp.MPBarFill   = mpFill;
        sp.MPValueText = mpValText;

        return panel;
    }

    /// <summary>Creates a BG + Fill image bar (Image.fillAmount-driven, no Slider).</summary>
    static Image MakeFillBar(Transform parent, string name, Color fillColor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos; rect.sizeDelta = size;

        // Background
        var bg = new GameObject("BG"); bg.transform.SetParent(go.transform, false);
        var bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(0.1f, 0.1f, 0.12f, 0.9f);
        var bgR = bg.GetComponent<RectTransform>();
        bgR.anchorMin = Vector2.zero; bgR.anchorMax = Vector2.one;
        bgR.offsetMin = Vector2.zero; bgR.offsetMax = Vector2.zero;

        // Fill area
        var fa = new GameObject("FillArea"); fa.transform.SetParent(go.transform, false);
        var faR = fa.AddComponent<RectTransform>();
        faR.anchorMin = Vector2.zero; faR.anchorMax = Vector2.one;
        faR.offsetMin = new Vector2(1f, 1f); faR.offsetMax = new Vector2(-1f, -1f);

        // Fill image (horizontal fill)
        var fill = new GameObject("Fill"); fill.transform.SetParent(fa.transform, false);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = fillColor;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 1f;
        var fillR = fill.GetComponent<RectTransform>();
        fillR.anchorMin = Vector2.zero; fillR.anchorMax = Vector2.one;
        fillR.offsetMin = Vector2.zero; fillR.offsetMax = Vector2.zero;

        return fillImg;
    }

    static Slider MakeSimpleSlider(Transform parent, string name, Color fillColor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var slider = go.AddComponent<Slider>();
        slider.value = 1f;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos; rect.sizeDelta = size;

        var bg = new GameObject("BG"); bg.transform.SetParent(go.transform, false);
        var bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(0.1f,0.1f,0.12f,0.9f);
        var bgR = bg.GetComponent<RectTransform>();
        bgR.anchorMin=Vector2.zero; bgR.anchorMax=Vector2.one;
        bgR.offsetMin=Vector2.zero; bgR.offsetMax=Vector2.zero;
        slider.targetGraphic = bgImg;

        var fa = new GameObject("FillArea"); fa.transform.SetParent(go.transform, false);
        var faR = fa.AddComponent<RectTransform>();
        faR.anchorMin=Vector2.zero; faR.anchorMax=Vector2.one;
        faR.offsetMin=new Vector2(1f,1f); faR.offsetMax=new Vector2(-1f,-1f);

        var fill = new GameObject("Fill"); fill.transform.SetParent(fa.transform, false);
        var fillImg = fill.AddComponent<Image>(); fillImg.color = fillColor;
        var fillR = fill.GetComponent<RectTransform>();
        fillR.anchorMin=Vector2.zero; fillR.anchorMax=Vector2.one;
        fillR.offsetMin=Vector2.zero; fillR.offsetMax=Vector2.zero;
        slider.fillRect = fillR;

        return slider;
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  COMBAT LIGHTING
    // ────────────────────────────────────────────────────────────────────────────
    static void SetupCombatLighting()
    {
        // Key light
        var key = new GameObject("KeyLight");
        var keyL = key.AddComponent<Light>();
        keyL.type = LightType.Directional;
        keyL.color = new Color(0.95f, 0.88f, 0.78f);
        keyL.intensity = 1.2f;
        key.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

        // Fill light (cool)
        var fill = new GameObject("FillLight");
        var fillL = fill.AddComponent<Light>();
        fillL.type = LightType.Directional;
        fillL.color = new Color(0.55f, 0.65f, 0.85f);
        fillL.intensity = 0.4f;
        fill.transform.rotation = Quaternion.Euler(20f, 145f, 0f);

        // Rim light
        var rim = new GameObject("RimLight");
        var rimL = rim.AddComponent<Light>();
        rimL.type = LightType.Directional;
        rimL.color = new Color(0.9f, 0.5f, 0.8f);
        rimL.intensity = 0.25f;
        rim.transform.rotation = Quaternion.Euler(5f, 90f, 0f);

        // Ambient
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.15f, 0.18f, 0.30f);
        RenderSettings.ambientEquatorColor = new Color(0.10f, 0.10f, 0.15f);
        RenderSettings.ambientGroundColor  = new Color(0.05f, 0.04f, 0.08f);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  UI HELPERS
    // ────────────────────────────────────────────────────────────────────────────
    static GameObject MakePanel(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img  = go.AddComponent<Image>();
        img.color = color;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos; rect.sizeDelta = size;
        return go;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text,
        float size, Color color, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos; rect.sizeDelta = sizeDelta;
        return tmp;
    }

    static Button MakeButton(Transform parent, string name, string label,
        Color bgColor, Color textColor, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img    = go.AddComponent<Image>();
        img.color  = bgColor;
        var btn    = go.AddComponent<Button>();

        var colors = btn.colors;
        colors.highlightedColor = bgColor * 1.3f;
        colors.pressedColor     = bgColor * 0.7f;
        btn.colors = colors;
        btn.targetGraphic = img;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos; rect.sizeDelta = size;

        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var tmp    = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20f;
        tmp.color     = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        var txtRect = txtGo.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;

        return btn;
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  UTILITIES
    // ────────────────────────────────────────────────────────────────────────────
    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    static void AddSceneToBuild(string path)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
            if (s.path == path) return;

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
        {
            new EditorBuildSettingsScene(path, true)
        };
        EditorBuildSettings.scenes = list.ToArray();
    }
}
