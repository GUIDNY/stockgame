using UnityEngine;

namespace Echobound.Player
{
    /// <summary>Mouse orbit camera with collision pull-in. Input only when the game is in the Playing state.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform Target;
        public float Distance = 5.5f;
        public float MinDistance = 1.2f;
        public float Height = 1.6f;
        public float Sensitivity = 2.2f;
        public float MinPitch = -25f;
        public float MaxPitch = 65f;
        public bool InputEnabled;
        public LayerMask CollisionMask = ~0;

        private float _yaw = 20f;
        private float _pitch = 18f;

        public static ThirdPersonCamera Create(Transform target)
        {
            var existing = Camera.main;
            GameObject go = existing != null ? existing.gameObject : new GameObject("Main Camera");
            if (existing == null)
            {
                go.tag = "MainCamera";
                var cam = go.AddComponent<Camera>();
                cam.nearClipPlane = 0.1f; cam.farClipPlane = 400f; cam.fieldOfView = 60f;
                go.AddComponent<AudioListener>();
            }
            foreach (var old in go.GetComponents<ThirdPersonCamera>()) Destroy(old);
            var tpc = go.AddComponent<ThirdPersonCamera>();
            tpc.Target = target;
            return tpc;
        }

        private void LateUpdate()
        {
            if (Target == null) return;
            if (InputEnabled)
            {
                _yaw += Input.GetAxis("Mouse X") * Sensitivity;
                _pitch -= Input.GetAxis("Mouse Y") * Sensitivity;
                _pitch = Mathf.Clamp(_pitch, MinPitch, MaxPitch);
            }
            var pivot = Target.position + Vector3.up * Height;
            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            var desired = pivot - rot * Vector3.forward * Distance;
            float dist = Distance;
            if (Physics.SphereCast(pivot, 0.25f, (desired - pivot).normalized, out var hit, Distance, CollisionMask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.transform.IsChildOf(Target)) dist = Mathf.Max(MinDistance, hit.distance - 0.1f);
            }
            transform.position = pivot - rot * Vector3.forward * dist;
            transform.rotation = rot;
        }
    }
}
