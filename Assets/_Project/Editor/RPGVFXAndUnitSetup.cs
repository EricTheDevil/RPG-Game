using UnityEngine;
using UnityEditor;
using System.IO;
using RPG.Units;
using RPG.Data;
using RPG.VFX;

/// <summary>
/// Creates:
///   1. Missing VFX particle prefabs (Death, CritBurst, Spawn, HitRing)
///   2. Unit glow Point Lights on Hero and Enemy prefabs
///   3. Trait strings on HeroStats and DarkKnightStats
///   4. Wires new VFX prefabs into CombatVFXManager in the open scene
///
/// Run via  RPG > Setup → VFX + Units
/// </summary>
public class RPGVFXAndUnitSetup
{
    [MenuItem("RPG/Setup → VFX + Units")]
    public static void Execute()
    {
        EnsureFolders();
        CreateVFXPrefabs();
        AddGlowToUnitPrefabs();
        SetTraitsOnStats();
        WireVFXIntoScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RPGVFXAndUnitSetup] ✅ VFX prefabs, unit glows, and traits all set up!");
    }

    // ── Folders ───────────────────────────────────────────────────────────────
    static void EnsureFolders()
    {
        foreach (var f in new[] { "Assets/_Project/Prefabs/VFX" })
            if (!Directory.Exists(f)) Directory.CreateDirectory(f);
    }

    // ── VFX Prefabs ───────────────────────────────────────────────────────────
    static void CreateVFXPrefabs()
    {
        CreateDeathVFX();
        CreateCritBurstVFX();
        CreateSpawnVFX();
        CreateHitRingVFX();
    }

    static void CreateDeathVFX()
    {
        const string path = "Assets/_Project/Prefabs/VFX/VFX_Death.prefab";
        if (File.Exists(path)) return;

        var go = new GameObject("VFX_Death");
        var ps = go.AddComponent<ParticleSystem>();

        // Main: large, dramatic, long duration
        var main = ps.main;
        main.duration        = 1.0f;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.15f, 0.5f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.4f, 0.1f), new Color(1f, 0.85f, 0.1f));
        main.gravityModifier = 0.3f;
        main.maxParticles    = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Emission: burst
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
        emission.rateOverTime = 0;

        // Shape: sphere
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.4f;

        // Velocity over lifetime: explode outward
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.radial  = new ParticleSystem.MinMaxCurve(2f);

        // Color over lifetime: fade out
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(1f,0.6f,0.1f), 0f),
                    new GradientColorKey(new Color(0.4f,0.1f,0f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Size over lifetime: shrink
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        var sizeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Renderer
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode     = ParticleSystemRenderMode.Billboard;
        rend.sortingOrder   = 1;

        SavePrefab(go, path);
        Debug.Log("[RPGVFXAndUnitSetup] Created VFX_Death.");
    }

    static void CreateCritBurstVFX()
    {
        const string path = "Assets/_Project/Prefabs/VFX/VFX_CritBurst.prefab";
        if (File.Exists(path)) return;

        var go = new GameObject("VFX_CritBurst");
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration        = 0.3f;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(4f, 9f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.85f, 0.1f), new Color(1f, 0.4f, 0.05f));
        main.maxParticles    = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 35) });
        emission.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.05f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.radial  = new ParticleSystem.MinMaxCurve(3f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(1f,0.9f,0.3f), 0f),
                    new GradientColorKey(new Color(1f,0.3f,0f), 0.5f),
                    new GradientColorKey(Color.black, 1f) },
            new[] { new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        SavePrefab(go, path);
        Debug.Log("[RPGVFXAndUnitSetup] Created VFX_CritBurst.");
    }

    static void CreateSpawnVFX()
    {
        const string path = "Assets/_Project/Prefabs/VFX/VFX_Spawn.prefab";
        if (File.Exists(path)) return;

        var go = new GameObject("VFX_Spawn");
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration        = 0.5f;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(0.5f, 0.9f, 1f), new Color(0.8f, 0.5f, 1f));
        main.gravityModifier = -0.15f;  // float upward
        main.maxParticles    = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 45) });
        emission.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.3f;
        shape.arc       = 360f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(0.7f, 1f, 1f), 0f),
                    new GradientColorKey(Color.white, 0.4f),
                    new GradientColorKey(new Color(0.6f,0.4f,1f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.6f, 0.5f),
                    new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        SavePrefab(go, path);
        Debug.Log("[RPGVFXAndUnitSetup] Created VFX_Spawn.");
    }

    static void CreateHitRingVFX()
    {
        const string path = "Assets/_Project/Prefabs/VFX/VFX_HitRing.prefab";
        if (File.Exists(path)) return;

        var go = new GameObject("VFX_HitRing");
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration        = 0.25f;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 0.8f), new Color(0.8f, 0.8f, 1f));
        main.maxParticles    = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });
        emission.rateOverTime = 0;

        // Ring shape: emit from circle edge, outward
        var shape = ps.shape;
        shape.shapeType   = ParticleSystemShapeType.Circle;
        shape.radius      = 0.15f;
        shape.arc         = 360f;
        shape.arcMode     = ParticleSystemShapeMultiModeValue.BurstSpread;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.radial  = new ParticleSystem.MinMaxCurve(2.5f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.9f, 0.9f, 1f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        SavePrefab(go, path);
        Debug.Log("[RPGVFXAndUnitSetup] Created VFX_HitRing.");
    }

    static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    // ── Unit Glow Lights ─────────────────────────────────────────────────────
    static void AddGlowToUnitPrefabs()
    {
        AddGlowToPrefab("Assets/_Project/Prefabs/HeroPrefab.prefab",
            new Color(0.3f, 0.6f, 1f),   // blue tint
            intensity: 1.2f, range: 2.5f);

        AddGlowToPrefab("Assets/_Project/Prefabs/EnemyPrefab.prefab",
            new Color(1f, 0.3f, 0.2f),   // red tint
            intensity: 1.0f, range: 2.2f);
    }

    static void AddGlowToPrefab(string prefabPath, Color color, float intensity, float range)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogWarning($"[RPGVFXAndUnitSetup] Prefab not found: {prefabPath}"); return; }

        using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
        var root = scope.prefabContentsRoot;

        // Don't add twice
        var existing = root.GetComponentInChildren<Light>();
        if (existing != null && existing.gameObject.name == "UnitGlow")
        {
            Debug.Log($"[RPGVFXAndUnitSetup] UnitGlow already on {prefabPath}");
            return;
        }

        // Create glow child
        var glowGo = new GameObject("UnitGlow");
        glowGo.transform.SetParent(root.transform, false);
        glowGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        var light = glowGo.AddComponent<Light>();
        light.type      = LightType.Point;
        light.color     = color;
        light.intensity = intensity;
        light.range     = range;
        light.shadows   = LightShadows.None;

        // Wire UnitGlow field on the Unit component
        var unit = root.GetComponent<Unit>();
        if (unit != null)
        {
            unit.UnitGlow = light;
            EditorUtility.SetDirty(root);
        }

        Debug.Log($"[RPGVFXAndUnitSetup] Added UnitGlow to {prefabPath}");
    }

    // ── Traits on Stats SOs ───────────────────────────────────────────────────
    static void SetTraitsOnStats()
    {
        SetTraits("Assets/_Project/ScriptableObjects/Stats/HeroStats.asset",
                  new[] { "Hero", "Warrior" });

        SetTraits("Assets/_Project/ScriptableObjects/Stats/DarkKnightStats.asset",
                  new[] { "Knight", "Warrior" });
    }

    static void SetTraits(string assetPath, string[] traits)
    {
        var so = AssetDatabase.LoadAssetAtPath<UnitStatsSO>(assetPath);
        if (so == null) { Debug.LogWarning($"[RPGVFXAndUnitSetup] Stats SO not found: {assetPath}"); return; }

        so.Traits.Clear();
        so.Traits.AddRange(traits);
        EditorUtility.SetDirty(so);
        Debug.Log($"[RPGVFXAndUnitSetup] Set traits on {so.name}: [{string.Join(", ", traits)}]");
    }

    // ── Wire new VFX prefabs into CombatVFXManager in scene ─────────────────
    static void WireVFXIntoScene()
    {
        var vfxGo = GameObject.Find("CombatVFXManager");
        if (vfxGo == null)
        {
            Debug.Log("[RPGVFXAndUnitSetup] CombatVFXManager not in open scene — skipping scene wire.");
            return;
        }

        var vfx = vfxGo.GetComponent<CombatVFXManager>();
        if (vfx == null) return;

        vfx.DeathVFXPrefab  = Load<ParticleSystem>("Assets/_Project/Prefabs/VFX/VFX_Death.prefab");
        vfx.CritBurstPrefab = Load<ParticleSystem>("Assets/_Project/Prefabs/VFX/VFX_CritBurst.prefab");
        vfx.SpawnVFXPrefab  = Load<ParticleSystem>("Assets/_Project/Prefabs/VFX/VFX_Spawn.prefab");
        vfx.HitRingPrefab   = Load<ParticleSystem>("Assets/_Project/Prefabs/VFX/VFX_HitRing.prefab");

        // Also wire FloatingText prefab and existing VFX if missing
        if (vfx.FloatingTextPrefab == null)
            vfx.FloatingTextPrefab = Load<FloatingText>("Assets/_Project/Prefabs/UI/FloatingText.prefab");

        if (vfx.AttackVFXPrefab  == null) vfx.AttackVFXPrefab  = Load<ParticleSystem>("Assets/_Project/Prefabs/VFX/VFX_Attack.prefab");
        if (vfx.MagicVFXPrefab   == null) vfx.MagicVFXPrefab   = Load<ParticleSystem>("Assets/_Project/Prefabs/VFX/VFX_Magic.prefab");
        if (vfx.SpecialVFXPrefab == null) vfx.SpecialVFXPrefab = Load<ParticleSystem>("Assets/_Project/Prefabs/VFX/VFX_Special.prefab");
        if (vfx.HealVFXPrefab    == null) vfx.HealVFXPrefab    = Load<ParticleSystem>("Assets/_Project/Prefabs/VFX/VFX_Heal.prefab");
        if (vfx.DefendVFXPrefab  == null) vfx.DefendVFXPrefab  = Load<ParticleSystem>("Assets/_Project/Prefabs/VFX/VFX_Defend.prefab");

        EditorUtility.SetDirty(vfx);

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        Debug.Log("[RPGVFXAndUnitSetup] All VFX prefabs wired into CombatVFXManager.");
    }

    static T Load<T>(string path) where T : Component
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return go != null ? go.GetComponent<T>() : null;
    }
}
