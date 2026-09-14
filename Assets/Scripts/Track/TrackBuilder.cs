using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using TurboLoop.Core;

namespace TurboLoop.Track
{
    /// <summary>
    /// Turns a TrackLayout into geometry: asphalt road mesh, red/white kerbs, edge lines, barriers, start line,
    /// gantry, grass and scenery. Also answers "where is the grid slot" and "where is the centreline".
    /// </summary>
    public class TrackBuilder : MonoBehaviour
    {
        public TrackLayout Layout { get; private set; }
        public TrackSpline Spline { get; private set; }
        public float RoadWidth { get; private set; }
        public Vector3[] Waypoints { get; private set; }
        public Vector3 Center { get; private set; }

        public static TrackBuilder Build(TrackLayout layout, Transform parent)
        {
            var go = new GameObject("Track " + layout.Name);
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<TrackBuilder>();
            b.Construct(layout);
            return b;
        }

        public Vector3 P(int i) => Waypoints[Spline.Wrap(i)];
        public Vector3 Left(int i) { var n = Spline.LeftNormal(i); return new Vector3(n.X, 0f, n.Z); }
        public Vector3 Tangent(int i) { var t = Spline.Tangents[Spline.Wrap(i)]; return new Vector3(t.X, 0f, t.Z); }
        public Quaternion HeadingAt(int i) => Quaternion.LookRotation(Tangent(i), Vector3.up);
        public int NearestIndex(Vector3 pos, int hint) => Spline.NearestIndex(new Vec2(pos.x, pos.z), hint, 30);

        /// <summary>Staggered two-column grid just past the start line, pole position first.</summary>
        public void GridSlot(int slot, out Vector3 position, out Quaternion rotation)
        {
            int row = slot / 2, col = slot % 2;
            int index = Spline.Wrap(14 - row * 4 - col * 2);
            float lateral = (col == 0 ? 1f : -1f) * RoadWidth * 0.22f;
            position = P(index) + Left(index) * lateral + Vector3.up * 0.3f;
            rotation = HeadingAt(index);
        }

        private void Construct(TrackLayout layout)
        {
            Layout = layout;
            RoadWidth = layout.RoadWidth;
            Spline = new TrackSpline(layout.ControlPoints, 3f);
            Waypoints = new Vector3[Spline.Count];
            var sum = Vector3.zero;
            for (int i = 0; i < Spline.Count; i++) { Waypoints[i] = new Vector3(Spline.Points[i].X, 0f, Spline.Points[i].Z); sum += Waypoints[i]; }
            Center = sum / Spline.Count;

            BuildGround(layout.Theme);
            float w = RoadWidth / 2f;
            var asphalt = Materials.Textured(Materials.Noise(256, new Color(0.22f, 0.22f, 0.24f), new Color(0.3f, 0.3f, 0.32f), 11, 2f), Color.white, new Vector2(1f, 1f), 0.15f);
            BuildStrip("Road", -w, w, 0.03f, asphalt, 8f, true);
            var kerb = Materials.Textured(Materials.Stripes(new Color(0.85f, 0.1f, 0.1f), new Color(0.95f, 0.95f, 0.95f)), Color.white, Vector2.one, 0.3f);
            BuildStrip("Kerb L", w, w + 1.3f, 0.045f, kerb, 2.5f, true);
            BuildStrip("Kerb R", -w - 1.3f, -w, 0.045f, kerb, 2.5f, true);
            var line = Materials.Get(new Color(0.95f, 0.95f, 0.95f), 0f, 0.3f);
            BuildStrip("Line L", w - 0.55f, w - 0.25f, 0.04f, line, 1f, false);
            BuildStrip("Line R", -w + 0.25f, -w + 0.55f, 0.04f, line, 1f, false);
            BuildWalls(w + 2.4f, layout.Theme);
            BuildStartLine(w);
            BuildGantry(w);
            Scenery.Populate(this, layout.Theme);
        }

        private void BuildGround(string theme)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(transform, false);
            ground.transform.position = new Vector3(Center.x, -0.02f, Center.z);
            ground.transform.localScale = new Vector3(140f, 1f, 140f);
            Color a, b;
            switch (theme)
            {
                case "mountain": a = new Color(0.3f, 0.42f, 0.22f); b = new Color(0.45f, 0.5f, 0.3f); break;
                case "city": a = new Color(0.32f, 0.32f, 0.34f); b = new Color(0.4f, 0.4f, 0.42f); break;
                default: a = new Color(0.25f, 0.5f, 0.2f); b = new Color(0.42f, 0.62f, 0.28f); break;
            }
            ground.GetComponent<Renderer>().sharedMaterial = Materials.Textured(Materials.Noise(256, a, b, 5, 1.5f), Color.white, new Vector2(220f, 220f), 0.05f);
        }

        /// <summary>A closed ribbon following the spline between two lateral offsets (metres left of centre; negative = right).</summary>
        private void BuildStrip(string name, float innerOffset, float outerOffset, float y, Material mat, float vMetresPerTile, bool collider)
        {
            int n = Spline.Count;
            var verts = new Vector3[(n + 1) * 2];
            var uvs = new Vector2[(n + 1) * 2];
            var normals = new Vector3[(n + 1) * 2];
            var tris = new int[n * 6];
            for (int i = 0; i <= n; i++)
            {
                int k = Spline.Wrap(i);
                Vector3 c = P(k); Vector3 left = Left(k);
                float v = (i < n ? Spline.CumulativeLength[k] : Spline.Length) / vMetresPerTile;
                verts[i * 2] = c + left * innerOffset + Vector3.up * y;
                verts[i * 2 + 1] = c + left * outerOffset + Vector3.up * y;
                uvs[i * 2] = new Vector2(0f, v); uvs[i * 2 + 1] = new Vector2(1f, v);
                normals[i * 2] = Vector3.up; normals[i * 2 + 1] = Vector3.up;
            }
            for (int i = 0; i < n; i++)
            {
                int a = i * 2, b = i * 2 + 1, c = (i + 1) * 2, d = (i + 1) * 2 + 1;
                // Winding so the face points up (Unity is clockwise front-facing).
                tris[i * 6] = a; tris[i * 6 + 1] = c; tris[i * 6 + 2] = b;
                tris[i * 6 + 3] = b; tris[i * 6 + 4] = c; tris[i * 6 + 5] = d;
            }
            var mesh = new Mesh { name = name };
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = verts; mesh.uv = uvs; mesh.normals = normals; mesh.triangles = tris;
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private void BuildWalls(float offset, string theme)
        {
            var root = new GameObject("Barriers").transform;
            root.SetParent(transform, false);
            var white = Materials.Get(new Color(0.92f, 0.92f, 0.9f), 0.1f, 0.4f);
            var accent = Materials.Get(theme == "city" ? new Color(0.2f, 0.5f, 0.95f) : new Color(0.85f, 0.15f, 0.15f), 0.1f, 0.4f);
            int n = Spline.Count;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < n; i++)
                {
                    Vector3 a = P(i) + Left(i) * (offset * side);
                    Vector3 b = P(i + 1) + Left(i + 1) * (offset * side);
                    Vector3 dir = b - a;
                    float len = dir.magnitude;
                    if (len < 0.05f) continue;
                    var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Barrier";
                    wall.transform.SetParent(root, false);
                    wall.transform.position = (a + b) * 0.5f + Vector3.up * 0.55f;
                    wall.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    wall.transform.localScale = new Vector3(0.5f, 1.1f, len + 0.4f);
                    wall.GetComponent<Renderer>().sharedMaterial = (i / 4) % 2 == 0 ? white : accent;
                }
            }
        }

        private void BuildStartLine(float w)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "StartLine";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.position = P(0) + Vector3.up * 0.05f;
            go.transform.rotation = HeadingAt(0);
            go.transform.localScale = new Vector3(w * 2f, 0.02f, 2.4f);
            go.GetComponent<Renderer>().sharedMaterial = Materials.Textured(Materials.Checker(Color.white, new Color(0.08f, 0.08f, 0.08f), 2), Color.white, new Vector2(w / 1.2f, 1f), 0.3f);
        }

        private void BuildGantry(float w)
        {
            var root = new GameObject("Gantry").transform;
            root.SetParent(transform, false);
            root.position = P(0);
            root.rotation = HeadingAt(0);
            var steel = Materials.Get(new Color(0.75f, 0.76f, 0.8f), 0.8f, 0.7f);
            var banner = Materials.Textured(Materials.Checker(Color.white, Color.black, 2), Color.white, new Vector2(12f, 1f), 0.3f);
            Box(root, new Vector3(w + 2.2f, 3.2f, 0f), new Vector3(0.5f, 6.4f, 0.5f), steel, "Pillar");
            Box(root, new Vector3(-w - 2.2f, 3.2f, 0f), new Vector3(0.5f, 6.4f, 0.5f), steel, "Pillar");
            Box(root, new Vector3(0f, 6.5f, 0f), new Vector3(w * 2f + 5f, 0.5f, 0.6f), steel, "Beam");
            Box(root, new Vector3(0f, 5.6f, 0f), new Vector3(w * 2f + 4f, 1.3f, 0.1f), banner, "Banner");
            var text = new GameObject("Text");
            text.transform.SetParent(root, false);
            text.transform.localPosition = new Vector3(0f, 7.6f, 0f);
            text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var tm = text.AddComponent<TextMesh>();
            tm.text = "TURBO LOOP"; tm.fontSize = 64; tm.characterSize = 0.25f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = new Color(1f, 0.85f, 0.2f);
            var font = UI.UIFactory.DefaultFont;
            if (font != null) { tm.font = font; text.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
        }

        public static GameObject Box(Transform parent, Vector3 localPos, Vector3 scale, Material mat, string name, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!collider) Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Distance from a world point to the nearest centreline sample (brute force; used at build time only).</summary>
        public float DistanceToTrack(Vector3 p)
        {
            int i = Spline.NearestIndex(new Vec2(p.x, p.z));
            return Vector3.Distance(new Vector3(p.x, 0f, p.z), P(i));
        }
    }
}
