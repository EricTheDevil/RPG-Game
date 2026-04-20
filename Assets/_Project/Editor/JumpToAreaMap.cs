#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using RPG.Core;
using RPG.Data;
using RPG.Map;

namespace RPG.Editor
{
    public static class JumpToAreaMap
    {
        [MenuItem("RPG/Preview/Jump To AreaMap")]
        public static void Jump()
        {
            var gm = GameManager.Instance;
            if (gm == null) { Debug.LogError("GameManager not found."); return; }

            var session = GameSession.Instance;
            if (session?.WorldMap == null) { Debug.LogError("No world map — run Jump To WorldMap first."); return; }

            // Enter the first node of layer 1
            var layer1 = session.WorldMap.Layers.Count > 1 ? session.WorldMap.Layers[1] : null;
            if (layer1 == null || layer1.Nodes.Count == 0) { Debug.LogError("No layer 1 nodes."); return; }

            gm.EnterAreaNode(layer1.Nodes[0].Id);
        }
    }
}
#endif
