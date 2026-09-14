using UnityEngine;
using StrikerFive.Core;

namespace StrikerFive.Pitch
{
    /// <summary>Grass, markings, goals with nets, advertising boards, stands with a crowd and floodlights. All primitives.</summary>
    public class PitchBuilder : MonoBehaviour
    {
        private Material _line;

        public static PitchBuilder Build(Transform parent)
        {
            var go = new GameObject("Pitch");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<PitchBuilder>();
            p.Construct();
            return p;
        }

        private void Construct()
        {
            float L = PitchGeometry.HalfLength, W = PitchGeometry.HalfWidth;
            _line = Materials.Get(new Color(0.97f, 0.97f, 0.97f), 0f, 0.2f);

            // Grass: pitch plus a surround.
            var grass = GameObject.CreatePrimitive(PrimitiveType.Plane);
            grass.name = "Grass";
            grass.transform.SetParent(transform, false);
            grass.transform.localScale = new Vector3((L + 12f) * 2f / 10f, 1f, (W + 12f) * 2f / 10f);
            grass.GetComponent<Renderer>().sharedMaterial = Materials.Textured(Materials.Grass(), new Vector2(1f, 1f), 0.08f);
            // Stripes run across the pitch: rotate the texture by rotating the plane's UV via 90° object rotation.
            grass.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            grass.transform.localScale = new Vector3((W + 12f) * 2f / 10f, 1f, (L + 12f) * 2f / 10f);

            // Markings.
            Line(new Vector3(-L, 0, 0), new Vector3(L, 0, 0), 0.12f, W * 2f, true);
            Line(new Vector3(0, 0, -W), new Vector3(0, 0, W), 0.12f, 0, false);
            Rect(-L, L, -W, W);
            foreach (int s in new[] { -1, 1 })
            {
                float bx0 = s * L, bx1 = s * (L - PitchGeometry.BoxLength);
                Rect(Mathf.Min(bx0, bx1), Mathf.Max(bx0, bx1), -PitchGeometry.BoxHalfWidth, PitchGeometry.BoxHalfWidth);
                float gx1 = s * (L - 4f);
                Rect(Mathf.Min(bx0, gx1), Mathf.Max(bx0, gx1), -5.5f, 5.5f);
                Spot(new Vector3(s * (L - 7f), 0, 0));
                Arc(new Vector3(s * (L - 7f), 0, 0), 6f, s > 0 ? 100f : -80f, 160f);
                Goal(s);
            }
            Circle(Vector3.zero, 7f);
            Spot(Vector3.zero);

            Boards();
            Stands();
            Floodlights();
        }

        private void Line(Vector3 a, Vector3 b, float width, float unused, bool unused2)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Line";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.position = (a + b) * 0.5f + Vector3.up * 0.012f;
            go.transform.rotation = Quaternion.LookRotation(b - a, Vector3.up);
            go.transform.localScale = new Vector3(width, 0.01f, Vector3.Distance(a, b));
            go.GetComponent<Renderer>().sharedMaterial = _line;
        }

        private void Rect(float x0, float x1, float z0, float z1)
        {
            Line(new Vector3(x0, 0, z0), new Vector3(x1, 0, z0), 0.12f, 0, false);
            Line(new Vector3(x0, 0, z1), new Vector3(x1, 0, z1), 0.12f, 0, false);
            Line(new Vector3(x0, 0, z0), new Vector3(x0, 0, z1), 0.12f, 0, false);
            Line(new Vector3(x1, 0, z0), new Vector3(x1, 0, z1), 0.12f, 0, false);
        }

        private void Circle(Vector3 c, float r) => Arc(c, r, 0f, 360f);

        private void Arc(Vector3 c, float r, float startDeg, float sweepDeg)
        {
            int segs = Mathf.Max(8, (int)(sweepDeg / 6f));
            for (int i = 0; i < segs; i++)
            {
                float a0 = (startDeg + sweepDeg * i / segs) * Mathf.Deg2Rad, a1 = (startDeg + sweepDeg * (i + 1) / segs) * Mathf.Deg2Rad;
                var p0 = c + new Vector3(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r);
                var p1 = c + new Vector3(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r);
                Line(p0, p1, 0.12f, 0, false);
            }
        }

        private void Spot(Vector3 c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.position = c + Vector3.up * 0.012f;
            go.transform.localScale = new Vector3(0.35f, 0.005f, 0.35f);
            go.GetComponent<Renderer>().sharedMaterial = _line;
        }

        private void Goal(int side)
        {
            float L = PitchGeometry.HalfLength, gw = PitchGeometry.GoalHalfWidth, gh = PitchGeometry.GoalHeight, gd = PitchGeometry.GoalDepth;
            var root = new GameObject("Goal " + side).transform;
            root.SetParent(transform, false);
            var post = Materials.Get(Color.white, 0.2f, 0.6f);
            float x = side * L;
            Cyl(root, new Vector3(x, gh / 2f, gw), 0.12f, gh, post);
            Cyl(root, new Vector3(x, gh / 2f, -gw), 0.12f, gh, post);
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bar.transform.SetParent(root, false);
            bar.transform.position = new Vector3(x, gh, 0);
            bar.transform.rotation = Quaternion.Euler(90f, 0, 0);
            bar.transform.localScale = new Vector3(0.12f, gw, 0.12f);
            bar.GetComponent<Renderer>().sharedMaterial = post;
            // Net: thin translucent panels with colliders so the ball stays in the goal.
            var net = Materials.Get(new Color(0.9f, 0.9f, 0.9f, 0.35f), 0f, 0.1f);
            net.SetFloat("_Mode", 3f); net.SetInt("_SrcBlend", 5); net.SetInt("_DstBlend", 10); net.SetInt("_ZWrite", 0); net.EnableKeyword("_ALPHABLEND_ON"); net.renderQueue = 3000;
            float bx = x + side * gd / 2f;
            Box(root, new Vector3(x + side * gd, gh / 2f, 0), new Vector3(0.05f, gh, gw * 2f), net);                 // back
            Box(root, new Vector3(bx, gh / 2f, gw), new Vector3(gd, gh, 0.05f), net);                                  // sides
            Box(root, new Vector3(bx, gh / 2f, -gw), new Vector3(gd, gh, 0.05f), net);
            Box(root, new Vector3(bx, gh, 0), new Vector3(gd, 0.05f, gw * 2f), net);                                   // roof
            Cyl(root, new Vector3(x + side * gd, gh / 2f, gw), 0.06f, gh, post);
            Cyl(root, new Vector3(x + side * gd, gh / 2f, -gw), 0.06f, gh, post);
        }

        private void Boards()
        {
            float L = PitchGeometry.HalfLength + 3.5f, W = PitchGeometry.HalfWidth + 3f;
            Color[] cols = { new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.4f, 0.9f), new Color(0.95f, 0.85f, 0.2f), new Color(0.2f, 0.75f, 0.4f), Color.white };
            var root = new GameObject("Boards").transform; root.SetParent(transform, false);
            int i = 0;
            for (float x = -L + 4f; x < L - 3f; x += 8f)
            {
                Box(root, new Vector3(x, 0.5f, W), new Vector3(7.6f, 1f, 0.25f), Materials.Get(cols[i++ % cols.Length], 0f, 0.5f));
                Box(root, new Vector3(x, 0.5f, -W), new Vector3(7.6f, 1f, 0.25f), Materials.Get(cols[i++ % cols.Length], 0f, 0.5f));
            }
            for (float z = -W + 4f; z < W - 3f; z += 8f)
            {
                if (Mathf.Abs(z) < 5f) continue;
                Box(root, new Vector3(L, 0.5f, z), new Vector3(0.25f, 1f, 7.6f), Materials.Get(cols[i++ % cols.Length], 0f, 0.5f));
                Box(root, new Vector3(-L, 0.5f, z), new Vector3(0.25f, 1f, 7.6f), Materials.Get(cols[i++ % cols.Length], 0f, 0.5f));
            }
        }

        private void Stands()
        {
            var root = new GameObject("Stands").transform; root.SetParent(transform, false);
            var concrete = Materials.Get(new Color(0.55f, 0.55f, 0.58f), 0f, 0.3f);
            var rng = new System.Random(9);
            Color[] crowd = { new Color(0.85f, 0.15f, 0.15f), new Color(0.15f, 0.3f, 0.85f), new Color(0.95f, 0.9f, 0.3f), Color.white, new Color(0.1f, 0.1f, 0.12f), new Color(0.2f, 0.7f, 0.35f) };
            float L = PitchGeometry.HalfLength + 8f, W = PitchGeometry.HalfWidth + 7f;
            for (int sideZ = -1; sideZ <= 1; sideZ += 2)
                for (int row = 0; row < 12; row++)
                {
                    float z = sideZ * (W + row * 1.5f);
                    float y = 0.5f + row * 0.9f;
                    Box(root, new Vector3(0, y, z), new Vector3(L * 2f + 10f, 1f, 1.5f), concrete);
                    for (float x = -L - 4f; x < L + 4f; x += 1.1f)
                        if (rng.NextDouble() < 0.85)
                            Box(root, new Vector3(x + (float)rng.NextDouble() * 0.3f, y + 0.9f, z + sideZ * 0.2f), new Vector3(0.5f, 0.8f, 0.5f), Materials.Get(crowd[rng.Next(crowd.Length)], 0f, 0.2f), false);
                    Box(root, new Vector3(0, 12.5f, sideZ * (W + 9f)), new Vector3(L * 2f + 12f, 0.4f, 20f), Materials.Get(new Color(0.3f, 0.3f, 0.34f)), false);
                }
            for (int sideX = -1; sideX <= 1; sideX += 2)
                for (int row = 0; row < 8; row++)
                {
                    float x = sideX * (L + row * 1.5f);
                    float y = 0.5f + row * 0.9f;
                    Box(root, new Vector3(x, y, 0), new Vector3(1.5f, 1f, W * 2f - 4f), concrete);
                    for (float z = -W + 3f; z < W - 3f; z += 1.1f)
                        if (rng.NextDouble() < 0.8)
                            Box(root, new Vector3(x + sideX * 0.2f, y + 0.9f, z + (float)rng.NextDouble() * 0.3f), new Vector3(0.5f, 0.8f, 0.5f), Materials.Get(crowd[rng.Next(crowd.Length)], 0f, 0.2f), false);
                }
        }

        private void Floodlights()
        {
            var root = new GameObject("Floodlights").transform; root.SetParent(transform, false);
            var steel = Materials.Get(new Color(0.7f, 0.7f, 0.72f), 0.7f, 0.6f);
            var lamp = Materials.Emissive(new Color(1f, 0.98f, 0.9f), 3f);
            float L = PitchGeometry.HalfLength + 14f, W = PitchGeometry.HalfWidth + 16f;
            foreach (int sx in new[] { -1, 1 })
                foreach (int sz in new[] { -1, 1 })
                {
                    var pos = new Vector3(sx * L, 0, sz * W);
                    Cyl(root, pos + Vector3.up * 14f, 0.6f, 28f, steel);
                    Box(root, pos + Vector3.up * 29f, new Vector3(5f, 3f, 0.6f), lamp, false).transform.rotation = Quaternion.LookRotation(new Vector3(-sx, -0.6f, -sz));
                }
        }

        private static GameObject Box(Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (!collider) Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static void Cyl(Transform parent, Vector3 pos, float radius, float height, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(radius * 2f, height / 2f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
