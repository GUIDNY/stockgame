using UnityEngine;
using StrikerFive.Core;

namespace StrikerFive.Players
{
    /// <summary>One footballer: movement on a CharacterController, facing, sprint, tackle and the visual body.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerAgent : MonoBehaviour
    {
        public TeamRuntime Team;
        public Role Role;
        public string PlayerName = "";
        public int Number;
        public float BaseSpeed = 7.2f;
        public float SprintSpeed = 9.6f;
        public float SpeedScale = 1f;

        public Vector3 Facing = Vector3.forward;
        public Vector3 Velocity { get; private set; }
        public bool Sprinting { get; private set; }
        public bool IsTackling => _tackleTimer > 0f;
        public bool Stumbling => _stumbleTimer > 0f;
        public bool IsControlled { get; private set; }
        public Vector3 Position => transform.position;
        public Transform Ring;
        public TextMesh Label;

        private CharacterController _cc;
        private Vector3 _moveDir;
        private float _tackleTimer, _stumbleTimer, _tackleCooldown;
        private Vector3 _tackleDir;
        private Transform _body;
        private float _bob;

        private void Awake() { _cc = GetComponent<CharacterController>(); }

        public bool HasBall => MatchManager.Instance != null && MatchManager.Instance.Ball.Owner == this;
        public bool CanTackle => _tackleCooldown <= 0f && !IsTackling && !Stumbling;

        /// <summary>Set this frame's desired movement direction (world space, y ignored).</summary>
        public void MoveDirection(Vector3 dir, bool sprint)
        {
            dir.y = 0f;
            _moveDir = dir.sqrMagnitude > 1f ? dir.normalized : dir;
            Sprinting = sprint && _moveDir.sqrMagnitude > 0.1f;
        }

        public void MoveTowards(Vector3 target, bool sprint, float stopDistance = 0.3f)
        {
            Vector3 to = target - transform.position; to.y = 0f;
            if (to.magnitude <= stopDistance) { MoveDirection(Vector3.zero, false); return; }
            MoveDirection(to.normalized, sprint);
        }

        public void Face(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) Facing = dir.normalized;
        }

        public void Tackle(Vector3 dir)
        {
            if (!CanTackle) return;
            dir.y = 0f;
            _tackleDir = dir.sqrMagnitude > 0.01f ? dir.normalized : Facing;
            _tackleTimer = 0.45f;
            _tackleCooldown = 1.1f;
        }

        public void Stumble(float seconds) { _stumbleTimer = Mathf.Max(_stumbleTimer, seconds); }

        public void SetControlled(bool on)
        {
            IsControlled = on;
            if (Ring != null) Ring.gameObject.SetActive(on);
            if (Label != null) Label.gameObject.SetActive(on);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _tackleCooldown -= dt;
            Vector3 vel;
            if (_tackleTimer > 0f)
            {
                _tackleTimer -= dt;
                vel = _tackleDir * 11f;
                if (_tackleTimer <= 0f) _stumbleTimer = 0.35f;
            }
            else if (_stumbleTimer > 0f)
            {
                _stumbleTimer -= dt;
                vel = _moveDir * BaseSpeed * 0.3f;
            }
            else
            {
                float speed = (Sprinting ? SprintSpeed : BaseSpeed) * SpeedScale;
                if (HasBall) speed *= Sprinting ? 0.9f : 0.88f;
                vel = _moveDir * speed;
            }
            Velocity = vel;
            _cc.Move((vel + Vector3.down * 9.81f) * dt);
            // Keep players on the pitch surround.
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, -PitchGeometry.HalfLength - 3f, PitchGeometry.HalfLength + 3f);
            p.z = Mathf.Clamp(p.z, -PitchGeometry.HalfWidth - 3f, PitchGeometry.HalfWidth + 3f);
            if (p.y < 0f) p.y = 0f;
            transform.position = p;

            if (_moveDir.sqrMagnitude > 0.01f && !HasBall) Face(_moveDir);
            else if (_moveDir.sqrMagnitude > 0.01f) Facing = Vector3.Slerp(Facing, _moveDir.normalized, 10f * dt).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Facing, Vector3.up), 14f * dt);

            if (_body != null)
            {
                float moving = Mathf.Clamp01(vel.magnitude / SprintSpeed);
                _bob += dt * (6f + moving * 10f);
                float lean = IsTackling ? 55f : moving * 8f;
                _body.localRotation = Quaternion.Euler(lean, 0f, Mathf.Sin(_bob) * moving * 3f);
                _body.localPosition = new Vector3(0f, IsTackling ? -0.5f : Mathf.Abs(Mathf.Sin(_bob)) * moving * 0.06f, 0f);
            }
        }

        public void AttachBody(Transform body) { _body = body; }
    }
}
