using UnityEngine;
using TurboLoop.Vehicles;

namespace TurboLoop.Core
{
    /// <summary>Smooth chase camera with speed-dependent distance and field of view, plus a slow orbit for the menu.</summary>
    public class ChaseCamera : MonoBehaviour
    {
        public CarController Target;
        public Vector3 OrbitCenter;
        public bool MenuMode = true;
        public float Distance = 7.5f;
        public float Height = 3.0f;
        public float LookAhead = 6f;

        private Camera _cam;
        private Vector3 _velocity;
        private float _orbit;

        public static ChaseCamera Create()
        {
            var existing = Camera.main;
            GameObject go = existing != null ? existing.gameObject : new GameObject("Main Camera");
            if (existing == null)
            {
                go.tag = "MainCamera";
                go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            if (go.GetComponent<AudioListener>() == null) go.AddComponent<AudioListener>();
            var cam = go.GetComponent<Camera>();
            cam.nearClipPlane = 0.2f; cam.farClipPlane = 1500f; cam.fieldOfView = 62f;
            cam.clearFlags = CameraClearFlags.Skybox;
            var cc = go.AddComponent<ChaseCamera>();
            cc._cam = cam;
            return cc;
        }

        private void LateUpdate()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (MenuMode || Target == null)
            {
                _orbit += Time.deltaTime * 4f;
                var pos = OrbitCenter + new Vector3(Mathf.Cos(_orbit * Mathf.Deg2Rad) * 170f, 70f, Mathf.Sin(_orbit * Mathf.Deg2Rad) * 170f);
                transform.position = Vector3.Lerp(transform.position, pos, 2f * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(OrbitCenter - transform.position), 2f * Time.deltaTime);
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, 55f, Time.deltaTime);
                return;
            }
            float speed01 = Mathf.Clamp01(Target.SpeedKmh / 200f);
            Vector3 flatForward = Vector3.ProjectOnPlane(Target.transform.forward, Vector3.up).normalized;
            // Blend the car heading with its velocity direction so drifts swing the camera slightly.
            Vector3 vel = Target.Body != null ? Vector3.ProjectOnPlane(Target.Body.velocity, Vector3.up) : Vector3.zero;
            if (vel.sqrMagnitude > 4f) flatForward = Vector3.Slerp(flatForward, vel.normalized, 0.35f).normalized;
            float dist = Distance + speed01 * 3.5f;
            float height = Height + speed01 * 0.8f;
            Vector3 desired = Target.transform.position - flatForward * dist + Vector3.up * height;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, 0.12f);
            Vector3 lookAt = Target.transform.position + Vector3.up * 1.0f + flatForward * LookAhead;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookAt - transform.position, Vector3.up), 10f * Time.deltaTime);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, 60f + speed01 * 18f, 4f * Time.deltaTime);
        }

        public void SnapTo(CarController target)
        {
            Target = target; MenuMode = false;
            Vector3 flatForward = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up).normalized;
            transform.position = target.transform.position - flatForward * Distance + Vector3.up * Height;
            transform.rotation = Quaternion.LookRotation(target.transform.position + Vector3.up - transform.position);
            _velocity = Vector3.zero;
        }
    }
}
