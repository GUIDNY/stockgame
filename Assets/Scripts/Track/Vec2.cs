using System;

namespace TurboLoop.Track
{
    /// <summary>Planar vector (x, z) for track math. Unity-independent so the track core can be unit tested.</summary>
    [Serializable]
    public struct Vec2
    {
        public float X, Z;
        public Vec2(float x, float z) { X = x; Z = z; }
        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Z + b.Z);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Z - b.Z);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Z * s);
        public static Vec2 operator *(float s, Vec2 a) => new Vec2(a.X * s, a.Z * s);
        public float Length => (float)Math.Sqrt(X * X + Z * Z);
        public float SqrLength => X * X + Z * Z;
        public Vec2 Normalized { get { float l = Length; return l > 1e-6f ? new Vec2(X / l, Z / l) : new Vec2(1, 0); } }
        /// <summary>Normal pointing to the left of the direction of travel (y-up, right-handed like Unity).</summary>
        public Vec2 Left => new Vec2(-Z, X);
        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Z * b.Z;
        /// <summary>2D cross product (a.x*b.z - a.z*b.x). Positive when b is to the left of a.</summary>
        public static float Cross(Vec2 a, Vec2 b) => a.X * b.Z - a.Z * b.X;
        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;
        public static Vec2 Lerp(Vec2 a, Vec2 b, float t) => a + (b - a) * t;
        public override string ToString() => $"({X:0.0}, {Z:0.0})";
    }
}
