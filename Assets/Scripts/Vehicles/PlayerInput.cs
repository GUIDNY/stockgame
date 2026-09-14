using UnityEngine;

namespace TurboLoop.Vehicles
{
    /// <summary>Keyboard driving: WASD / arrows, Space handbrake. Steering is smoothed so keyboard taps feel analogue.</summary>
    public class PlayerInput : MonoBehaviour, ICarInput
    {
        public float Throttle { get; private set; }
        public float Steer { get; private set; }
        public bool Handbrake { get; private set; }

        private void Update()
        {
            float t = Input.GetAxisRaw("Vertical");
            float s = Input.GetAxisRaw("Horizontal");
            Throttle = Mathf.MoveTowards(Throttle, t, 6f * Time.deltaTime);
            Steer = Mathf.MoveTowards(Steer, s, (Mathf.Abs(s) > 0.01f ? 5f : 9f) * Time.deltaTime);
            Handbrake = Input.GetKey(KeyCode.Space);
        }
    }
}
