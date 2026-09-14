using UnityEngine;
using StrikerFive.Core;

namespace StrikerFive.Players
{
    /// <summary>Keyboard control of the human team's selected player: move, sprint, pass, shoot (hold), lob, tackle, switch.</summary>
    public class HumanController
    {
        private readonly TeamRuntime _team;
        private readonly MatchManager _match;
        private float _shootCharge = -1f;
        private float _switchTimer;
        public float ShootCharge01 => _shootCharge < 0f ? 0f : Mathf.Clamp01(_shootCharge / 0.9f);

        public HumanController(TeamRuntime team, MatchManager match) { _team = team; _match = match; }

        public void Update(float dt)
        {
            var ball = _match.Ball;
            _switchTimer -= dt;
            UpdateSelection(ball);
            var p = _team.Controlled;
            if (p == null) return;

            Vector3 dir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            bool sprint = Input.GetKey(KeyCode.Space);
            p.MoveDirection(dir, sprint);
            Vector3 aim = dir.sqrMagnitude > 0.1f ? dir.normalized : p.Facing;

            if (p.HasBall)
            {
                if (Input.GetKeyDown(KeyCode.J)) Pass(p, aim);
                if (Input.GetKeyDown(KeyCode.L)) { p.Face(aim); ball.Kick(p, aim, 15f, 7.5f); }
                if (Input.GetKey(KeyCode.K)) _shootCharge = _shootCharge < 0f ? 0f : _shootCharge + dt;
                if (Input.GetKeyUp(KeyCode.K) && _shootCharge >= 0f) { Shoot(p, aim, ShootCharge01); _shootCharge = -1f; }
            }
            else
            {
                _shootCharge = -1f;
                if ((Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K)) && p.CanTackle)
                {
                    Vector3 toBall = ball.Position - p.Position; toBall.y = 0f;
                    if (toBall.magnitude < 4f) p.Tackle(toBall);
                }
                if (Input.GetKeyDown(KeyCode.Q)) SwitchNext(ball);
            }
        }

        private void UpdateSelection(Ball.BallController ball)
        {
            if (ball.Owner != null && ball.Owner.Team == _team) { Select(ball.Owner); return; }
            if (ball.Owner != null && ball.Owner.Team == _team.Goalkeeper?.Team && ball.Owner.Role == Role.Goalkeeper) return;
            var candidate = _team.ClosestTo(ball.Predict(0.35f), false);
            if (candidate == null) return;
            if (_team.Controlled == null) { Select(candidate); return; }
            if (candidate != _team.Controlled && _switchTimer <= 0f)
            {
                float dCur = Vector3.Distance(_team.Controlled.Position, ball.Position);
                float dNew = Vector3.Distance(candidate.Position, ball.Position);
                if (dNew + 2.5f < dCur || _team.Controlled.Role == Role.Goalkeeper) { Select(candidate); _switchTimer = 0.6f; }
            }
        }

        private void SwitchNext(Ball.BallController ball)
        {
            var next = _team.ClosestTo(ball.Position, false, _team.Controlled);
            if (next != null) { Select(next); _switchTimer = 0.8f; }
        }

        private void Select(PlayerAgent p)
        {
            if (_team.Controlled == p) return;
            _team.Controlled?.SetControlled(false);
            _team.Controlled = p;
            p.SetControlled(true);
        }

        private void Pass(PlayerAgent p, Vector3 aim)
        {
            PlayerAgent best = null; float bestScore = -1f;
            foreach (var mate in _team.Players)
            {
                if (mate == p) continue;
                Vector3 to = mate.Position - p.Position; to.y = 0f;
                float dist = to.magnitude;
                if (dist < 1.5f || dist > 34f) continue;
                float score = Vector3.Dot(to.normalized, aim) - dist * 0.01f;
                if (score > bestScore) { bestScore = score; best = mate; }
            }
            Vector3 dir = best != null && bestScore > 0.2f ? (best.Position + best.Velocity * 0.35f - p.Position) : aim * 12f;
            float power = Mathf.Clamp(dir.magnitude * 0.85f + 6f, 10f, 24f);
            p.Face(dir);
            _match.Ball.Kick(p, dir, power, 0.4f);
        }

        private void Shoot(PlayerAgent p, Vector3 aim, float charge)
        {
            Vector3 goal = _team.AttackGoal;
            Vector3 toGoal = goal - p.Position; toGoal.y = 0f;
            // Aim: mostly at goal, steered across the face by the stick.
            float lateral = Vector3.Dot(aim, Vector3.Cross(Vector3.up, toGoal.normalized));
            Vector3 target = goal + Vector3.Cross(Vector3.up, toGoal.normalized) * (lateral * 2.8f);
            Vector3 dir = toGoal.magnitude < 45f ? (target - p.Position) : aim;
            float power = Mathf.Lerp(14f, 31f, charge);
            float lift = Mathf.Lerp(0.8f, 4.5f, charge) * (toGoal.magnitude > 14f ? 1f : 0.5f);
            p.Face(dir);
            _match.Ball.Kick(p, dir, power, lift, -lateral * charge);
        }
    }
}
