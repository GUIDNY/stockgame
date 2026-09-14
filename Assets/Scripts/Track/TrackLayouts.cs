namespace TurboLoop.Track
{
    public class TrackLayout
    {
        public string Name;
        public string Description;
        public Vec2[] ControlPoints;
        public float RoadWidth = 14f;
        /// <summary>Colour theme key used by the scenery builder: "coast", "mountain", "city".</summary>
        public string Theme = "coast";
    }

    /// <summary>Hand-authored closed circuits. Control points are (x, z) in metres; the start line is at the first point, heading +x.</summary>
    public static class TrackLayouts
    {
        public static readonly TrackLayout[] All =
        {
            new TrackLayout
            {
                Name = "Seabreeze Loop", Description = "Fast flowing curves along the coast. Good for learning the car.",
                Theme = "coast", RoadWidth = 15f,
                ControlPoints = new[]
                {
                    new Vec2(0, 0), new Vec2(70, -4), new Vec2(130, 20), new Vec2(160, 70), new Vec2(140, 125),
                    new Vec2(90, 150), new Vec2(40, 130), new Vec2(0, 150), new Vec2(-60, 160), new Vec2(-120, 130),
                    new Vec2(-150, 70), new Vec2(-130, 15), new Vec2(-70, -6)
                }
            },
            new TrackLayout
            {
                Name = "Ridge Hairpin", Description = "Long straight into a brutal hairpin, then a tight technical section.",
                Theme = "mountain", RoadWidth = 13f,
                ControlPoints = new[]
                {
                    new Vec2(0, 0), new Vec2(90, 0), new Vec2(170, 6), new Vec2(215, 40), new Vec2(200, 85),
                    new Vec2(150, 80), new Vec2(120, 120), new Vec2(140, 170), new Vec2(90, 200), new Vec2(30, 175),
                    new Vec2(0, 120), new Vec2(-60, 140), new Vec2(-120, 110), new Vec2(-130, 50), new Vec2(-80, 10)
                }
            },
            new TrackLayout
            {
                Name = "Neon District", Description = "Street circuit with 90-degree corners and short bursts of speed.",
                Theme = "city", RoadWidth = 14f,
                ControlPoints = new[]
                {
                    new Vec2(0, 0), new Vec2(80, 0), new Vec2(130, 10), new Vec2(140, 60), new Vec2(100, 80),
                    new Vec2(60, 60), new Vec2(20, 90), new Vec2(30, 150), new Vec2(90, 165), new Vec2(150, 140),
                    new Vec2(190, 180), new Vec2(150, 230), new Vec2(60, 240), new Vec2(-40, 220), new Vec2(-90, 150),
                    new Vec2(-110, 60), new Vec2(-80, 5)
                }
            }
        };
    }
}
