using UnityEngine;

namespace StrikerFive.Core
{
    /// <summary>TV-style side camera that tracks the ball along the pitch and pushes in near the goals.</summary>
    public class BroadcastCamera : MonoBehaviour
    {
        public Transform Target;
        public bool MenuMode = true;
        private Camera _cam;
        private Vector3 _vel;
        private float _orbit;

        public static BroadcastCamera Create()
        {
            var existing = Camera.main;
            GameObject go = existing != null ? existing.gameObject : new GameObject("Main Camera");
            if (existing == null) { go.tag = "MainCamera"; go.AddComponent<Camera>(); }
            if (go.GetComponent<AudioListener>() == null) go.AddComponent<AudioListener>();
            var cam = go.GetComponent<Camera>();
            cam.nearClipPlane = 0.3f; cam.farClipPlane = 1200f; cam.fieldOfView = 38f; cam.clearFlags = CameraClearFlags.Skybox;
            var bc = go.AddComponent<BroadcastCamera>();
            bc._cam = cam;
            return bc;
        }

        private void LateUpdate()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (MenuMode || Target == null)
            {
                _orbit += Time.deltaTime * 5f;
                var pos = new Vector3(Mathf.Cos(_orbit * Mathf.Deg2Rad) * 70f, 32f, Mathf.Sin(_orbit * Mathf.Deg2Rad) * 70f);
                transform.position = Vector3.Lerp(transform.position, pos, 2f * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.zero - transform.position), 2f * Time.deltaTime);
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, 45f, Time.deltaTime);
                return;
            }
            Vector3 b = Target.position;
            float x = Mathf.Clamp(b.x * 0.9f, -PitchGeometry.HalfLength + 6f, PitchGeometry.HalfLength - 6f);
            float nearGoal = Mathf.Clamp01((Mathf.Abs(b.x) - 16f) / 16f);
            Vector3 desired = new Vector3(x, 27f - nearGoal * 5f, -46f + nearGoal * 8f + b.z * 0.15f);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _vel, 0.35f);
            Vector3 lookAt = new Vector3(x, 0.5f, b.z * 0.35f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookAt - transform.position, Vector3.up), 6f * Time.deltaTime);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, 36f + nearGoal * 4f, 2f * Time.deltaTime);
        }
    }
}
