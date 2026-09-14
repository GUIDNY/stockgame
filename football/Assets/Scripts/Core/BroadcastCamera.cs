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
                float r = 70f * PitchGeometry.Scale;
                var pos = new Vector3(Mathf.Cos(_orbit * Mathf.Deg2Rad) * r, 32f * PitchGeometry.Scale, Mathf.Sin(_orbit * Mathf.Deg2Rad) * r);
                transform.position = Vector3.Lerp(transform.position, pos, 2f * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.zero - transform.position), 2f * Time.deltaTime);
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, 45f, Time.deltaTime);
                return;
            }
            Vector3 b = Target.position;
            float s = Mathf.Lerp(0.72f, 1f, (PitchGeometry.Scale - 0.6f) / 0.4f);
            float x = Mathf.Clamp(b.x * 0.95f, -PitchGeometry.HalfLength + 4f * s, PitchGeometry.HalfLength - 4f * s);
            float nearGoal = Mathf.Clamp01((Mathf.Abs(b.x) - PitchGeometry.HalfLength * 0.5f) / (PitchGeometry.HalfLength * 0.5f));
            Vector3 desired = new Vector3(x, (17f - nearGoal * 3f) * s, (-30f + nearGoal * 6f) * s + b.z * 0.25f);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _vel, 0.22f);
            Vector3 lookAt = new Vector3(x, 0.8f, b.z * 0.5f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookAt - transform.position, Vector3.up), 8f * Time.deltaTime);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, 44f + nearGoal * 4f, 2f * Time.deltaTime);
        }
    }
}
