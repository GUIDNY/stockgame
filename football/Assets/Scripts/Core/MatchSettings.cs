using UnityEngine;

namespace StrikerFive.Core
{
    public class TeamDef
    {
        public string Name;
        public string Short;
        public Color Shirt;
        public Color Shorts;
        public Color Accent;
    }

    public static class MatchSettings
    {
        public static int HomeTeam = 0;
        public static int AwayTeam = 1;
        public static float HalfMinutes = 3f;
        /// <summary>0 easy, 1 normal, 2 hard: scales AI speed and reaction.</summary>
        public static int Difficulty = 1;

        public static readonly TeamDef[] Teams =
        {
            new TeamDef { Name = "Harbor City", Short = "HAR", Shirt = new Color(0.85f, 0.1f, 0.12f), Shorts = new Color(0.95f, 0.95f, 0.95f), Accent = Color.white },
            new TeamDef { Name = "Northgate United", Short = "NGU", Shirt = new Color(0.1f, 0.3f, 0.85f), Shorts = new Color(0.08f, 0.1f, 0.2f), Accent = new Color(1f, 0.85f, 0.2f) },
            new TeamDef { Name = "Sunfield Rovers", Short = "SUN", Shirt = new Color(0.98f, 0.8f, 0.1f), Shorts = new Color(0.1f, 0.1f, 0.1f), Accent = Color.black },
            new TeamDef { Name = "Verdant Athletic", Short = "VER", Shirt = new Color(0.1f, 0.6f, 0.3f), Shorts = new Color(0.95f, 0.95f, 0.95f), Accent = Color.white },
            new TeamDef { Name = "Ironbridge FC", Short = "IRO", Shirt = new Color(0.25f, 0.25f, 0.3f), Shorts = new Color(0.9f, 0.5f, 0.1f), Accent = new Color(0.9f, 0.5f, 0.1f) },
            new TeamDef { Name = "Coral Bay", Short = "COR", Shirt = new Color(0.15f, 0.75f, 0.8f), Shorts = new Color(0.95f, 0.95f, 0.95f), Accent = new Color(0.95f, 0.3f, 0.5f) }
        };

        public static readonly string[] Surnames = { "Adler", "Brant", "Cole", "Dario", "Ekene", "Faro", "Grey", "Holt", "Ilic", "Joss", "Kade", "Lund", "Moss", "Nix", "Orme", "Pell", "Quist", "Rook", "Sato", "Tamm", "Uzo", "Vane", "Wick", "Yael", "Zed" };

        public static float AiSpeed => Difficulty == 0 ? 0.88f : Difficulty == 1 ? 0.97f : 1.04f;
        public static float AiReaction => Difficulty == 0 ? 0.6f : Difficulty == 1 ? 0.4f : 0.25f;
    }
}
