using UnityEngine;
using RPG.Core;

/// <summary>
/// Ensures GameManager exists in every scene.
/// Place on a root GameObject in each scene.
/// </summary>
public class SceneBootstrap : MonoBehaviour
{
    [SerializeField] private GameManager GameManagerPrefab;

    private void Awake()
    {
        if (GameManager.Instance == null && GameManagerPrefab != null)
            Instantiate(GameManagerPrefab);
    }
}
