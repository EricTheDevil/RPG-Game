// Editor-only debug helper — inject session data so WorldMap renders without going through MainMenu.
// Remove this component from the scene before shipping.
using UnityEngine;
using RPG.Data;
using RPG.Map;

namespace RPG.Core
{
    public class WorldMapDebugBootstrap : MonoBehaviour
    {
        [Header("Config (assign in Inspector)")]
        public WorldMapConfigSO WorldMapConfig;

        private void Awake()
        {
            if (GameSession.Instance == null)
            {
                var go = new GameObject("GameSession");
                go.AddComponent<GameSession>();
            }

            var session = GameSession.Instance;
            if (session.WorldMap != null) return; // already set by real flow

            if (WorldMapConfig == null)
            {
                Debug.LogError("[WorldMapDebugBootstrap] WorldMapConfig not assigned.");
                return;
            }

            session.WorldMap          = WorldMapGenerator.Generate(WorldMapConfig);
            session.CurrentLayerIndex = 0;
            session.ApplyResources(new ResourceDelta { Gold = 50, Scrap = 10, Rations = 6, Morale = 7 });

            Debug.Log($"[WorldMapDebugBootstrap] Generated map: {session.WorldMap.Layers.Count} layers.");
        }
    }
}
