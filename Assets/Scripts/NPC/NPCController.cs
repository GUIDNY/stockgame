using UnityEngine;
using Echobound.Combat;
using Echobound.Core;
using Echobound.Player;
using Echobound.World;

namespace Echobound.NPC
{
    /// <summary>
    /// Scene view of an NpcState: placeholder capsule with a name label. Walks between location anchors when
    /// the simulation moves the NPC, wanders a little in place, can be talked to, attacked and killed.
    /// </summary>
    public class NPCController : InteractableBase
    {
        public NpcState Model { get; private set; }
        public bool IsBrave => Model.Role == "GUARD_COMMANDER" || Model.Role == "CRIMINAL" || Model.Role == "FACTION_LEADER" || Model.Role == "SMUGGLER";

        private GameManager _gm;
        private CharacterController _cc;
        private Health _health;
        private Transform _visual;
        private TextMesh _label;
        private Vector3 _anchor;
        private Vector3 _target;
        private float _wanderTimer;
        private float _fleeTimer;
        private float _attackTimer;
        private bool _deadView;

        public static NPCController Create(NpcState model, GameManager gm, Transform parent)
        {
            var go = new GameObject("NPC " + model.NpcId + " " + model.DisplayName);
            go.transform.SetParent(parent, false);
            var cc = go.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0, 0.9f, 0);
            visual.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            visual.GetComponent<Renderer>().sharedMaterial = gm.Town.MaterialFor(ColorFor(model));
            var hat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hat.name = "RoleMarker";
            Destroy(hat.GetComponent<Collider>());
            hat.transform.SetParent(go.transform, false);
            hat.transform.localPosition = new Vector3(0, 1.95f, 0);
            hat.transform.localScale = new Vector3(0.5f, 0.15f, 0.5f);
            hat.GetComponent<Renderer>().sharedMaterial = gm.Town.MaterialFor(RoleColor(model.Role));
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0, 2.45f, 0);
            var tm = labelGo.AddComponent<TextMesh>();
            tm.fontSize = 40; tm.characterSize = 0.1f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            var font = UI.UIFactory.DefaultFont;
            if (font != null) { tm.font = font; labelGo.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            labelGo.AddComponent<Billboard>();
            var health = go.AddComponent<Health>();
            health.Set(100, model.Health);
            var ctrl = go.AddComponent<NPCController>();
            ctrl.Model = model; ctrl._gm = gm; ctrl._label = tm; ctrl._health = health; ctrl._visual = visual.transform; ctrl._cc = cc;
            ctrl.SetLocation(model.CurrentLocation, true);
            ctrl.RefreshFromModel();
            return ctrl;
        }

        private static Color ColorFor(NpcState m)
        {
            switch (m.Faction)
            {
                case "TOWN_GUARD": return new Color(0.35f, 0.45f, 0.75f);
                case "IRON_HAND": return new Color(0.6f, 0.2f, 0.2f);
                case "MERCHANT_CIRCLE": return new Color(0.75f, 0.6f, 0.25f);
                default: return new Color(0.6f, 0.6f, 0.6f);
            }
        }

        private static Color RoleColor(string role)
        {
            int h = 0; foreach (char c in role) h = h * 31 + c;
            return Color.HSVToRGB(Mathf.Abs(h % 360) / 360f, 0.7f, 0.9f);
        }

        private void Awake()
        {
            if (_health == null) _health = GetComponent<Health>();
        }

        private void Start()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        public override string Prompt => Model.Alive ? $"Talk to {Model.DisplayName} [E]" : $"{Model.DisplayName} (dead)";
        public override bool CanInteract => Model.Alive && !Model.HostileToPlayer;
        public override void Interact() => _gm.TalkTo(this);

        public void SetLocation(string locationId, bool teleport)
        {
            if (!WorldBible.IsValidLocation(locationId)) return;
            _anchor = _gm.Town.RandomPointIn(locationId);
            _target = _anchor;
            if (teleport)
            {
                _cc.enabled = false;
                transform.position = _anchor;
                _cc.enabled = true;
            }
        }

        public void RefreshFromModel()
        {
            if (_label != null)
            {
                _label.text = Model.Alive ? $"{Model.DisplayName}\n<size=26>{Model.Role.Replace('_', ' ').ToLowerInvariant()}</size>" : $"{Model.DisplayName}\n<size=26>(dead)</size>";
                _label.color = Model.Alive ? (Model.HostileToPlayer ? new Color(1f, 0.4f, 0.4f) : Color.white) : new Color(0.6f, 0.6f, 0.6f);
                _label.richText = true;
            }
            if (!Model.Alive && !_deadView) LayDown();
            _health.Set(100, Model.Alive ? Mathf.Max(1, Model.Health) : 0);
        }

        private void Update()
        {
            if (_deadView) return;
            if (!Model.Alive) { RefreshFromModel(); return; }
            if (_visual != null) _visual.localScale = Vector3.Lerp(_visual.localScale, new Vector3(0.7f, 0.9f, 0.7f), 8f * Time.deltaTime);
            var player = _gm.Player;
            bool playing = _gm.State == GameState.Playing;
            Vector3 move = Vector3.zero;

            if (Model.HostileToPlayer && player != null && player.gameObject.activeInHierarchy && playing)
            {
                Vector3 to = player.transform.position - transform.position; to.y = 0;
                _attackTimer -= Time.deltaTime;
                if (to.magnitude > 1.7f && to.magnitude < 18f) move = GreyboxTownBuilder.Steer(transform, to.normalized) * 3.4f;
                else if (to.magnitude <= 1.7f && _attackTimer <= 0f) { _attackTimer = 1.3f; _gm.DamagePlayer(7, Model.Faction == WorldBible.NoFaction ? "UNKNOWN" : Model.Faction); }
                if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), 8f * Time.deltaTime);
            }
            else if (_fleeTimer > 0f && player != null)
            {
                _fleeTimer -= Time.deltaTime;
                Vector3 away = transform.position - player.transform.position; away.y = 0;
                move = GreyboxTownBuilder.Steer(transform, away.normalized) * 4.5f;
            }
            else if (_gm.Dialogue != null && _gm.Dialogue.Current == Model && player != null)
            {
                Vector3 to = player.transform.position - transform.position; to.y = 0;
                if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), 6f * Time.deltaTime);
            }
            else
            {
                Vector3 to = _target - transform.position; to.y = 0;
                if (to.magnitude > 0.8f) move = GreyboxTownBuilder.Steer(transform, to.normalized) * 2.2f;
                else
                {
                    _wanderTimer -= Time.deltaTime;
                    if (_wanderTimer <= 0f)
                    {
                        _wanderTimer = 4f + Random.value * 8f;
                        _target = _anchor + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
                    }
                }
            }
            if (move.sqrMagnitude > 0.01f && !Model.HostileToPlayer)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move.normalized), 6f * Time.deltaTime);
            _cc.Move((move + Vector3.down * 8f) * Time.deltaTime);
        }

        private void OnDamaged(int amount, string source)
        {
            if (!Model.Alive) return;
            Model.Health = _health.Current;
            if (_visual != null) _visual.localScale = new Vector3(0.85f, 0.8f, 0.85f);
            if (source == WorldBible.PlayerId)
            {
                if (_gm.Dialogue != null && _gm.Dialogue.Current == Model) _gm.EndConversation();
                _gm.OnNpcAttacked(this, source);
                if (!Model.HostileToPlayer) _fleeTimer = 6f;
                RefreshFromModel();
            }
        }

        private void OnDied(string source)
        {
            if (!Model.Alive) return;
            _gm.OnNpcKilled(this, source);
            RefreshFromModel();
        }

        private void LayDown()
        {
            _deadView = true;
            if (_visual != null) _visual.localRotation = Quaternion.Euler(90, 0, 0);
            if (_visual != null) _visual.localPosition = new Vector3(0, 0.35f, 0);
            _cc.enabled = false;
            var col = gameObject.AddComponent<CapsuleCollider>(); col.height = 1.8f; col.radius = 0.35f; col.center = new Vector3(0, 0.35f, 0); col.direction = 2;
        }
    }
}
