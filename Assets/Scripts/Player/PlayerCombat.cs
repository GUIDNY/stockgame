using UnityEngine;
using Echobound.Combat;
using Echobound.Core;

namespace Echobound.Player
{
    /// <summary>Basic melee: left click swings at anything with a Health component in front of the player.</summary>
    public class PlayerCombat : MonoBehaviour
    {
        public int Damage = 20;
        public float Cooldown = 0.55f;
        public float Range = 1.6f;
        private float _timer;
        private readonly Collider[] _hits = new Collider[16];
        private Transform _visual;

        private void Start() { _visual = transform.Find("Visual"); }

        private void Update()
        {
            _timer -= Time.deltaTime;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            if (_timer <= 0f && Input.GetMouseButtonDown(0)) Swing();
            if (_visual != null) _visual.localScale = Vector3.Lerp(_visual.localScale, new Vector3(0.7f, 0.9f, 0.7f), 10f * Time.deltaTime);
        }

        private void Swing()
        {
            _timer = Cooldown;
            if (_visual != null) _visual.localScale = new Vector3(0.85f, 0.8f, 0.85f);
            var origin = transform.position + Vector3.up * 0.9f + transform.forward * Range * 0.6f;
            int n = Physics.OverlapSphereNonAlloc(origin, Range * 0.7f, _hits, ~0, QueryTriggerInteraction.Ignore);
            var hitSet = new System.Collections.Generic.HashSet<Health>();
            for (int i = 0; i < n; i++)
            {
                var h = _hits[i] != null ? _hits[i].GetComponentInParent<Health>() : null;
                if (h == null || h.transform == transform || hitSet.Contains(h)) continue;
                hitSet.Add(h);
                h.TakeDamage(Damage, World.WorldBible.PlayerId);
            }
        }

        /// <summary>Damage to the player is routed through the game manager so PlayerState stays the source of truth.</summary>
        public void TakeDamage(int amount, string source) => GameManager.Instance?.DamagePlayer(amount, source);
    }
}
