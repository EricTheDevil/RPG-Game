using UnityEngine;
using UnityEditor;

public class AddCheshireMeshToHero
{
    public static void Execute()
    {
        var prefabPath = "Assets/_Project/Prefabs/HeroPrefab.prefab";
        var fbxPath = "Assets/_Project/Models/Meshy_AI_Cheshire_0415064128_texture_fbx/Meshy_AI_Cheshire_0415064128_texture.fbx";

        var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null) { Debug.LogError("FBX not found: " + fbxPath); return; }

        // Load and edit prefab
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null) { Debug.LogError("Prefab not found: " + prefabPath); return; }

        // Remove existing CheshireMesh if present
        var existing = prefabRoot.transform.Find("CheshireMesh");
        if (existing != null) GameObject.DestroyImmediate(existing.gameObject);

        // Instantiate FBX as child
        var meshInst = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset, prefabRoot.transform);
        meshInst.name = "CheshireMesh";
        meshInst.transform.localPosition = Vector3.zero;
        meshInst.transform.localRotation = Quaternion.identity;
        meshInst.transform.localScale = Vector3.one;

        // Save back
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log("CheshireMesh added to HeroPrefab successfully.");
    }
}
