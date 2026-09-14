using UnityEngine;
using StrikerFive.Core;

namespace StrikerFive.Players
{
    public static class PlayerFactory
    {
        public static PlayerAgent Create(TeamRuntime team, Role role, string name, int number, Vector3 position, Transform parent, bool isKeeper)
        {
            var go = new GameObject($"{team.Def.Short} {number} {name}");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var cc = go.AddComponent<CharacterController>();
            cc.radius = 0.4f; cc.height = 1.9f; cc.center = new Vector3(0f, 0.95f, 0f); cc.slopeLimit = 45f; cc.stepOffset = 0.3f;
            var agent = go.AddComponent<PlayerAgent>();
            agent.Team = team; agent.Role = role; agent.PlayerName = name; agent.Number = number;

            var model = Resources.Load<GameObject>("PlayerModel");
            if (model != null)
            {
                var inst = Object.Instantiate(model, go.transform);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                var animator = inst.GetComponentInChildren<Animator>();
                agent.AttachAnimator(animator);
                // Tint the kit: multiply every material colour by the team shirt colour.
                foreach (var r in inst.GetComponentsInChildren<Renderer>())
                    foreach (var m in r.materials) m.color = Color.Lerp(m.color, isKeeper ? Color.Lerp(team.Def.Accent, new Color(0.2f, 0.9f, 0.4f), 0.6f) : team.Def.Shirt, 0.65f);
                AddIndicators(agent, go.transform, team, name);
                agent.SetControlled(false);
                return agent;
            }
            var body = new GameObject("Body").transform;
            body.SetParent(go.transform, false);
            agent.AttachBody(body);
            Color shirt = isKeeper ? Color.Lerp(team.Def.Accent, new Color(0.2f, 0.9f, 0.4f), 0.6f) : team.Def.Shirt;
            var shirtMat = Materials.Get(shirt, 0f, 0.3f);
            var shortsMat = Materials.Get(isKeeper ? new Color(0.1f, 0.1f, 0.1f) : team.Def.Shorts, 0f, 0.3f);
            var skin = Materials.Get(new Color(0.85f, 0.65f, 0.5f), 0f, 0.3f);
            var socks = Materials.Get(team.Def.Shirt * 0.8f, 0f, 0.3f);
            var boots = Materials.Get(new Color(0.1f, 0.1f, 0.1f), 0.2f, 0.5f);
            Part(body, PrimitiveType.Capsule, new Vector3(0f, 1.2f, 0f), new Vector3(0.95f, 0.46f, 0.62f), shirtMat, "Torso");
            Part(body, PrimitiveType.Cube, new Vector3(0f, 0.74f, 0f), new Vector3(0.82f, 0.3f, 0.52f), shortsMat, "Shorts");
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 1.9f, 0f), new Vector3(0.56f, 0.58f, 0.56f), skin, "Head");
            Part(body, PrimitiveType.Sphere, new Vector3(0f, 2.08f, -0.04f), new Vector3(0.58f, 0.3f, 0.58f), Materials.Get(new Color(0.15f, 0.1f, 0.08f)), "Hair");
            // Legs and arms hang from pivots so they can swing while running.
            var legL = Pivot(body, new Vector3(-0.2f, 0.6f, 0f));
            var legR = Pivot(body, new Vector3(0.2f, 0.6f, 0f));
            Part(legL, PrimitiveType.Cylinder, new Vector3(0f, -0.3f, 0f), new Vector3(0.26f, 0.3f, 0.26f), socks, "Leg");
            Part(legL, PrimitiveType.Cube, new Vector3(0f, -0.56f, 0.08f), new Vector3(0.28f, 0.14f, 0.44f), boots, "Boot");
            Part(legR, PrimitiveType.Cylinder, new Vector3(0f, -0.3f, 0f), new Vector3(0.26f, 0.3f, 0.26f), socks, "Leg");
            Part(legR, PrimitiveType.Cube, new Vector3(0f, -0.56f, 0.08f), new Vector3(0.28f, 0.14f, 0.44f), boots, "Boot");
            var armL = Pivot(body, new Vector3(-0.56f, 1.42f, 0f));
            var armR = Pivot(body, new Vector3(0.56f, 1.42f, 0f));
            Part(armL, PrimitiveType.Capsule, new Vector3(0f, -0.3f, 0f), new Vector3(0.22f, 0.32f, 0.22f), shirtMat, "Arm");
            Part(armL, PrimitiveType.Sphere, new Vector3(0f, -0.62f, 0f), new Vector3(0.2f, 0.2f, 0.2f), skin, "Hand");
            Part(armR, PrimitiveType.Capsule, new Vector3(0f, -0.3f, 0f), new Vector3(0.22f, 0.32f, 0.22f), shirtMat, "Arm");
            Part(armR, PrimitiveType.Sphere, new Vector3(0f, -0.62f, 0f), new Vector3(0.2f, 0.2f, 0.2f), skin, "Hand");
            agent.AttachLimbs(legL, legR, armL, armR);
            var numberGo = new GameObject("Number");
            numberGo.transform.SetParent(body, false);
            numberGo.transform.localPosition = new Vector3(0f, 1.3f, -0.33f);
            numberGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var tm = numberGo.AddComponent<TextMesh>();
            tm.text = number.ToString(); tm.fontSize = 40; tm.characterSize = 0.09f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = team.Def.Accent;
            var font = UI.UIFactory.DefaultFont;
            if (font != null) { tm.font = font; numberGo.GetComponent<MeshRenderer>().sharedMaterial = font.material; }

            AddIndicators(agent, go.transform, team, name);
            agent.SetControlled(false);
            return agent;
        }

        private static void AddIndicators(PlayerAgent agent, Transform root, TeamRuntime team, string name)
        {
            var font = UI.UIFactory.DefaultFont;
            var go = root.gameObject;
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(go.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            ring.transform.localScale = new Vector3(1.8f, 0.01f, 1.8f);
            ring.GetComponent<Renderer>().sharedMaterial = Materials.Emissive(team.Def.Accent == Color.white ? new Color(1f, 0.9f, 0.3f) : team.Def.Accent, 1.5f);
            agent.Ring = ring.transform;
            var label = new GameObject("Label");
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new Vector3(0f, 2.75f, 0f);
            var lt = label.AddComponent<TextMesh>();
            lt.text = name; lt.fontSize = 48; lt.characterSize = 0.09f; lt.anchor = TextAnchor.MiddleCenter; lt.alignment = TextAlignment.Center; lt.color = Color.white;
            if (font != null) { lt.font = font; label.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            label.AddComponent<Billboard>();
            agent.Label = lt;
        }

        private static Transform Pivot(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("Pivot").transform;
            go.SetParent(parent, false);
            go.localPosition = localPos;
            return go;
        }

        private static GameObject Part(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat, string name)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }

    public class Billboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
