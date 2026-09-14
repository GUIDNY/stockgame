using UnityEngine;

namespace Echobound.Core
{
    /// <summary>Drives the pure WorldClock from real time and renders day/night on the sun and ambient light.</summary>
    public class GameClock : MonoBehaviour
    {
        public float RealSecondsPerGameMinute = 1.5f;
        public bool Running;
        private WorldClock _clock;
        private Light _sun;
        private float _accumulator;
        private static readonly Color DayAmbient = new Color(0.55f, 0.58f, 0.62f);
        private static readonly Color NightAmbient = new Color(0.08f, 0.09f, 0.14f);
        private static readonly Color DaySun = new Color(1f, 0.96f, 0.88f);
        private static readonly Color DuskSun = new Color(1f, 0.6f, 0.35f);

        public void Init(WorldClock clock, Light sun)
        {
            _clock = clock; _sun = sun;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 140f;
            UpdateLighting();
        }

        private void Update()
        {
            if (_clock == null) return;
            if (Running)
            {
                _accumulator += Time.deltaTime;
                while (_accumulator >= RealSecondsPerGameMinute)
                {
                    _accumulator -= RealSecondsPerGameMinute;
                    _clock.Advance(1);
                }
            }
            UpdateLighting();
        }

        public void UpdateLighting()
        {
            if (_clock == null || _sun == null) return;
            float f = _clock.DayFraction;                 // 0 = midnight, 0.5 = noon
            float elevation = Mathf.Sin((f - 0.25f) * Mathf.PI * 2f); // -1 midnight, +1 noon
            _sun.transform.rotation = Quaternion.Euler(elevation * 80f + 10f, f * 360f, 0f);
            float day = Mathf.Clamp01((elevation + 0.3f) * 1.5f); // 18:00 dusk, 21:00 night, 06:00 dawn
            _sun.intensity = Mathf.Lerp(0.05f, 1.1f, day);
            _sun.color = Color.Lerp(DuskSun, DaySun, Mathf.Clamp01(elevation * 3f));
            RenderSettings.ambientLight = Color.Lerp(NightAmbient, DayAmbient, day);
            RenderSettings.fogColor = Color.Lerp(new Color(0.03f, 0.04f, 0.07f), new Color(0.62f, 0.66f, 0.72f), day);
            if (Camera.main != null) Camera.main.backgroundColor = RenderSettings.fogColor;
        }
    }
}
