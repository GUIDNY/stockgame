using UnityEngine;

namespace Echobound.Core
{
    /// <summary>
    /// Makes any scene playable: if no GameManager exists after the scene loads, one is created.
    /// Open an empty scene in Unity and press Play; the whole game is assembled at runtime.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindObjectOfType<GameManager>() != null) return;
            var root = new GameObject("Echobound");
            root.AddComponent<GameManager>();
        }
    }
}
