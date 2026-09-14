using UnityEngine;
using StrikerFive.Core;
using StrikerFive.Players;

namespace StrikerFive.Ball
{
    /// <summary>Physical ball. While owned it is steered ahead of the owner's feet; kicks release it.</summary>
    public class BallController : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public PlayerAgent Owner;
        public PlayerAgent LastTouch;
        public float KickCooldownUntil;
        public Vector3 Position => transform.position;
        public Vector3 Velocity => Body.velocity;

        public static BallController Create(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Ball";
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * PitchGeometry.BallRadius * 2f;
            go.GetComponent<Renderer>().sharedMaterial = Materials.Textured(Materials.Checker(Color.white, new Color(0.12f, 0.12f, 0.12f), 6), new Vector2(2f, 1f), 0.6f);
            var col = go.GetComponent<SphereCollider>();
            col.material = new PhysicMaterial("Ball") { bounciness = 0.55f, dynamicFriction = 0.45f, staticFriction = 0.45f, bounceCombine = PhysicMaterialCombine.Maximum, frictionCombine = PhysicMaterialCombine.Average };
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.43f; rb.drag = 0.25f; rb.angularDrag = 0.6f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var b = go.AddComponent<BallController>();
            b.Body = rb;
            return b;
        }

        private void FixedUpdate()
        {
            if (Owner == null) return;
            float ahead = Owner.Sprinting ? 1.25f : 0.8f;
            Vector3 target = Owner.Position + Owner.Facing * ahead + Vector3.up * PitchGeometry.BallRadius;
            Vector3 vel = (target - transform.position) * 10f + Owner.Velocity;
            vel = Vector3.ClampMagnitude(vel, 14f);
            vel.y = Mathf.Min(Body.velocity.y, 0f);
            Body.velocity = vel;
            Body.angularVelocity = Vector3.Cross(Vector3.up, vel) / PitchGeometry.BallRadius;
        }

        public void Kick(PlayerAgent kicker, Vector3 direction, float power, float lift)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) direction = kicker.Facing;
            Owner = null;
            LastTouch = kicker;
            KickCooldownUntil = Time.time + 0.35f;
            Body.velocity = direction.normalized * power + Vector3.up * lift;
            Body.angularVelocity = Vector3.Cross(Vector3.up, direction.normalized) * power * 2f;
            MatchManager.Instance?.Audio?.Kick(power);
        }

        public void PlaceAt(Vector3 position)
        {
            Owner = null;
            Body.velocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            transform.position = position + Vector3.up * PitchGeometry.BallRadius;
        }

        public Vector3 Predict(float seconds)
        {
            var v = Body.velocity; v.y = 0f;
            return transform.position + v * seconds;
        }
    }
}
