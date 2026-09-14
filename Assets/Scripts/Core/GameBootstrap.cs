using UnityEngine;

namespace TurboLoop.Core
{
    /// <summary>Any scene becomes the game: if no RaceManager exists after load, one is created and builds everything.</summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindObjectOfType<RaceManager>() != null) return;
            new GameObject("TurboLoop").AddComponent<RaceManager>();
        }
    }
}
