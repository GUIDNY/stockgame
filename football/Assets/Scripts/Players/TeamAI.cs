using System.Collections.Generic;
using UnityEngine;
using StrikerFive.Core;

namespace StrikerFive.Players
{
    /// <summary>
    /// Team brain: one chaser goes for the ball, the rest hold a sliding formation, the ball carrier decides
    /// between shooting, passing and dribbling, and the keeper guards the line. Skips the human-controlled player.
    /// </summary>
    public class TeamAI
    {
        private readonly TeamRuntime _team;
        private readonly MatchManager _match;
        private readonly Dictionary<PlayerAgent, float> _decision = new Dictionary<PlayerAgent, float>();
        private readonly Dictionary<PlayerAgent, Vector3> _dribbleTarget = new Dictionary<PlayerAgent, Vector3>();
        private float _keeperHold;

        public TeamAI(TeamRuntime team, MatchManager match) { _team = team; _match = match; }

        public void Update(float dt)
        {
            var ball = _match.Ball;
            var owner = ball.Owner;
            bool inPossession = owner != null && owner.Team == _team;
            Vector3 predicted = ball.Predict(0.35f);

            PlayerAgent chaser = _team.IsHuman ? _team.Controlled : null;
            if (!inPossession && chaser == null)
                chaser = _team.ClosestTo(predicted, PitchGeometry.InBox(ball.Position.x, ball.Position.z, -_team.Side));
            if (owner != null && owner.Team == _team) chaser = owner;

            foreach (var p in _team.Players)
            {
                if (_team.IsHuman && p == _team.Controlled) continue;
                if (p.Role == Role.Goalkeeper) { Keeper(p, dt); continue; }
                if (p == owner) { OnBall(p, dt); continue; }
                if (p == chaser) { Chase(p, predicted, owner); continue; }
                OffBall(p, inPossession);
            }
        }

        private void Chase(PlayerAgent p, Vector3 predicted, PlayerAgent owner)
        {
            var ball = _match.Ball;
            float dist = Vector3.Distance(p.Position, ball.Position);
            p.MoveTowards(predicted, dist > 4f, 0.2f);
            if (owner != null && owner.Team != _team && dist < 1.9f && p.CanTackle && Random.value < MatchSettings.AiReaction * 2f * Time.deltaTime * 10f)
                p.Tackle(ball.Position - p.Position);
        }

        private void OffBall(PlayerAgent p, bool inPossession)
        {
            var ball = _match.Ball.Position;
            if (_team.Goalkeeper == null && !inPossession)
            {
                // No keeper: stand between the ball and our goal.
                Vector3 own = _team.OwnGoal;
                Vector3 guard = Vector3.Lerp(own, ball, 0.35f);
                guard.x = Mathf.Clamp(guard.x * _team.Side, -PitchGeometry.HalfLength + 1.5f, 0f) * _team.Side;
                p.MoveTowards(guard, Vector3.Distance(p.Position, guard) > 6f, 0.4f);
                p.Face(ball - p.Position);
                return;
            }
            var (hx, hz) = Formation.Home(p.Role, _team.Side, ball.x, ball.z, inPossession);
            var home = new Vector3(hx, 0f, hz);
            // Light attraction to the ball keeps play compact.
            home = Vector3.Lerp(home, ball, inPossession ? 0.08f : 0.15f);
            foreach (var mate in _team.Players)
            {
                if (mate == p) continue;
                Vector3 away = p.Position - mate.Position; away.y = 0f;
                if (away.magnitude < 3f) home += away.normalized * (3f - away.magnitude);
            }
            float dist = Vector3.Distance(p.Position, home);
            p.MoveTowards(home, dist > 9f, 0.6f);
            if (dist < 0.8f) p.Face(ball - p.Position);
        }

        private void OnBall(PlayerAgent p, float dt)
        {
            _decision.TryGetValue(p, out float timer);
            timer -= dt;
            if (timer > 0f)
            {
                _decision[p] = timer;
                if (_dribbleTarget.TryGetValue(p, out var t)) p.MoveTowards(t, NearestOpponentDistance(p.Position) > 5f, 0.5f);
                return;
            }
            _decision[p] = MatchSettings.AiReaction + Random.value * 0.3f;

            Vector3 goal = _team.AttackGoal;
            Vector3 toGoal = goal - p.Position; toGoal.y = 0f;
            float distGoal = toGoal.magnitude;
            float pressure = NearestOpponentDistance(p.Position);

            // Shoot when close and the lane is open.
            if (distGoal < 21f * PitchGeometry.Scale && LaneClear(p.Position, goal, 2.2f))
            {
                Vector3 aim = goal + new Vector3(0f, 0f, Random.Range(-2.4f, 2.4f)) - p.Position;
                p.Face(aim);
                _match.Ball.Kick(p, aim, 22f + Random.value * 5f, 1.2f + Random.value * 1.5f);
                return;
            }

            // Best pass.
            PlayerAgent best = null; float bestScore = -100f;
            foreach (var mate in _team.Players)
            {
                if (mate == p || mate.Role == Role.Goalkeeper) continue;
                Vector3 to = mate.Position - p.Position; to.y = 0f;
                float dist = to.magnitude;
                if (dist < 3f || dist > 32f * PitchGeometry.Scale) continue;
                float forward = (mate.Position.x - p.Position.x) * _team.Side;
                float open = NearestOpponentDistance(mate.Position);
                float score = forward * 0.5f + Mathf.Min(open, 8f) * 1.2f + (LaneClear(p.Position, mate.Position, 1.6f) ? 4f : -8f) - dist * 0.1f;
                if (score > bestScore) { bestScore = score; best = mate; }
            }
            bool wantPass = best != null && bestScore > 5f && (pressure < 3.2f || Random.value < 0.3f || distGoal > 30f * PitchGeometry.Scale);
            if (wantPass)
            {
                Vector3 target = best.Position + best.Velocity * 0.4f;
                Vector3 dir = target - p.Position;
                float power = Mathf.Clamp(dir.magnitude * 0.85f + 6f, 10f, 22f);
                p.Face(dir);
                _match.Ball.Kick(p, dir, power, 0.4f);
                return;
            }

            // Dribble towards goal, stepping around the nearest opponent.
            Vector3 target2 = goal;
            var opp = NearestOpponent(p.Position);
            if (opp != null)
            {
                Vector3 toOpp = opp.Position - p.Position; toOpp.y = 0f;
                if (toOpp.magnitude < 5f && Vector3.Dot(toOpp.normalized, toGoal.normalized) > 0.3f)
                {
                    Vector3 side = Vector3.Cross(Vector3.up, toGoal.normalized);
                    float sign = Vector3.Dot(toOpp, side) > 0f ? -1f : 1f;
                    target2 = p.Position + toGoal.normalized * 6f + side * sign * 6f;
                }
            }
            target2.x = PitchGeometry.ClampX(target2.x, 1f); target2.z = PitchGeometry.ClampZ(target2.z, 1f);
            _dribbleTarget[p] = target2;
            p.MoveTowards(target2, pressure > 5f, 0.5f);
        }

        private void Keeper(PlayerAgent gk, float dt)
        {
            var ball = _match.Ball;
            Vector3 own = _team.OwnGoal;
            int goalSide = -_team.Side;
            if (ball.Owner == gk)
            {
                _keeperHold += dt;
                Vector3 clear = new Vector3(_team.Side, 0f, Random.Range(-0.6f, 0.6f));
                gk.Face(clear);
                if (_keeperHold > 0.6f) { _keeperHold = 0f; ball.Kick(gk, clear, 24f, 7f); }
                return;
            }
            _keeperHold = 0f;
            bool threat = PitchGeometry.InBox(ball.Position.x, ball.Position.z, goalSide) || (ball.Position - own).magnitude < 12f;
            Vector3 vel = ball.Velocity; vel.y = 0f;
            bool incoming = Vector3.Dot(vel, own - ball.Position) > 0f && vel.magnitude > 3f;
            if (threat && (incoming || (ball.Position - own).magnitude < 7f) && ball.Owner == null)
            {
                Vector3 target = ball.Predict(0.25f);
                target.x = Mathf.Clamp(target.x * _team.Side, -PitchGeometry.HalfLength, -PitchGeometry.HalfLength + PitchGeometry.BoxLength) * _team.Side;
                target.z = Mathf.Clamp(target.z, -PitchGeometry.BoxHalfWidth, PitchGeometry.BoxHalfWidth);
                gk.MoveTowards(target, true, 0.2f);
                return;
            }
            float z = Mathf.Clamp(ball.Position.z * 0.5f, -2.6f, 2.6f);
            Vector3 line = new Vector3(own.x + _team.Side * 1.3f, 0f, z);
            gk.MoveTowards(line, false, 0.25f);
            gk.Face(ball.Position - gk.Position);
        }

        private float NearestOpponentDistance(Vector3 p)
        {
            var o = NearestOpponent(p);
            return o == null ? 99f : Vector3.Distance(o.Position, p);
        }

        private PlayerAgent NearestOpponent(Vector3 p) => _match.Opponent(_team).ClosestTo(p, true);

        private bool LaneClear(Vector3 from, Vector3 to, float radius)
        {
            Vector3 seg = to - from; seg.y = 0f;
            float len = seg.magnitude;
            if (len < 0.01f) return true;
            Vector3 dir = seg / len;
            foreach (var opp in _match.Opponent(_team).Players)
            {
                Vector3 rel = opp.Position - from; rel.y = 0f;
                float along = Vector3.Dot(rel, dir);
                if (along < 0.5f || along > len) continue;
                float side = (rel - dir * along).magnitude;
                if (side < radius) return false;
            }
            return true;
        }
    }
}
