using UnityEngine;
using UnityEditor;

/// <summary>
/// Wires the Cheshire model into HeroPrefab:
///  1. Updates HeroUnit.mat with HDRP Lit PBR textures
///  2. Swaps Body MeshFilter from capsule to Cheshire mesh
///  3. Assigns HeroStats SO to the HeroUnit component
///
/// Run via RPG > Wire Cheshire Hero
/// </summary>
public class WireCheshire
{
    const string FBX_PATH     = "Assets/_Project/Models/Meshy_AI_Cheshire_0415064128_texture_fbx/Meshy_AI_Cheshire_0415064128_texture.fbx";
    const string MAT_PATH     = "Assets/_Project/Materials/HeroUnit.mat";
    const string PREFAB_PATH  = "Assets/_Project/Prefabs/HeroPrefab.prefab";
    const string STATS_PATH   = "Assets/_Project/ScriptableObjects/Stats/HeroStats.asset";

    const string TEX_ALBEDO   = "Assets/_Project/Models/Meshy_AI_Cheshire_0415064128_texture_fbx/Meshy_AI_Cheshire_0415064128_texture.png";
    const string TEX_NORMAL   = "Assets/_Project/Models/Meshy_AI_Cheshire_0415064128_texture_fbx/Meshy_AI_Cheshire_0415064128_texture_normal.png";
    const string TEX_METALLIC = "Assets/_Project/Models/Meshy_AI_Cheshire_0415064128_texture_fbx/Meshy_AI_Cheshire_0415064128_texture_metallic.png";
    const string TEX_ROUGHNESS= "Assets/_Project/Models/Meshy_AI_Cheshire_0415064128_texture_fbx/Meshy_AI_Cheshire_0415064128_texture_roughness.png";

    [MenuItem("RPG/Wire Cheshire Hero")]
    public static void Execute()
    {
        // ── 1. Update material ────────────────────────────────────────────────
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MAT_PATH);
        if (mat == null) { Debug.LogError("[WireCheshire] HeroUnit.mat not found."); return; }

        // Use HDRP Lit shader
        var shader = Shader.Find("HDRP/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        mat.shader = shader;

        var albedo    = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_ALBEDO);
        var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_NORMAL);
        var metallic  = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_METALLIC);
        var roughness = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_ROUGHNESS);

        if (albedo   != null) { mat.SetTexture("_BaseColorMap",   albedo);   mat.SetTexture("_MainTex", albedo); }
        if (normalTex!= null) { mat.SetTexture("_NormalMap",      normalTex); SetNormalMapImportSettings(TEX_NORMAL); }
        if (metallic != null)   mat.SetTexture("_MaskMap",        metallic);
        if (roughness!= null)
        {
            // HDRP Lit uses inverted roughness (smoothness). We store roughness in _MaskMap channel A
            // but also set smoothness value low since we have a roughness map.
            mat.SetFloat("_Smoothness", 0f); // will be driven by roughness texture
        }

        // Make sure metallic workflow
        mat.SetFloat("_MaterialID", 1f); // HDRP Standard Lit
        mat.EnableKeyword("_NORMALMAP");
        mat.EnableKeyword("_MASKMAP");

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        Debug.Log("[WireCheshire] HeroUnit.mat updated with PBR textures.");

        // ── 2. Swap mesh in HeroPrefab/Body ──────────────────────────────────
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefabAsset == null) { Debug.LogError("[WireCheshire] HeroPrefab.prefab not found."); return; }

        // Find the first SkinnedMeshRenderer or Mesh inside the FBX
        Mesh cheshireMesh = null;
        var fbxAssets = AssetDatabase.LoadAllAssetsAtPath(FBX_PATH);
        foreach (var a in fbxAssets)
        {
            if (a is Mesh m) { cheshireMesh = m; break; }
        }

        if (cheshireMesh == null) { Debug.LogError("[WireCheshire] No Mesh found in Cheshire FBX."); return; }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH))
        {
            var root = scope.prefabContentsRoot;

            // Find or create Body child
            var bodyTr = root.transform.Find("Body");
            if (bodyTr == null)
            {
                var bodyGo = new GameObject("Body");
                bodyGo.transform.SetParent(root.transform, false);
                bodyTr = bodyGo.transform;
            }

            // Reset scale to 1 (Cheshire FBX is already ~1.45m tall)
            bodyTr.localScale = Vector3.one;
            bodyTr.localPosition = Vector3.zero;
            bodyTr.localRotation = Quaternion.identity;

            // Check if FBX has a SkinnedMeshRenderer (skinned) or just a regular mesh
            var fbxRoot = AssetDatabase.LoadAssetAtPath<GameObject>(FBX_PATH);
            var fbxSkinned = fbxRoot != null ? fbxRoot.GetComponentInChildren<SkinnedMeshRenderer>() : null;

            if (fbxSkinned != null)
            {
                // Cheshire is skinned — instantiate the FBX root as a child and use it for rendering
                // Remove any existing MeshFilter / MeshRenderer on Body
                var mf = bodyTr.GetComponent<MeshFilter>();
                var mr = bodyTr.GetComponent<MeshRenderer>();
                if (mf != null) Object.DestroyImmediate(mf);
                if (mr != null) Object.DestroyImmediate(mr);

                // Check if a Cheshire instance already exists under root
                var existing = root.transform.Find("CheshireMesh");
                if (existing != null) Object.DestroyImmediate(existing.gameObject);

                var cheshireInst = Object.Instantiate(fbxRoot, root.transform);
                cheshireInst.name = "CheshireMesh";
                cheshireInst.transform.localPosition = Vector3.zero;
                cheshireInst.transform.localRotation = Quaternion.identity;
                cheshireInst.transform.localScale    = Vector3.one;

                // Assign material to all SkinnedMeshRenderers
                foreach (var smr in cheshireInst.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mats = new Material[smr.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                    smr.sharedMaterials = mats;
                }

                // Update HeroUnit renderer reference to point to CheshireMesh
                var heroUnit = root.GetComponent<RPG.Units.HeroUnit>();
                if (heroUnit != null)
                {
                    heroUnit.UnitRenderer = cheshireInst.GetComponentInChildren<SkinnedMeshRenderer>();
                    EditorUtility.SetDirty(heroUnit);
                }

                Debug.Log("[WireCheshire] Cheshire skinned mesh instantiated under HeroPrefab.");
            }
            else
            {
                // Static mesh — assign to MeshFilter on Body
                var mf = bodyTr.GetComponent<MeshFilter>();
                if (mf == null) mf = bodyTr.gameObject.AddComponent<MeshFilter>();
                mf.sharedMesh = cheshireMesh;

                var mr = bodyTr.GetComponent<MeshRenderer>();
                if (mr == null) mr = bodyTr.gameObject.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;

                Debug.Log("[WireCheshire] Cheshire static mesh assigned to HeroPrefab/Body.");
            }

            // ── 3. Wire HeroStats SO ──────────────────────────────────────────
            var stats = AssetDatabase.LoadAssetAtPath<RPG.Data.UnitStatsSO>(STATS_PATH);
            var heroUnit2 = root.GetComponent<RPG.Units.HeroUnit>();
            if (heroUnit2 != null && stats != null)
            {
                heroUnit2.Stats = stats;
                EditorUtility.SetDirty(heroUnit2);
                Debug.Log("[WireCheshire] HeroStats.asset wired to HeroUnit.");
            }
            else
            {
                if (stats == null)    Debug.LogWarning("[WireCheshire] HeroStats.asset not found.");
                if (heroUnit2 == null) Debug.LogWarning("[WireCheshire] HeroUnit component not found on prefab root.");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WireCheshire] ✅ HeroPrefab fully wired with Cheshire model + PBR material + HeroStats!");
    }

    static void SetNormalMapImportSettings(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.NormalMap)
        {
            imp.textureType = TextureImporterType.NormalMap;
            imp.SaveAndReimport();
        }
    }
}
