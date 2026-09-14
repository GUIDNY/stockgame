using UnityEngine;
using TurboLoop.Vehicles;

namespace TurboLoop.Audio
{
    /// <summary>Procedural engine and tyre-squeal audio. Clips are synthesised at startup; no audio assets needed.</summary>
    public class EngineAudio : MonoBehaviour
    {
        private static AudioClip _engineClip, _skidClip;
        private CarController _car;
        private AudioSource _engine, _skid;
        private float _skidVolume;

        public static EngineAudio Attach(CarController car, bool isPlayer)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(car.transform, false);
            var ea = go.AddComponent<EngineAudio>();
            ea._car = car;
            ea._engine = go.AddComponent<AudioSource>();
            ea._engine.clip = EngineClip(); ea._engine.loop = true; ea._engine.playOnAwake = false;
            ea._engine.spatialBlend = isPlayer ? 0.2f : 1f; ea._engine.minDistance = 5f; ea._engine.maxDistance = 90f; ea._engine.rolloffMode = AudioRolloffMode.Linear;
            ea._engine.volume = 0.2f; ea._engine.pitch = 0.6f;
            ea._engine.Play();
            ea._skid = go.AddComponent<AudioSource>();
            ea._skid.clip = SkidClip(); ea._skid.loop = true; ea._skid.playOnAwake = false;
            ea._skid.spatialBlend = isPlayer ? 0.2f : 1f; ea._skid.minDistance = 5f; ea._skid.maxDistance = 60f; ea._skid.rolloffMode = AudioRolloffMode.Linear;
            ea._skid.volume = 0f;
            ea._skid.Play();
            return ea;
        }

        private static AudioClip EngineClip()
        {
            if (_engineClip != null) return _engineClip;
            const int rate = 44100;
            const float baseHz = 55f; // 55 cycles per second: loops seamlessly on a 1 s clip
            var data = new float[rate];
            for (int i = 0; i < rate; i++)
            {
                float t = i / (float)rate;
                float phase = t * baseHz;
                float saw = 2f * (phase - Mathf.Floor(phase + 0.5f));
                float s = 0.45f * saw + 0.35f * Mathf.Sin(phase * Mathf.PI * 2f) + 0.2f * Mathf.Sin(phase * Mathf.PI * 4f) + 0.1f * Mathf.Sin(phase * Mathf.PI * 6f);
                data[i] = Mathf.Clamp(s * 0.6f, -1f, 1f);
            }
            _engineClip = AudioClip.Create("engine", rate, 1, rate, false);
            _engineClip.SetData(data, 0);
            return _engineClip;
        }

        private static AudioClip SkidClip()
        {
            if (_skidClip != null) return _skidClip;
            const int rate = 22050;
            var data = new float[rate];
            var rng = new System.Random(3);
            float low = 0f;
            for (int i = 0; i < rate; i++)
            {
                float white = (float)(rng.NextDouble() * 2 - 1);
                low = Mathf.Lerp(low, white, 0.12f);        // crude low-pass
                float t = i / (float)rate;
                data[i] = Mathf.Clamp(low * 1.6f + 0.25f * Mathf.Sin(t * 880f * Mathf.PI * 2f), -1f, 1f) * 0.8f;
            }
            _skidClip = AudioClip.Create("skid", rate, 1, rate, false);
            _skidClip.SetData(data, 0);
            return _skidClip;
        }

        private void Update()
        {
            if (_car == null) return;
            float dt = Time.deltaTime;
            _engine.pitch = Mathf.Lerp(_engine.pitch, 0.55f + _car.Rpm01 * 1.75f, 8f * dt);
            _engine.volume = Mathf.Lerp(_engine.volume, 0.16f + 0.22f * _car.Throttle01 + 0.08f * _car.Rpm01, 6f * dt);
            float target = _car.IsDrifting ? Mathf.Clamp01(Mathf.Abs(_car.LateralSpeed) / 12f) * 0.55f : 0f;
            _skidVolume = Mathf.Lerp(_skidVolume, target, 10f * dt);
            _skid.volume = _skidVolume;
            _skid.pitch = 0.9f + Mathf.Clamp01(_car.SpeedKmh / 200f) * 0.3f;
        }
    }
}
