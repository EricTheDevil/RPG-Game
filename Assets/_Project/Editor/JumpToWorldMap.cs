#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using RPG.Core;
using RPG.Data;
using RPG.Map;

namespace RPG.Editor
{
    public static class JumpToWorldMap
    {
        [MenuItem("RPG/Preview/Jump To WorldMap")]
        public static void Jump()
        {
            var gm = GameManager.Instance;
            if (gm == null) { Debug.LogError("GameManager not found — start play mode first."); return; }

            var cls = AssetDatabase.LoadAssetAtPath<ClassDefinitionSO>(
                "Assets/_Project/ScriptableObjects/Classes/Class_Warrior.asset");
            if (cls == null) { Debug.LogError("Warrior class asset not found."); return; }

            GameSession.Instance?.ResetSession();
            gm.OnClassSelected(cls);
        }
    }
}
#endif
