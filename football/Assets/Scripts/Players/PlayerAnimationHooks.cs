using UnityEngine;

namespace StrikerFive.Players
{
    /// <summary>
    /// Lives on the animated model next to its Animator. Provides a procedural kick: during a short window the
    /// kicking foot is driven by IK in an arc towards the ball, so no kick animation clip is required.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationHooks : MonoBehaviour
    {
        private Animator _animator;
        private float _kickTimer = -1f;
        private Vector3 _kickDir = Vector3.forward;
        private const float KickDuration = 0.32f;

        private void Awake() { _animator = GetComponent<Animator>(); }

        public void StartKick(Vector3 worldDirection)
        {
            _kickDir = worldDirection.sqrMagnitude > 0.01f ? worldDirection.normalized : transform.forward;
            _kickTimer = 0f;
        }

        private void Update()
        {
            if (_kickTimer >= 0f) { _kickTimer += Time.deltaTime; if (_kickTimer > KickDuration) _kickTimer = -1f; }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || _kickTimer < 0f) return;
            float t = _kickTimer / KickDuration;                       // 0..1 through the kick
            float weight = Mathf.Sin(t * Mathf.PI);                     // ease in and out
            var hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null) return;
            // Back-swing then follow-through: foot travels from behind the body to in front, rising slightly.
            float along = Mathf.Lerp(-0.45f, 0.75f, Mathf.SmoothStep(0f, 1f, t));
            float up = 0.12f + Mathf.Sin(t * Mathf.PI) * 0.35f;
            Vector3 side = Vector3.Cross(Vector3.up, _kickDir) * -0.14f;
            Vector3 target = hips.position + Vector3.down * (hips.position.y - transform.position.y) + _kickDir * along + Vector3.up * up + side;
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, weight);
            _animator.SetIKPosition(AvatarIKGoal.RightFoot, target);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, weight * 0.6f);
            _animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.LookRotation(_kickDir, Vector3.up) * Quaternion.Euler(-35f, 0f, 0f));
        }
    }
}
