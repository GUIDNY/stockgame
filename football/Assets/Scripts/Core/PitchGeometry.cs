namespace StrikerFive.Core
{
    public enum OutKind { None, Sideline, GoalLine }

    /// <summary>Pitch dimensions and rules geometry. Pure C#, unit tested. X runs goal to goal, Z across the pitch.</summary>
    public static class PitchGeometry
    {
        public const float HalfLength = 32f;   // goal lines at x = ±32
        public const float HalfWidth = 21f;    // touchlines at z = ±21
        public const float GoalHalfWidth = 3.2f;
        public const float GoalHeight = 2.3f;
        public const float GoalDepth = 2.2f;
        public const float BoxLength = 10f;    // penalty area depth from the goal line
        public const float BoxHalfWidth = 11f;
        public const float BallRadius = 0.22f;

        /// <summary>Goal centre for the goal that team `side` attacks (side = +1 attacks +x).</summary>
        public static (float x, float z) AttackGoal(int side) => (HalfLength * side, 0f);
        public static (float x, float z) OwnGoal(int side) => (-HalfLength * side, 0f);

        /// <summary>True when the ball is fully over the line inside the goal at x = 32*goalSide.</summary>
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
