using UnityEngine;

namespace Echobound.Player
{
    /// <summary>Camera-relative WASD movement with run and gravity on a CharacterController. Placeholder capsule visual.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
    {
        public float WalkSpeed = 4f;
        public float RunSpeed = 7.5f;
        public float Gravity = -22f;
        public float TurnSpeed = 14f;
        public bool InputEnabled;
        public Transform CameraTransform;
        public bool IsRunning { get; private set; }
        public Vector3 PlanarVelocity { get; private set; }

        private CharacterController _cc;
        private float _verticalVelocity;

        public static ThirdPersonController Create(Vector3 position, Material bodyMaterial)
        {
            var go = new GameObject("Player");
            go.transform.position = position;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0); cc.slopeLimit = 50f; cc.stepOffset = 0.4f;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            Object.Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0, 0.9f, 0);
            visual.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            visual.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Facing";
            Object.Destroy(nose.GetComponent<Collider>());
            nose.transform.SetParent(go.transform, false);
            nose.transform.localPosition = new Vector3(0, 1.4f, 0.4f);
            nose.transform.localScale = new Vector3(0.2f, 0.2f, 0.3f);
            nose.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
            var ctrl = go.AddComponent<ThirdPersonController>();
            go.AddComponent<PlayerInteractor>();
            go.AddComponent<PlayerCombat>();
            return ctrl;
        }

        private void Awake() { _cc = GetComponent<CharacterController>(); }

        private void Update()
        {
            Vector3 move = Vector3.zero;
            IsRunning = false;
            if (InputEnabled)
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                Vector3 forward = CameraTransform != null ? Vector3.ProjectOnPlane(CameraTransform.forward, Vector3.up).normalized : Vector3.forward;
                Vector3 right = CameraTransform != null ? Vector3.ProjectOnPlane(CameraTransform.right, Vector3.up).normalized : Vector3.right;
                move = (forward * v + right * h);
                if (move.sqrMagnitude > 1f) move.Normalize();
                IsRunning = Input.GetKey(KeyCode.LeftShift) && move.sqrMagnitude > 0.01f;
            }
            float speed = IsRunning ? RunSpeed : WalkSpeed;
            if (_cc.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += Gravity * Time.deltaTime;
            Vector3 velocity = move * speed + Vector3.up * _verticalVelocity;
            _cc.Move(velocity * Time.deltaTime);
            PlanarVelocity = move * speed;
            if (move.sqrMagnitude > 0.001f)
            {
                var targetRot = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, TurnSpeed * Time.deltaTime);
            }
        }

        public void Teleport(Vector3 position, float yaw = float.NaN)
        {
            _cc.enabled = false;
            transform.position = position;
            if (!float.IsNaN(yaw)) transform.rotation = Quaternion.Euler(0, yaw, 0);
            _cc.enabled = true;
            _verticalVelocity = 0f;
        }
    }
}
