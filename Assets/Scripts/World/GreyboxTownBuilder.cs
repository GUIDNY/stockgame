using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Echobound.Core;
using Echobound.Inventory;

namespace Echobound.World
{
    /// <summary>
    /// Builds the greybox town from primitives at runtime: ground, ten location zones with distinct looks,
    /// search spots, beds, a notice board, a sun and some wandering townsfolk. Everything static lives here;
    /// dynamic contents (items in search spots) are synced from WorldState.
    /// </summary>
    public class GreyboxTownBuilder : MonoBehaviour
    {
        public Light Sun { get; private set; }
        public Vector3 SpawnPoint { get; private set; }
        private readonly Dictionary<string, LocationZone> _zones = new Dictionary<string, LocationZone>();
        private readonly Dictionary<string, SearchSpot> _spots = new Dictionary<string, SearchSpot>();
        private readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();
        private Shader _shader;
        private Transform _static, _dynamic;
        private System.Random _rng = new System.Random(7);
        public Material SpotEmpty, SpotFull;

        private struct ZoneDef { public string Id; public Vector3 Center; public Vector2 Size; public Color Floor; }

        private static readonly ZoneDef[] Layout =
        {
            new ZoneDef { Id = "TOWN_SQUARE", Center = new Vector3(0, 0, 0), Size = new Vector2(30, 30), Floor = new Color(0.55f, 0.53f, 0.5f) },
            new ZoneDef { Id = "TAVERN", Center = new Vector3(-30, 0, 8), Size = new Vector2(18, 14), Floor = new Color(0.45f, 0.3f, 0.2f) },
            new ZoneDef { Id = "GUARD_STATION", Center = new Vector3(30, 0, 10), Size = new Vector2(18, 14), Floor = new Color(0.4f, 0.42f, 0.5f) },
            new ZoneDef { Id = "MARKET", Center = new Vector3(0, 0, -32), Size = new Vector2(30, 18), Floor = new Color(0.6f, 0.5f, 0.35f) },
            new ZoneDef { Id = "RESIDENTIAL", Center = new Vector3(-34, 0, -32), Size = new Vector2(26, 26), Floor = new Color(0.5f, 0.45f, 0.4f) },
            new ZoneDef { Id = "WAREHOUSE", Center = new Vector3(36, 0, -34), Size = new Vector2(22, 20), Floor = new Color(0.3f, 0.3f, 0.32f) },
            new ZoneDef { Id = "FACTION_BASE", Center = new Vector3(40, 0, 40), Size = new Vector2(20, 20), Floor = new Color(0.35f, 0.2f, 0.2f) },
            new ZoneDef { Id = "FOREST", Center = new Vector3(-40, 0, 48), Size = new Vector2(40, 34), Floor = new Color(0.16f, 0.3f, 0.16f) },
            new ZoneDef { Id = "RUINS", Center = new Vector3(6, 0, 62), Size = new Vector2(26, 24), Floor = new Color(0.42f, 0.42f, 0.38f) },
            new ZoneDef { Id = "UNDERGROUND", Center = new Vector3(66, -3, 0), Size = new Vector2(20, 20), Floor = new Color(0.12f, 0.1f, 0.12f) },
        };

        public static GreyboxTownBuilder Build(Transform parent)
        {
            var go = new GameObject("Town");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<GreyboxTownBuilder>();
            b.Construct();
            return b;
        }

        public Material MaterialFor(Color c)
        {
            if (_materials.TryGetValue(c, out var m)) return m;
            if (_shader == null) _shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Legacy Shaders/Diffuse");
            m = new Material(_shader) { color = c };
            _materials[c] = m;
            return m;
        }

        private void Construct()
        {
            _static = new GameObject("Static").transform; _static.SetParent(transform, false);
            _dynamic = new GameObject("Dynamic").transform; _dynamic.SetParent(transform, false);
            SpotEmpty = MaterialFor(new Color(0.5f, 0.42f, 0.3f));
            SpotFull = MaterialFor(new Color(0.85f, 0.7f, 0.3f));

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(_static, false);
            ground.transform.localScale = new Vector3(22, 1, 22);
            ground.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(0.28f, 0.34f, 0.26f));

            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(_static, false);
            Sun = sunGo.AddComponent<Light>();
            Sun.type = LightType.Directional; Sun.shadows = LightShadows.Soft; Sun.intensity = 1f;

            foreach (var z in Layout) BuildZone(z);
            BuildWalls();
            SpawnPoint = new Vector3(0, 0.1f, 14f);
            for (int i = 0; i < 6; i++) SpawnTownsperson(i);
        }

        private void BuildZone(ZoneDef z)
        {
            var root = new GameObject(z.Id).transform;
            root.SetParent(_static, false);
            root.position = z.Center;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root, false);
            floor.transform.localPosition = new Vector3(0, -0.05f, 0);
            floor.transform.localScale = new Vector3(z.Size.x, 0.1f, z.Size.y);
            floor.GetComponent<Renderer>().sharedMaterial = MaterialFor(z.Floor);

            var zoneGo = new GameObject("Zone");
            zoneGo.transform.SetParent(root, false);
            var box = zoneGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0, 2.5f, 0);
            box.size = new Vector3(z.Size.x, 5f, z.Size.y);
            var zone = zoneGo.AddComponent<LocationZone>();
            zone.Id = z.Id; zone.Center = z.Center; zone.Size = z.Size;
            _zones[z.Id] = zone;

            Sign(root, WorldBible.LocationName(z.Id), new Vector3(0, 3.2f, z.Size.y / 2f - 1f));

            switch (z.Id)
            {
                case "TOWN_SQUARE":
                    Prop(root, PrimitiveType.Cylinder, new Vector3(0, 0.5f, 0), new Vector3(4, 0.5f, 4), new Color(0.6f, 0.65f, 0.75f), "Fountain");
                    Prop(root, PrimitiveType.Cylinder, new Vector3(0, 1.5f, 0), new Vector3(0.8f, 1.2f, 0.8f), new Color(0.6f, 0.65f, 0.75f));
                    var board = Prop(root, PrimitiveType.Cube, new Vector3(8, 1.2f, 6), new Vector3(2.2f, 1.6f, 0.2f), new Color(0.35f, 0.25f, 0.15f), "Notice board");
                    board.AddComponent<NoticeBoard>();
                    for (int i = 0; i < 4; i++) Prop(root, PrimitiveType.Cube, new Vector3(-10 + i * 6.6f, 0.4f, -10), new Vector3(1.6f, 0.8f, 0.6f), new Color(0.4f, 0.3f, 0.2f), "Bench");
                    Spot(root, z.Id, new Vector3(-10, 0.5f, 8), "the market cart", false);
                    break;
                case "TAVERN":
                    Building(root, z.Size, new Color(0.5f, 0.35f, 0.25f), openSide: 1);
                    for (int i = 0; i < 3; i++) Prop(root, PrimitiveType.Cube, new Vector3(-5 + i * 5, 0.5f, 0), new Vector3(1.8f, 1f, 1.8f), new Color(0.35f, 0.22f, 0.12f), "Table");
                    Prop(root, PrimitiveType.Cube, new Vector3(0, 0.6f, -5), new Vector3(10, 1.2f, 1), new Color(0.3f, 0.18f, 0.1f), "Bar");
                    var bed = Prop(root, PrimitiveType.Cube, new Vector3(6.5f, 0.3f, 4.5f), new Vector3(2.2f, 0.6f, 1.2f), new Color(0.7f, 0.65f, 0.6f), "Bed");
                    bed.AddComponent<BedSpot>();
                    Spot(root, z.Id, new Vector3(-6.5f, 0.5f, -5), "the tavern book", true);
                    break;
                case "GUARD_STATION":
                    Building(root, z.Size, new Color(0.35f, 0.38f, 0.48f), openSide: 1);
                    Prop(root, PrimitiveType.Cube, new Vector3(0, 0.5f, -3), new Vector3(3, 1f, 1.2f), new Color(0.3f, 0.3f, 0.35f), "Desk");
                    for (int i = 0; i < 2; i++) Prop(root, PrimitiveType.Cube, new Vector3(-6 + i * 12, 1.2f, -4), new Vector3(2.5f, 2.4f, 2.5f), new Color(0.25f, 0.25f, 0.3f), "Cell");
                    Spot(root, z.Id, new Vector3(6, 0.5f, 2), "the commander's desk", true);
                    break;
                case "MARKET":
                    for (int i = 0; i < 5; i++)
                    {
                        Prop(root, PrimitiveType.Cube, new Vector3(-12 + i * 6, 0.6f, 4), new Vector3(3, 1.2f, 1.5f), new Color(0.75f, 0.5f, 0.3f), "Stall");
                        Prop(root, PrimitiveType.Cube, new Vector3(-12 + i * 6, 2.6f, 4), new Vector3(3.4f, 0.1f, 2.4f), new Color(0.8f, 0.3f, 0.3f), "Awning");
                    }
                    Spot(root, z.Id, new Vector3(0, 0.5f, -4), "the merchant's stall", true);
                    break;
                case "RESIDENTIAL":
                    for (int i = 0; i < 4; i++)
                    {
                        var hx = (i % 2 == 0 ? -7 : 7); var hz = (i < 2 ? -7 : 7);
                        Prop(root, PrimitiveType.Cube, new Vector3(hx, 1.6f, hz), new Vector3(7, 3.2f, 7), new Color(0.6f, 0.55f, 0.5f), "House");
                        Prop(root, PrimitiveType.Cube, new Vector3(hx, 3.6f, hz), new Vector3(7.6f, 0.8f, 7.6f), new Color(0.4f, 0.25f, 0.2f), "Roof");
                    }
                    var bed2 = Prop(root, PrimitiveType.Cube, new Vector3(0, 0.3f, 0), new Vector3(2.2f, 0.6f, 1.2f), new Color(0.7f, 0.65f, 0.6f), "Bed");
                    bed2.AddComponent<BedSpot>();
                    Spot(root, z.Id, new Vector3(0, 0.5f, 11), "the doctor's cabinet", true);
                    break;
                case "WAREHOUSE":
                    Building(root, z.Size, new Color(0.3f, 0.3f, 0.33f), openSide: 3, tall: true);
                    for (int i = 0; i < 6; i++) Prop(root, PrimitiveType.Cube, new Vector3(-7 + (i % 3) * 6, 0.9f, (i < 3 ? -5 : 4)), new Vector3(2.2f, 1.8f, 2.2f), new Color(0.45f, 0.35f, 0.25f), "Crate");
                    Spot(root, z.Id, new Vector3(7, 0.5f, -6), "the loose crate", false);
                    break;
                case "FACTION_BASE":
                    Building(root, z.Size, new Color(0.4f, 0.22f, 0.22f), openSide: 2);
                    for (int i = 0; i < 6; i++) Prop(root, PrimitiveType.Cylinder, new Vector3(-9 + i * 3.6f, 1f, 11), new Vector3(0.4f, 1f, 0.4f), new Color(0.2f, 0.15f, 0.15f), "Post");
                    Prop(root, PrimitiveType.Cube, new Vector3(0, 0.6f, -4), new Vector3(4, 1.2f, 1.5f), new Color(0.25f, 0.15f, 0.15f), "Table");
                    Spot(root, z.Id, new Vector3(-6, 0.5f, -6), "the strongbox", true);
                    break;
                case "FOREST":
                    for (int i = 0; i < 34; i++)
                    {
                        var p = new Vector3((float)(_rng.NextDouble() * 2 - 1) * 17f, 0, (float)(_rng.NextDouble() * 2 - 1) * 14f);
                        if (p.magnitude < 3f) continue;
                        Prop(root, PrimitiveType.Cylinder, p + Vector3.up * 2f, new Vector3(0.5f, 2f, 0.5f), new Color(0.3f, 0.2f, 0.12f), "Trunk");
                        Prop(root, PrimitiveType.Sphere, p + Vector3.up * 4.5f, new Vector3(3f, 3f, 3f), new Color(0.12f, 0.35f, 0.14f), "Canopy", collider: false);
                    }
                    Spot(root, z.Id, new Vector3(4, 0.5f, 6), "the hollow tree", false);
                    break;
                case "RUINS":
                    for (int i = 0; i < 9; i++)
                    {
                        var p = new Vector3(-9 + (i % 3) * 9, 0, -8 + (i / 3) * 8);
                        Prop(root, PrimitiveType.Cylinder, p + Vector3.up * (1f + i % 3), new Vector3(1.2f, 1f + i % 3, 1.2f), new Color(0.5f, 0.5f, 0.45f), "Pillar");
                    }
                    Prop(root, PrimitiveType.Cube, new Vector3(0, 0.4f, 0), new Vector3(6, 0.8f, 3), new Color(0.45f, 0.45f, 0.4f), "Altar");
                    Spot(root, z.Id, new Vector3(0, 1.2f, 0), "the altar stones", false);
                    break;
                case "UNDERGROUND":
                    // A sunken pit reached by a ramp from the east side of the square.
                    Building(root, z.Size, new Color(0.15f, 0.12f, 0.15f), openSide: 3, tall: true);
                    var ramp = Prop(root, PrimitiveType.Cube, new Vector3(-14, -1.5f, 0), new Vector3(10, 0.4f, 6), new Color(0.2f, 0.18f, 0.2f), "Ramp");
                    ramp.transform.rotation = Quaternion.Euler(0, 0, -17f);
                    for (int i = 0; i < 4; i++) Prop(root, PrimitiveType.Cube, new Vector3(-4 + i * 3, 0.8f, 5), new Vector3(1.5f, 1.6f, 1.5f), new Color(0.3f, 0.25f, 0.2f), "Barrel");
                    Prop(root, PrimitiveType.Cube, new Vector3(0, 6f, 0), new Vector3(z.Size.x, 0.3f, z.Size.y), new Color(0.1f, 0.08f, 0.1f), "Ceiling");
                    var torch = new GameObject("Torch"); torch.transform.SetParent(root, false); torch.transform.localPosition = new Vector3(0, 3, 0);
                    var l = torch.AddComponent<Light>(); l.type = LightType.Point; l.range = 18f; l.intensity = 1.4f; l.color = new Color(1f, 0.7f, 0.4f);
                    Spot(root, z.Id, new Vector3(6, 0.5f, -6), "the hidden cache", false);
                    break;
            }
        }

        private void Building(Transform root, Vector2 size, Color color, int openSide, bool tall = false)
        {
            float h = tall ? 5f : 3f;
            float hx = size.x / 2f, hz = size.y / 2f;
            // Waist-high walls on three sides (NPCs and the player can still get around them), full on the open side except the doorway.
            for (int side = 0; side < 4; side++)
            {
                bool isOpen = side == openSide;
                Vector3 center; Vector3 scale;
                switch (side)
                {
                    case 0: center = new Vector3(0, h / 2f, hz); scale = new Vector3(size.x, h, 0.4f); break;
                    case 1: center = new Vector3(0, h / 2f, -hz); scale = new Vector3(size.x, h, 0.4f); break;
                    case 2: center = new Vector3(hx, h / 2f, 0); scale = new Vector3(0.4f, h, size.y); break;
                    default: center = new Vector3(-hx, h / 2f, 0); scale = new Vector3(0.4f, h, size.y); break;
                }
                if (!isOpen) { Prop(root, PrimitiveType.Cube, center, scale, color, "Wall"); continue; }
                // Doorway: two wall segments with a 4m gap in the middle.
                bool alongX = side < 2;
                float len = alongX ? size.x : size.y;
                float seg = (len - 4f) / 2f;
                for (int k = -1; k <= 1; k += 2)
                {
                    var c = center + (alongX ? new Vector3(k * (seg / 2f + 2f), 0, 0) : new Vector3(0, 0, k * (seg / 2f + 2f)));
                    var s = alongX ? new Vector3(seg, h, 0.4f) : new Vector3(0.4f, h, seg);
                    Prop(root, PrimitiveType.Cube, c, s, color, "Wall");
                }
            }
            if (tall) Prop(root, PrimitiveType.Cube, new Vector3(0, h, 0), new Vector3(size.x, 0.3f, size.y), color * 0.8f, "Roof", collider: false);
        }

        private void BuildWalls()
        {
            // Town walls: the world is bounded so the player cannot fall off.
            var root = new GameObject("Walls").transform; root.SetParent(_static, false);
            var c = new Color(0.3f, 0.3f, 0.3f);
            Prop(root, PrimitiveType.Cube, new Vector3(0, 3, 90), new Vector3(200, 6, 1), c, "Wall N");
            Prop(root, PrimitiveType.Cube, new Vector3(0, 3, -70), new Vector3(200, 6, 1), c, "Wall S");
            Prop(root, PrimitiveType.Cube, new Vector3(-80, 3, 10), new Vector3(1, 6, 200), c, "Wall W");
            Prop(root, PrimitiveType.Cube, new Vector3(90, 3, 10), new Vector3(1, 6, 200), c, "Wall E");
        }

        private GameObject Prop(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color, string name = "Prop", bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(color);
            if (!collider) Destroy(go.GetComponent<Collider>());
            return go;
        }

        private void Sign(Transform parent, string text, Vector3 localPos)
        {
            var go = new GameObject("Sign " + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text; tm.fontSize = 48; tm.characterSize = 0.18f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.95f, 0.8f);
            var font = UI.UIFactory.DefaultFont;
            if (font != null) { tm.font = font; go.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            go.AddComponent<Billboard>();
        }

        private void Spot(Transform parent, string locationId, Vector3 localPos, string label, bool isPrivate)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "SearchSpot " + locationId;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            var spot = go.AddComponent<SearchSpot>();
            spot.Init(locationId, label, isPrivate, SpotEmpty, SpotFull);
            _spots[locationId] = spot;
        }

        private void SpawnTownsperson(int i)
        {
            var go = new GameObject("Townsperson " + i);
            go.transform.SetParent(_dynamic, false);
            go.transform.position = RandomPointIn(WorldBible.Locations[i % 5]) + Vector3.up * 0.1f;
            var cc = go.AddComponent<CharacterController>(); cc.height = 1.7f; cc.radius = 0.3f; cc.center = new Vector3(0, 0.85f, 0);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0, 0.85f, 0);
            visual.transform.localScale = new Vector3(0.6f, 0.85f, 0.6f);
            visual.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(0.45f, 0.45f, 0.5f));
            go.AddComponent<BackgroundNpc>().Init(this, 100 + i);
        }

        // ------------------------------------------------------------------ queries
        public LocationZone Zone(string id) => _zones.TryGetValue(id, out var z) ? z : _zones["TOWN_SQUARE"];
        public Vector3 CenterOf(string id) => Zone(id).Center;
        public Vector3 RandomPointIn(string id) => Zone(id).RandomPoint(_rng, 3f) + Vector3.up * 0.1f;
        public SearchSpot SpotAt(string id) => _spots.TryGetValue(id, out var s) ? s : null;

        /// <summary>Simple obstacle avoidance: if something blocks the way, slide along it.</summary>
        public static Vector3 Steer(Transform t, Vector3 desired)
        {
            var origin = t.position + Vector3.up * 0.9f;
            if (!Physics.Raycast(origin, desired, out var hit, 1.6f, ~0, QueryTriggerInteraction.Ignore)) return desired;
            if (hit.collider.GetComponentInParent<Player.ThirdPersonController>() != null) return Vector3.zero;
            var right = Vector3.Cross(Vector3.up, desired).normalized;
            bool rightFree = !Physics.Raycast(origin, (desired + right).normalized, 1.6f, ~0, QueryTriggerInteraction.Ignore);
            return rightFree ? (desired + right).normalized : (desired - right).normalized;
        }

        // ------------------------------------------------------------------ dynamic content
        public void SyncSpotsFromWorld(WorldState world)
        {
            foreach (var spot in _spots.Values) spot.Contents.Clear();
            if (world != null)
                foreach (var p in world.PlacedItems)
                    if (_spots.TryGetValue(p.Location, out var spot)) spot.Contents.Add(p.Item);
            foreach (var spot in _spots.Values) spot.Refresh();
        }

        public void PlantItem(string locationId, string itemType, string label)
        {
            var world = GameManager.Instance?.World;
            if (world == null) return;
            world.PlacedItems.Add(new PlacedItem { Location = locationId, Item = Item.Create(itemType, label, "Planted here recently.") });
            SyncSpotsFromWorld(world);
        }

        public void ClearDynamic()
        {
            foreach (var spot in _spots.Values) { spot.Contents.Clear(); spot.Refresh(); }
        }
    }

    /// <summary>Keeps a text mesh facing the camera.</summary>
    public class Billboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
