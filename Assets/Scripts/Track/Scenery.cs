using UnityEngine;
using TurboLoop.Core;

namespace TurboLoop.Track
{
    /// <summary>Trees, buildings, grandstand, tyre stacks and flags placed around the circuit per theme.</summary>
    public static class Scenery
    {
        public static void Populate(TrackBuilder t, string theme)
        {
            var root = new GameObject("Scenery").transform;
            root.SetParent(t.transform, false);
            var rng = new System.Random(t.Layout.Name.GetHashCode());
            float clearance = t.RoadWidth / 2f + 7f;

            // Compute bounds of the circuit for scattering.
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var p in t.Waypoints) { minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z); }
            minX -= 90f; maxX += 90f; minZ -= 90f; maxZ += 90f;

            int count = theme == "city" ? 90 : 160;
            for (int k = 0; k < count; k++)
            {
                var pos = new Vector3(Mathf.Lerp(minX, maxX, (float)rng.NextDouble()), 0f, Mathf.Lerp(minZ, maxZ, (float)rng.NextDouble()));
                if (t.DistanceToTrack(pos) < clearance + (theme == "city" ? 8f : 0f)) continue;
                switch (theme)
                {
                    case "city": Building(root, pos, rng); break;
                    case "mountain": Pine(root, pos, rng); break;
                    default: if (rng.NextDouble() < 0.7) Tree(root, pos, rng); else Palm(root, pos, rng); break;
                }
            }

            Grandstand(t, root);
            TyreStacks(t, root, rng);
            Flags(t, root, rng);
        }

        private static void Tree(Transform root, Vector3 pos, System.Random rng)
        {
            float h = 3f + (float)rng.NextDouble() * 3f;
            var trunk = Materials.Get(new Color(0.35f, 0.22f, 0.12f));
            var leaves = Materials.Get(Color.Lerp(new Color(0.15f, 0.45f, 0.15f), new Color(0.35f, 0.6f, 0.2f), (float)rng.NextDouble()));
            Prim(root, PrimitiveType.Cylinder, pos + Vector3.up * h * 0.5f, new Vector3(0.5f, h * 0.5f, 0.5f), trunk);
            Prim(root, PrimitiveType.Sphere, pos + Vector3.up * (h + 1.5f), new Vector3(4.5f, 4f, 4.5f), leaves);
        }

        private static void Palm(Transform root, Vector3 pos, System.Random rng)
        {
            float h = 6f + (float)rng.NextDouble() * 3f;
            var trunk = Materials.Get(new Color(0.5f, 0.38f, 0.22f));
            var leaves = Materials.Get(new Color(0.2f, 0.55f, 0.25f));
            var t = Prim(root, PrimitiveType.Cylinder, pos + Vector3.up * h * 0.5f, new Vector3(0.4f, h * 0.5f, 0.4f), trunk);
            t.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 8f, (float)rng.NextDouble() * 360f, 0f);
            for (int i = 0; i < 5; i++)
            {
                var leaf = Prim(root, PrimitiveType.Cube, pos + Vector3.up * h, new Vector3(0.6f, 0.08f, 4f), leaves, false);
                leaf.transform.rotation = Quaternion.Euler(-25f, i * 72f, 0f);
                leaf.transform.position += leaf.transform.forward * 1.6f;
            }
        }

        private static void Pine(Transform root, Vector3 pos, System.Random rng)
        {
            float h = 5f + (float)rng.NextDouble() * 5f;
            var trunk = Materials.Get(new Color(0.3f, 0.2f, 0.1f));
            var leaves = Materials.Get(new Color(0.1f, 0.35f, 0.18f));
            Prim(root, PrimitiveType.Cylinder, pos + Vector3.up * h * 0.5f, new Vector3(0.5f, h * 0.5f, 0.5f), trunk);
            for (int i = 0; i < 3; i++)
            {
                float s = 5f - i * 1.3f;
                Prim(root, PrimitiveType.Sphere, pos + Vector3.up * (h * 0.45f + i * h * 0.22f), new Vector3(s, s * 0.7f, s), leaves, false);
            }
        }

        private static void Building(Transform root, Vector3 pos, System.Random rng)
        {
            float w = 10f + (float)rng.NextDouble() * 14f, d = 10f + (float)rng.NextDouble() * 14f, h = 12f + (float)rng.NextDouble() * 40f;
            var shade = 0.35f + (float)rng.NextDouble() * 0.35f;
            var body = Materials.Textured(Materials.Stripes(new Color(shade, shade, shade + 0.05f), new Color(0.55f, 0.7f, 0.9f), 2, 8), Color.white, new Vector2(w / 3f, h / 3f), 0.5f);
            var b = Prim(root, PrimitiveType.Cube, pos + Vector3.up * h * 0.5f, new Vector3(w, h, d), body);
            b.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 90f, 0f);
            Prim(root, PrimitiveType.Cube, pos + Vector3.up * (h + 0.3f), new Vector3(w * 0.9f, 0.6f, d * 0.9f), Materials.Get(new Color(0.2f, 0.2f, 0.22f)), false).transform.rotation = b.transform.rotation;
        }

        private static void Grandstand(TrackBuilder t, Transform root)
        {
            int start = 20; int length = 30;
            var steel = Materials.Get(new Color(0.6f, 0.62f, 0.66f), 0.5f, 0.5f);
            Color[] seats = { new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.4f, 0.9f), new Color(0.95f, 0.8f, 0.2f), new Color(0.2f, 0.75f, 0.35f) };
            float baseOffset = t.RoadWidth / 2f + 6f;
            for (int i = start; i < start + length; i += 2)
            {
                Vector3 c = t.P(i); Vector3 left = t.Left(i);
                var rot = Quaternion.LookRotation(-left, Vector3.up);
                for (int row = 0; row < 6; row++)
                {
                    var pos = c + left * (baseOffset + row * 1.6f) + Vector3.up * (0.6f + row * 0.9f);
                    var step = Prim(root, PrimitiveType.Cube, pos, new Vector3(6.2f, 1.2f + row * 0.0f, 1.6f), row == 0 ? steel : Materials.Get(seats[(i / 2 + row) % seats.Length], 0f, 0.3f));
                    step.transform.rotation = rot;
                }
                var roof = Prim(root, PrimitiveType.Cube, c + left * (baseOffset + 5f) + Vector3.up * 9f, new Vector3(6.2f, 0.3f, 12f), steel, false);
                roof.transform.rotation = rot;
                Prim(root, PrimitiveType.Cube, c + left * (baseOffset + 10.5f) + Vector3.up * 4.5f, new Vector3(0.4f, 9f, 0.4f), steel).transform.rotation = rot;
            }
        }

        private static void TyreStacks(TrackBuilder t, Transform root, System.Random rng)
        {
            var black = Materials.Get(new Color(0.07f, 0.07f, 0.07f), 0f, 0.35f);
            var white = Materials.Get(new Color(0.9f, 0.9f, 0.9f), 0f, 0.35f);
            int n = t.Spline.Count;
            for (int i = 0; i < n; i += 3)
            {
                float k = t.Spline.Curvature[i];
                if (Mathf.Abs(k) < 0.028f) continue;
                float side = k > 0f ? -1f : 1f; // outside of the corner
                Vector3 pos = t.P(i) + t.Left(i) * (side * (t.RoadWidth / 2f + 4.2f));
                for (int s = 0; s < 3; s++)
                    Prim(root, PrimitiveType.Cylinder, pos + Vector3.up * (0.3f + s * 0.55f), new Vector3(1.1f, 0.27f, 1.1f), (i / 3 + s) % 4 == 0 ? white : black, s == 0);
            }
        }

        private static void Flags(TrackBuilder t, Transform root, System.Random rng)
        {
            Color[] colors = { new Color(0.95f, 0.3f, 0.2f), new Color(0.2f, 0.5f, 0.95f), new Color(0.95f, 0.85f, 0.2f), new Color(0.3f, 0.8f, 0.4f) };
            var pole = Materials.Get(new Color(0.8f, 0.8f, 0.82f), 0.6f, 0.6f);
            int n = t.Spline.Count;
            for (int i = 8; i < n; i += 14)
            {
                if (Mathf.Abs(t.Spline.Curvature[i]) > 0.02f) continue;
                Vector3 pos = t.P(i) + t.Left(i) * (t.RoadWidth / 2f + 4.5f) * (i % 28 == 8 ? 1f : -1f);
                Prim(root, PrimitiveType.Cylinder, pos + Vector3.up * 3f, new Vector3(0.15f, 3f, 0.15f), pole, false);
                var flag = Prim(root, PrimitiveType.Cube, pos + Vector3.up * 5.2f, new Vector3(0.05f, 1.6f, 2.2f), Materials.Get(colors[(i / 14) % colors.Length], 0f, 0.3f), false);
                flag.transform.rotation = t.HeadingAt(i);
                flag.transform.position += flag.transform.forward * 1.1f;
            }
        }

        private static GameObject Prim(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            if (!collider) Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
