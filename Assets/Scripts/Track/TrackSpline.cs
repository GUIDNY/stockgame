using System;
using System.Collections.Generic;

namespace TurboLoop.Track
{
    /// <summary>
    /// Closed Catmull-Rom spline resampled at (near) uniform arc length. Provides centreline points, tangents,
    /// signed curvature and cumulative length: everything the mesh builder, the AI and the lap tracker need.
    /// </summary>
    public class TrackSpline
    {
        public readonly List<Vec2> Points = new List<Vec2>();
        public readonly List<Vec2> Tangents = new List<Vec2>();
        /// <summary>Signed turn angle (radians) per metre at each point; positive = turning left.</summary>
        public readonly List<float> Curvature = new List<float>();
        public readonly List<float> CumulativeLength = new List<float>();
        public float Length { get; private set; }
        public int Count => Points.Count;

        public TrackSpline(IList<Vec2> control, float spacing = 3f, int samplesPerSegment = 24)
        {
            if (control == null || control.Count < 4) throw new ArgumentException("A closed track needs at least 4 control points");
            // 1. Dense sampling of the closed Catmull-Rom curve.
            var dense = new List<Vec2>();
            int n = control.Count;
            for (int i = 0; i < n; i++)
            {
                Vec2 p0 = control[(i - 1 + n) % n], p1 = control[i], p2 = control[(i + 1) % n], p3 = control[(i + 2) % n];
                for (int s = 0; s < samplesPerSegment; s++)
                {
                    float t = s / (float)samplesPerSegment;
                    dense.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }
            // 2. Resample by arc length.
            float total = 0f;
            for (int i = 0; i < dense.Count; i++) total += Vec2.Distance(dense[i], dense[(i + 1) % dense.Count]);
            int target = Math.Max(16, (int)Math.Round(total / spacing));
            float step = total / target;
            float acc = 0f;
            int seg = 0;
            float segLen = Vec2.Distance(dense[0], dense[1 % dense.Count]);
            float segPos = 0f;
            for (int k = 0; k < target; k++)
            {
                float want = k * step;
                while (acc + (segLen - segPos) < want && seg < dense.Count * 2)
                {
                    acc += segLen - segPos;
                    seg = (seg + 1) % dense.Count;
                    segPos = 0f;
                    segLen = Vec2.Distance(dense[seg], dense[(seg + 1) % dense.Count]);
                }
                float within = want - acc + segPos;
                float t = segLen > 1e-6f ? within / segLen : 0f;
                Points.Add(Vec2.Lerp(dense[seg], dense[(seg + 1) % dense.Count], t));
            }
            // 3. Derived data.
            float cum = 0f;
            for (int i = 0; i < Points.Count; i++)
            {
                Vec2 prev = Points[(i - 1 + Points.Count) % Points.Count], next = Points[(i + 1) % Points.Count];
                Tangents.Add((next - prev).Normalized);
                CumulativeLength.Add(cum);
                cum += Vec2.Distance(Points[i], next);
            }
            Length = cum;
            for (int i = 0; i < Points.Count; i++)
            {
                Vec2 a = Tangents[(i - 1 + Points.Count) % Points.Count], b = Tangents[(i + 1) % Points.Count];
                float angle = (float)Math.Atan2(Vec2.Cross(a, b), Vec2.Dot(a, b));
                float dist = Vec2.Distance(Points[(i - 1 + Points.Count) % Points.Count], Points[(i + 1) % Points.Count]);
                Curvature.Add(dist > 1e-4f ? angle / dist : 0f);
            }
        }

        public static Vec2 CatmullRom(Vec2 p0, Vec2 p1, Vec2 p2, Vec2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }

        public Vec2 LeftNormal(int i) => Tangents[Wrap(i)].Left;
        public int Wrap(int i) => ((i % Count) + Count) % Count;

        /// <summary>Nearest sample index. With a hint, only a window around it is searched (fast and robust against shortcuts).</summary>
        public int NearestIndex(Vec2 p, int hint = -1, int window = 25)
        {
            int best = 0; float bestD = float.MaxValue;
            if (hint < 0)
            {
                for (int i = 0; i < Count; i++) { float d = (Points[i] - p).SqrLength; if (d < bestD) { bestD = d; best = i; } }
                return best;
            }
            for (int k = -window; k <= window; k++)
            {
                int i = Wrap(hint + k);
                float d = (Points[i] - p).SqrLength;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>Largest absolute curvature within the next `metres` metres from index i.</summary>
        public float MaxCurvatureAhead(int i, float metres)
        {
            float max = 0f; float travelled = 0f; int idx = i;
            while (travelled < metres)
            {
                max = Math.Max(max, Math.Abs(Curvature[Wrap(idx)]));
                travelled += Vec2.Distance(Points[Wrap(idx)], Points[Wrap(idx + 1)]);
                idx++;
                if (idx - i > Count) break;
            }
            return max;
        }

        /// <summary>Signed lateral offset of p from the centreline at index i (positive = left of travel).</summary>
        public float LateralOffset(Vec2 p, int i) => Vec2.Dot(p - Points[Wrap(i)], LeftNormal(i));

        /// <summary>True if any two non-adjacent centreline segments cross: the layout would overlap itself.</summary>
        public bool SelfIntersects(float minSeparation)
        {
            for (int i = 0; i < Count; i++)
            {
                for (int j = i + 2; j < Count; j++)
                {
                    if (i == 0 && j == Count - 1) continue;
                    if (SegmentsCross(Points[i], Points[Wrap(i + 1)], Points[j], Points[Wrap(j + 1)])) return true;
                }
            }
            // Also reject layouts whose non-adjacent parts come closer than the road width.
            int skip = (int)(minSeparation / Math.Max(0.1f, Length / Count)) + 2;
            for (int i = 0; i < Count; i++)
                for (int j = i + skip; j < Count - skip + i && j < Count; j++)
                    if ((Points[i] - Points[j]).SqrLength < minSeparation * minSeparation) return true;
            return false;
        }

        private static bool SegmentsCross(Vec2 a, Vec2 b, Vec2 c, Vec2 d)
        {
            float d1 = Vec2.Cross(b - a, c - a), d2 = Vec2.Cross(b - a, d - a), d3 = Vec2.Cross(d - c, a - c), d4 = Vec2.Cross(d - c, b - c);
            return ((d1 > 0) != (d2 > 0)) && ((d3 > 0) != (d4 > 0));
        }
    }
}
