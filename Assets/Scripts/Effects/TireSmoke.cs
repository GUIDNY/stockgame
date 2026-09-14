using UnityEngine;
using TurboLoop.Vehicles;

namespace TurboLoop.Effects
{
    /// <summary>Tyre smoke from the rear wheels while drifting. Particle systems are configured entirely in code.</summary>
    public class TireSmoke : MonoBehaviour
    {
        private CarController _car;
        private ParticleSystem[] _systems;

        public static TireSmoke Attach(CarController car)
        {
            var comp = car.gameObject.AddComponent<TireSmoke>();
            comp._car = car;
            comp._systems = new ParticleSystem[2];
            for (int i = 0; i < 2; i++)
            {
                var pivot = car.WheelPivots[2 + i];
                var go = new GameObject("Smoke");
                go.transform.SetParent(pivot != null ? pivot : car.transform, false);
                go.transform.localPosition = new Vector3(0f, -0.25f, -0.2f);
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = 0.9f;
                main.startSpeed = 1.4f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.5f);
                main.startColor = new Color(0.9f, 0.9f, 0.9f, 0.35f);
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 250;
                main.gravityModifier = -0.05f;
                var emission = ps.emission;
                emission.rateOverTime = 0f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.3f;
                var col = ps.colorOverLifetime;
                col.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0.4f, 0f), new GradientAlphaKey(0f, 1f) });
                col.color = gradient;
                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                var shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");
                if (shader != null && renderer != null) renderer.material = new Material(shader);
                comp._systems[i] = ps;
            }
            return comp;
        }

        private void Update()
        {
            if (_car == null || _systems == null) return;
            float rate = _car.IsDrifting ? Mathf.Lerp(20f, 70f, Mathf.Clamp01(Mathf.Abs(_car.LateralSpeed) / 12f)) : 0f;
            foreach (var ps in _systems)
            {
                if (ps == null) continue;
                var emission = ps.emission;
                emission.rateOverTime = rate;
            }
        }
    }
}
