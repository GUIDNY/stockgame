namespace StrikerFive.Core
{
    public enum OutKind { None, Sideline, GoalLine }

    /// <summary>
    /// Pitch dimensions and rules geometry. Sized per match format (1v1 plays on a small pitch with small goals).
    /// Pure C#, unit tested. X runs goal to goal, Z across the pitch.
    /// </summary>
    public static class PitchGeometry
    {
        public static float HalfLength = 32f;   // goal lines at x = ±HalfLength
        public static float HalfWidth = 21f;    // touchlines at z = ±HalfWidth
        public static float GoalHalfWidth = 3.2f;
        public static float GoalHeight = 2.3f;
        public static float GoalDepth = 2.2f;
        public static float BoxLength = 10f;    // penalty area depth from the goal line
        public static float BoxHalfWidth = 11f;
        public const float BallRadius = 0.32f;

        /// <summary>Relative size compared with the full five-a-side pitch; used to scale formations and camera.</summary>
        public static float Scale => HalfLength / 32f;

        public static void Configure(int playersPerSide)
        {
            switch (playersPerSide)
            {
                case 1: HalfLength = 20f; HalfWidth = 13f; GoalHalfWidth = 2.4f; GoalHeight = 2.0f; BoxLength = 6f; BoxHalfWidth = 7f; break;
                case 2: HalfLength = 25f; HalfWidth = 16f; GoalHalfWidth = 2.8f; GoalHeight = 2.2f; BoxLength = 8f; BoxHalfWidth = 9f; break;
                default: HalfLength = 32f; HalfWidth = 21f; GoalHalfWidth = 3.2f; GoalHeight = 2.3f; BoxLength = 10f; BoxHalfWidth = 11f; break;
            }
        }

        public static (float x, float z) AttackGoal(int side) => (HalfLength * side, 0f);
        public static (float x, float z) OwnGoal(int side) => (-HalfLength * side, 0f);

        /// <summary>True when the ball is fully over the line inside the goal at x = HalfLength*goalSide.</summary>
        public static bool IsGoal(float x, float y, float z, int goalSide)
        {
            bool overLine = goalSide > 0 ? x - BallRadius > HalfLength : x + BallRadius < -HalfLength;
            return overLine && System.Math.Abs(z) < GoalHalfWidth - 0.05f && y < GoalHeight - 0.1f;
        }

        public static OutKind Out(float x, float z)
        {
            if (System.Math.Abs(z) - BallRadius > HalfWidth) return OutKind.Sideline;
            if (System.Math.Abs(x) - BallRadius > HalfLength) return OutKind.GoalLine;
            return OutKind.None;
        }

        public static bool InBox(float x, float z, int goalSide)
        {
            bool depth = goalSide > 0 ? x > HalfLength - BoxLength : x < -HalfLength + BoxLength;
            return depth && System.Math.Abs(z) < BoxHalfWidth;
        }

        public static float ClampX(float x, float margin = 0.5f) => System.Math.Max(-HalfLength + margin, System.Math.Min(HalfLength - margin, x));
        public static float ClampZ(float z, float margin = 0.5f) => System.Math.Max(-HalfWidth + margin, System.Math.Min(HalfWidth - margin, z));
    }
}
