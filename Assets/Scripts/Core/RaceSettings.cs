using UnityEngine;

namespace TurboLoop.Core
{
    /// <summary>Menu choices that survive between races.</summary>
    public static class RaceSettings
    {
        public static int TrackIndex = 0;
        public static int Laps = 3;
        public static int Opponents = 5;
        public static int PlayerColorIndex = 0;

        public static readonly Color[] Palette =
        {
            new Color(0.9f, 0.12f, 0.12f), new Color(0.15f, 0.4f, 0.95f), new Color(0.98f, 0.8f, 0.1f),
            new Color(0.15f, 0.7f, 0.35f), new Color(0.95f, 0.45f, 0.1f), new Color(0.6f, 0.2f, 0.85f),
            new Color(0.95f, 0.95f, 0.95f), new Color(0.12f, 0.12f, 0.14f)
        };
        public static readonly string[] PaletteNames = { "Red", "Blue", "Yellow", "Green", "Orange", "Purple", "White", "Black" };
        public static readonly string[] AiNames = { "Kestrel", "Riva", "Bolt", "Orion", "Mako", "Zephyr", "Nova", "Sable" };
    }
}
