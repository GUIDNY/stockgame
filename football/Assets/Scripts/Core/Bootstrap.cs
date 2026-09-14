using UnityEngine;

namespace StrikerFive.Core
{
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindObjectOfType<MatchManager>() != null) return;
            new GameObject("StrikerFive").AddComponent<MatchManager>();
        }
    }
}
