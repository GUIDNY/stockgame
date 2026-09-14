using UnityEngine;

namespace StrikerFive.Audio
{
    /// <summary>Synthesised crowd bed, kicks, whistles and goal roar. No audio assets.</summary>
    public class StadiumAudio : MonoBehaviour
    {
        private AudioSource _crowd, _fx;
        private AudioClip _kick, _whistle, _roar;
        private float _excitement;

        public static StadiumAudio Create(Transform parent)
        {
            var go = new GameObject("StadiumAudio");
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<StadiumAudio>();
            a._crowd = go.AddComponent<AudioSource>();
            a._crowd.clip = Noise("crowd", 2f, 0.06f, 220f); a._crowd.loop = true; a._crowd.volume = 0.25f; a._crowd.spatialBlend = 0f;
            a._crowd.Play();
            a._fx = go.AddComponent<AudioSource>();
            a._fx.spatialBlend = 0f; a._fx.volume = 0.8f;
            a._kick = Noise("kick", 0.09f, 0.9f, 0f, true);
            a._whistle = Tone("whistle", 0.5f, 2300f, 28f);
            a._roar = Noise("roar", 2.5f, 0.05f, 400f, true);
            return a;
        }

        private static AudioClip Noise(string name, float seconds, float smoothing, float toneHz, bool decay = false)
        {
            const int rate = 22050;
            int len = (int)(rate * seconds);
            var data = new float[len];
            var rng = new System.Random(name.Length);
            float low = 0f;
            for (int i = 0; i < len; i++)
            {
                float white = (float)(rng.NextDouble() * 2 - 1);
                low = Mathf.Lerp(low, white, smoothing);
                float t = i / (float)rate;
                float env = decay ? Mathf.Exp(-t * (seconds < 0.2f ? 30f : 1.2f)) : 1f;
                float s = low * 2.5f + (toneHz > 0f ? 0.15f * Mathf.Sin(t * toneHz * Mathf.PI * 2f) * Mathf.Sin(t * 0.7f) : 0f);
                data[i] = Mathf.Clamp(s * env, -1f, 1f);
            }
            var clip = AudioClip.Create(name, len, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Tone(string name, float seconds, float hz, float tremoloHz)
        {
            const int rate = 22050;
            int len = (int)(rate * seconds);
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Clamp01(t * 60f) * Mathf.Clamp01((seconds - t) * 20f);
                float trem = 0.6f + 0.4f * Mathf.Sin(t * tremoloHz * Mathf.PI * 2f);
                data[i] = Mathf.Sin(t * hz * Mathf.PI * 2f) * 0.6f * env * trem;
            }
            var clip = AudioClip.Create(name, len, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void Kick(float power) { _fx.PlayOneShot(_kick, Mathf.Clamp01(power / 30f) * 0.9f + 0.2f); }
        public void Whistle(int count) { StartCoroutine(WhistleRoutine(count)); }
        public void Goal() { _fx.PlayOneShot(_roar, 1f); _excitement = 1f; }
        public void SetExcitement(float e) { _excitement = Mathf.Max(_excitement, e); }

        private System.Collections.IEnumerator WhistleRoutine(int count)
        {
            for (int i = 0; i < count; i++) { _fx.PlayOneShot(_whistle, 0.7f); yield return new WaitForSeconds(0.55f); }
        }

        private void Update()
        {
            _excitement = Mathf.MoveTowards(_excitement, 0f, Time.deltaTime * 0.25f);
            _crowd.volume = Mathf.Lerp(_crowd.volume, 0.22f + _excitement * 0.5f, 2f * Time.deltaTime);
            _crowd.pitch = 0.95f + _excitement * 0.15f;
        }
    }
}
