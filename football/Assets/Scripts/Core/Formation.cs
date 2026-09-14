using System;

namespace StrikerFive.Core
{
    public enum Role { Goalkeeper, DefenderLeft, DefenderRight, Midfielder, Forward }

    /// <summary>Formation that slides with the ball and scales with the pitch. Pure C#, unit tested.</summary>
    public static class Formation
    {
        public static readonly Role[] Roles = { Role.Goalkeeper, Role.DefenderLeft, Role.DefenderRight, Role.Midfielder, Role.Forward };

        /// <summary>Roles used for a given squad size. 1v1 and 2v2 have no goalkeeper.</summary>
        public static Role[] RolesFor(int playersPerSide)
        {
            switch (playersPerSide)
            {
                case 1: return new[] { Role.Forward };
                case 2: return new[] { Role.DefenderLeft, Role.Forward };
                default: return Roles;
            }
        }

        /// <summary>Base position for a team attacking towards +x on the full-size pitch (mirrored by `side`).</summary>
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
            float s = PitchGeometry.Scale;
            var (bx, bz) = Base(role);
            bx *= s; bz *= s;
            float ballXRel = ballX * side;
            float shift = Clamp(ballXRel, -32f * s, 32f * s) * (role == Role.Goalkeeper ? 0.06f : 0.45f);
            float push = (inPossession ? 4f : -3f) * s;
            if (role == Role.Goalkeeper) push = 0f;
            float x = bx + shift + push;
            float z = bz + ballZ * (role == Role.Goalkeeper ? 0.15f : 0.35f);
            float edge = PitchGeometry.HalfLength;
            x = Clamp(x, role == Role.Goalkeeper ? -(edge - 1f) : -(edge - 3f), role == Role.Goalkeeper ? -20f * s : edge - 3f);
            z = Clamp(z, -(PitchGeometry.HalfWidth - 2f), PitchGeometry.HalfWidth - 2f);
            return (x * side, z);
        }

        private static float Clamp(float v, float a, float b) => Math.Max(a, Math.Min(b, v));
    }
}
