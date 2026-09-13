using UnityEngine;
using Echobound.Core;
using Echobound.World;

namespace Echobound.Combat
{
    /// <summary>The one enemy type (THUG): idles, chases the player within range, swings when close.</summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class EnemyAI : MonoBehaviour
    {
        public string Faction = "IRON_HAND";
        public string LocationId = "TOWN_SQUARE";
        public float AggroRange = 14f;
        public float AttackRange = 1.7f;
        public float Speed = 3.6f;
        public int Damage = 8;
        public float AttackCooldown = 1.2f;

        private CharacterController _cc;
        private Health _health;
        private float _attackTimer;
        private Transform _visual;
        private bool _dead;

        public static EnemyAI Create(Vector3 position, string faction, string locationId, Material material, Font font)
        {
            var go = new GameObject("Thug " + faction);
            go.transform.position = position;
            var cc = go.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            Object.Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0, 0.9f, 0);
            visual.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            visual.GetComponent<Renderer>().sharedMaterial = material;
            var label = new GameObject("Label");
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new Vector3(0, 2.3f, 0);
            var tm = label.AddComponent<TextMesh>();
            tm.text = WorldBible.FactionName(faction) + " thug"; tm.fontSize = 40; tm.characterSize = 0.1f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = new Color(1f, 0.5f, 0.45f);
            if (font != null) { tm.font = font; label.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            label.AddComponent<Billboard>();
            var health = go.AddComponent<Health>(); health.Set(40, 40);
            var ai = go.AddComponent<EnemyAI>();
            ai.Faction = faction; ai.LocationId = locationId;
            return ai;
        }

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _health = GetComponent<Health>();
            _visual = transform.Find("Visual");
            _health.Died += OnDied;
            _health.Damaged += (a, s) => { if (_visual != null) _visual.localScale = new Vector3(0.95f, 0.8f, 0.95f); };
        }

        private void Update()
        {
            if (_dead) return;
            if (_visual != null) _visual.localScale = Vector3.Lerp(_visual.localScale, new Vector3(0.8f, 0.9f, 0.8f), 8f * Time.deltaTime);
            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null || !gm.Player.gameObject.activeInHierarchy || gm.State == GameState.Dead) { _cc.Move(Vector3.down * 5f * Time.deltaTime); return; }
            _attackTimer -= Time.deltaTime;
            Vector3 to = gm.Player.transform.position - transform.position; to.y = 0;
            float dist = to.magnitude;
            Vector3 move = Vector3.zero;
            if (dist < AggroRange && dist > AttackRange)
            {
                move = GreyboxTownBuilder.Steer(transform, to.normalized) * Speed;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), 8f * Time.deltaTime);
            }
            else if (dist <= AttackRange && _attackTimer <= 0f && gm.State == GameState.Playing)
            {
                _attackTimer = AttackCooldown;
                gm.DamagePlayer(Damage, Faction);
            }
            _cc.Move((move + Vector3.down * 8f) * Time.deltaTime);
        }

        private void OnDied(string source)
        {
            if (_dead) return;
            _dead = true;
            if (source == WorldBible.PlayerId) GameManager.Instance?.EnemyKilled(LocationId, Faction);
            if (_visual != null) _visual.localRotation = Quaternion.Euler(90, 0, 0);
            _cc.enabled = false;
            var col = gameObject.AddComponent<CapsuleCollider>(); col.height = 1.8f; col.radius = 0.35f; col.center = new Vector3(0, 0.3f, 0); col.direction = 2;
            Destroy(gameObject, 20f);
        }
    }
}
