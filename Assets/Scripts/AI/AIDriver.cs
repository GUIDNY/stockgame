using UnityEngine;
using TurboLoop.Track;
using TurboLoop.Vehicles;

namespace TurboLoop.AI
{
    /// <summary>
    /// Waypoint-following opponent: looks ahead along the centreline, brakes for corners based on curvature,
    /// keeps a personal lane, avoids cars directly ahead and recovers when stuck.
    /// </summary>
    public class AIDriver : MonoBehaviour, ICarInput
    {
        public float Throttle { get; private set; }
        public float Steer { get; private set; }
        public bool Handbrake { get; private set; }

        private TrackSpline _spline;
        private CarController _car;
        private float _skill = 1f;        // 0.85 .. 1.0 scales top speed and braking confidence
        private float _lane;              // metres left (+) / right (-) of centre
        private float _stuckTimer;
        private float _reverseTimer;
        private float _laneDriftTimer;
        private float _laneTarget;

        public void Init(TrackSpline spline, CarController car, float skill, float lane)
        {
            _spline = spline; _car = car; _skill = skill; _lane = lane; _laneTarget = lane;
        }

        private void Update()
        {
            if (_spline == null || _car == null) return;
            float dt = Time.deltaTime;
            float speed = Mathf.Abs(_car.ForwardSpeed);

            // Occasionally change lane a little so the pack does not drive in a train.
            _laneDriftTimer -= dt;
            if (_laneDriftTimer <= 0f) { _laneDriftTimer = Random.Range(4f, 9f); _laneTarget = Random.Range(-1f, 1f) * (_spline != null ? 3.2f : 0f); }
            _lane = Mathf.MoveTowards(_lane, _laneTarget, 0.8f * dt);

            // Stuck recovery: reverse for a moment with inverted steering.
            if (_car.ControlsEnabled && speed < 1.2f) _stuckTimer += dt; else _stuckTimer = 0f;
            if (_stuckTimer > 2.2f) { _reverseTimer = 1.4f; _stuckTimer = 0f; }
            if (_reverseTimer > 0f)
            {
                _reverseTimer -= dt;
                Throttle = -1f;
                Steer = -Mathf.Sign(SteerTowards(LookTarget(4)));
                Handbrake = false;
                return;
            }

            int lookAhead = Mathf.Clamp(Mathf.RoundToInt((7f + speed * 0.5f) / 3f), 3, 14);
            Vector3 target = LookTarget(lookAhead);
            float steer = SteerTowards(target);

            // Corner speed from curvature ahead: v = sqrt(r * g * mu).
            float curvature = _spline.MaxCurvatureAhead(_car.TrackIndex, 18f + speed * 1.1f);
            float radius = 1f / Mathf.Max(0.004f, curvature);
            float cornerSpeed = Mathf.Sqrt(radius * 9.81f * 1.05f) * _skill;
            float desired = Mathf.Min(_car.MaxSpeed * _skill, cornerSpeed);
            desired *= Mathf.Lerp(1f, 0.75f, Mathf.Abs(steer));

            float throttle;
            if (speed < desired - 1.5f) throttle = 1f;
            else if (speed > desired + 3f) throttle = -0.7f;
            else throttle = 0.25f;

            // Avoid the car directly ahead.
            var origin = transform.position + Vector3.up * 0.6f + transform.forward * 2.4f;
            if (Physics.SphereCast(origin, 1.0f, transform.forward, out var hit, 9f, ~0, QueryTriggerInteraction.Ignore))
            {
                var other = hit.collider.GetComponentInParent<CarController>();
                if (other != null && other != _car)
                {
                    float side = Vector3.Dot(hit.point - transform.position, transform.right);
                    steer += side > 0f ? -0.5f : 0.5f;
                    if (other.ForwardSpeed < _car.ForwardSpeed) throttle = Mathf.Min(throttle, 0.55f);
                }
            }

            Throttle = throttle;
            Steer = Mathf.Clamp(steer, -1f, 1f);
            Handbrake = false;
        }

        private Vector3 LookTarget(int samplesAhead)
        {
            int i = _spline.Wrap(_car.TrackIndex + samplesAhead);
            var p = _spline.Points[i];
            var n = _spline.LeftNormal(i);
            return new Vector3(p.X + n.X * _lane, 0f, p.Z + n.Z * _lane);
        }

        private float SteerTowards(Vector3 target)
        {
            Vector3 to = target - transform.position; to.y = 0f;
            float angle = Vector3.SignedAngle(transform.forward, to, Vector3.up);
            return Mathf.Clamp(angle / 32f, -1f, 1f);
        }
    }
}
