using System;

namespace StrikerFive.Core
{
    public enum Role { Goalkeeper, DefenderLeft, DefenderRight, Midfielder, Forward }

    /// <summary>1-2-1-1 formation that slides with the ball. Pure C#, unit tested.</summary>
    public static class Formation
    {
        public static readonly Role[] Roles = { Role.Goalkeeper, Role.DefenderLeft, Role.DefenderRight, Role.Midfielder, Role.Forward };

        /// <summary>Base position for a team attacking towards +x (mirrored by `side`).</summary>
        public static (float x, float z) Base(Role role)
        {
            switch (role)
            {
                case Role.Goalkeeper: return (-30f, 0f);
                case Role.DefenderLeft: return (-16f, 9f);
                case Role.DefenderRight: return (-16f, -9f);
                case Role.Midfielder: return (-4f, 0f);
                default: return (8f, 0f);
            }
        }

        /// <summary>Where a player should stand given the ball position. Side = +1 attacks +x, -1 attacks -x.</summary>
        public static (float x, float z) Home(Role role, int side, float ballX, float ballZ, bool inPossession)
        {
            var (bx, bz) = Base(role);
            float ballXRel = ballX * side;   // ball position in the team's own frame
            float shift = Clamp(ballXRel, -32f, 32f) * (role == Role.Goalkeeper ? 0.06f : 0.45f);
            float push = inPossession ? 4f : -3f;
            if (role == Role.Goalkeeper) push = 0f;
            float x = bx + shift + push;
            float z = bz + ballZ * (role == Role.Goalkeeper ? 0.15f : 0.35f);
            x = Clamp(x, role == Role.Goalkeeper ? -31f : -29f, role == Role.Goalkeeper ? -20f : 29f);
            z = Clamp(z, -19f, 19f);
            return (x * side, z);
        }

        private static float Clamp(float v, float a, float b) => Math.Max(a, Math.Min(b, v));
    }
}
