using UnityEngine;
using TurboLoop.Track;

namespace TurboLoop.Vehicles
{
    /// <summary>
    /// Arcade car physics on a Rigidbody: engine/brake forces, speed-sensitive steering as yaw rate,
    /// lateral grip with handbrake drifts, downforce and a speed cap. No wheel colliders: predictable and fun.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [Header("Identity")]
        public string DriverName = "Driver";
        public bool IsPlayer;
        public Color PaintColor = Color.red;

        [Header("Tuning")]
        public float MaxSpeed = 56f;          // m/s (~200 km/h)
        public float EngineAccel = 26f;
        public float BrakeAccel = 36f;
        public float ReverseAccel = 10f;
        public float Grip = 9.5f;
        public float DriftGrip = 2.3f;
        public float MaxSteerRate = 125f;     // deg/s at low speed
        public float Downforce = 0.35f;

        [Header("Runtime")]
        public ICarInput Input;
        public bool ControlsEnabled;
        public float SpeedKmh;
        public float ForwardSpeed;
        public float LateralSpeed;
        public bool Grounded;
        public bool IsDrifting;
        public float Throttle01;
        public float Rpm01 = 0.3f;
        public int TrackIndex;
        public int Rank = 1;
        public LapTracker Lap;

        [Header("Visuals")]
        public Transform BodyVisual;
        public Transform[] WheelPivots = new Transform[4];   // FL, FR, RL, RR
        public Transform[] WheelSpinners = new Transform[4];

        public Rigidbody Body { get; private set; }
        private float _steerSmoothed;
        private float _spin;
        private float _tiltPitch, _tiltRoll;
        private float _lastForwardSpeed;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            float throttle = ControlsEnabled && Input != null ? Mathf.Clamp(Input.Throttle, -1f, 1f) : 0f;
            float steerIn = ControlsEnabled && Input != null ? Mathf.Clamp(Input.Steer, -1f, 1f) : 0f;
            bool handbrake = ControlsEnabled && Input != null && Input.Handbrake;
            _steerSmoothed = Mathf.MoveTowards(_steerSmoothed, steerIn, 5f * dt);

            Vector3 vel = Body.velocity;
            Vector3 fwd = transform.forward;
            Vector3 right = transform.right;
            ForwardSpeed = Vector3.Dot(vel, fwd);
            LateralSpeed = Vector3.Dot(vel, right);
            float speed = vel.magnitude;
            Grounded = Physics.Raycast(transform.position + Vector3.up * 0.6f, Vector3.down, 1.1f, ~0, QueryTriggerInteraction.Ignore);

            if (Grounded)
            {
                if (throttle > 0.01f)
                {
                    float headroom = 1f - Mathf.Clamp01(ForwardSpeed / MaxSpeed);
                    Body.AddForce(fwd * (throttle * EngineAccel * Mathf.Max(0.12f, headroom)), ForceMode.Acceleration);
                }
                else if (throttle < -0.01f)
                {
                    if (ForwardSpeed > 0.5f) Body.AddForce(-fwd * (BrakeAccel * -throttle), ForceMode.Acceleration);
                    else Body.AddForce(fwd * (throttle * ReverseAccel * (1f - Mathf.Clamp01(-ForwardSpeed / (MaxSpeed * 0.3f)))), ForceMode.Acceleration);
                }
                else Body.AddForce(-fwd * (ForwardSpeed * 0.4f), ForceMode.Acceleration); // engine braking
                if (handbrake) Body.AddForce(-fwd * (ForwardSpeed * 1.4f), ForceMode.Acceleration);

                float grip = handbrake ? DriftGrip : Grip;
                float gripScale = Mathf.Lerp(1f, 0.6f, Mathf.Clamp01(speed / MaxSpeed));
                Body.AddForce(-right * (LateralSpeed * grip * gripScale), ForceMode.Acceleration);

                float absFwd = Mathf.Abs(ForwardSpeed);
                float speedFactor = Mathf.Clamp01(absFwd / 6f) * Mathf.Lerp(1f, 0.42f, Mathf.Clamp01(absFwd / MaxSpeed));
                float yawRate = _steerSmoothed * MaxSteerRate * speedFactor * (ForwardSpeed < -0.5f ? -1f : 1f);
                if (handbrake) yawRate *= 1.4f;
                Vector3 av = Body.angularVelocity;
                av.y = Mathf.Lerp(av.y, yawRate * Mathf.Deg2Rad, 14f * dt);
                Body.angularVelocity = av;

                Body.AddForce(Vector3.down * (speed * Downforce), ForceMode.Acceleration);
            }

            float cap = MaxSpeed * 1.08f;
            if (speed > cap) Body.velocity = vel.normalized * cap;

            IsDrifting = Grounded && Mathf.Abs(LateralSpeed) > 4.5f && speed > 8f;
            Throttle01 = Mathf.Clamp01(throttle);
            SpeedKmh = speed * 3.6f;

            // Fake gearbox for the engine sound.
            float norm = Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / MaxSpeed);
            float g = norm * 5f;
            float target = 0.25f + 0.75f * (g - Mathf.Floor(g));
            if (speed < 2f) target = Mathf.Max(0.2f, Throttle01 * 0.6f);
            Rpm01 = Mathf.Lerp(Rpm01, target, 6f * dt);
            _lastForwardSpeed = ForwardSpeed;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _spin += ForwardSpeed / 0.35f * Mathf.Rad2Deg * dt;
            float steerVisual = _steerSmoothed * 28f;
            for (int i = 0; i < 4; i++)
            {
                if (WheelPivots[i] != null && i < 2) WheelPivots[i].localRotation = Quaternion.Euler(0f, steerVisual, 0f);
                if (WheelSpinners[i] != null) WheelSpinners[i].localRotation = Quaternion.Euler(_spin, 0f, 90f);
            }
            if (BodyVisual != null)
            {
                float accel = Body != null ? (ForwardSpeed - _lastForwardSpeed) : 0f;
                _tiltPitch = Mathf.Lerp(_tiltPitch, Mathf.Clamp(-Throttle01 * 1.8f + (Input != null && Input.Throttle < -0.1f && ForwardSpeed > 2f ? 2.5f : 0f), -3f, 3f), 5f * dt);
                _tiltRoll = Mathf.Lerp(_tiltRoll, Mathf.Clamp(LateralSpeed * 0.9f, -7f, 7f), 6f * dt);
                BodyVisual.localRotation = Quaternion.Euler(_tiltPitch, 0f, _tiltRoll);
            }
        }

        public void ResetTo(Vector3 position, Quaternion rotation)
        {
            Body.velocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(position + Vector3.up * 0.4f, rotation);
            _steerSmoothed = 0f;
        }
    }
}
