using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TurboLoop.Core
{
    /// <summary>Material and procedural texture cache. Everything visual is generated at runtime; no art assets.</summary>
    public static class Materials
    {
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        private static Shader _standard;

        public static Shader Standard
        {
            get
            {
                if (_standard == null) _standard = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Legacy Shaders/Diffuse");
                return _standard;
            }
        }

        public static Material Get(Color c, float metallic = 0f, float smoothness = 0.35f)
        {
            string key = $"c{c.r:0.00}{c.g:0.00}{c.b:0.00}{c.a:0.00}m{metallic:0.00}s{smoothness:0.00}";
            if (Cache.TryGetValue(key, out var m)) return m;
            m = new Material(Standard) { color = c };
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glossiness", smoothness);
            Cache[key] = m;
            return m;
        }

        public static Material Textured(Texture2D tex, Color tint, Vector2 tiling, float smoothness = 0.2f)
        {
            var m = new Material(Standard) { color = tint, mainTexture = tex, mainTextureScale = tiling };
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Glossiness", smoothness);
            return m;
        }

        public static Material Emissive(Color c)
        {
            string key = "e" + c;
            if (Cache.TryGetValue(key, out var m)) return m;
            m = new Material(Standard) { color = c };
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 1.8f);
            Cache[key] = m;
            return m;
        }

        public static Texture2D Noise(int size, Color a, Color b, int seed, float scale = 1f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var rng = new System.Random(seed);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float n = (float)rng.NextDouble();
                    float p = Mathf.PerlinNoise(x * scale * 0.09f + seed, y * scale * 0.09f + seed);
                    pixels[y * size + x] = Color.Lerp(a, b, Mathf.Clamp01(0.55f * p + 0.45f * n));
                }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>Horizontal stripes so the pattern repeats along the v (road direction) axis.</summary>
        public static Texture2D Stripes(Color a, Color b, int stripes = 2, int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                bool odd = (y * stripes / size) % 2 == 1;
                for (int x = 0; x < size; x++) pixels[y * size + x] = odd ? b : a;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        public static Texture2D Checker(Color a, Color b, int cells = 8)
        {
            var tex = new Texture2D(cells, cells, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            var pixels = new Color[cells * cells];
            for (int y = 0; y < cells; y++)
                for (int x = 0; x < cells; x++) pixels[y * cells + x] = ((x + y) % 2 == 0) ? a : b;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>Bright daytime lighting: procedural skybox, warm sun, sky-coloured ambient, light distance fog.</summary>
        public static void SetupSky(Light sun, string theme)
        {
            Color skyTint, ground, fog;
            switch (theme)
            {
                case "mountain": skyTint = new Color(0.45f, 0.6f, 0.85f); ground = new Color(0.35f, 0.4f, 0.35f); fog = new Color(0.72f, 0.8f, 0.9f); break;
                case "city": skyTint = new Color(0.55f, 0.55f, 0.75f); ground = new Color(0.3f, 0.3f, 0.34f); fog = new Color(0.75f, 0.76f, 0.82f); break;
                default: skyTint = new Color(0.4f, 0.65f, 0.95f); ground = new Color(0.3f, 0.5f, 0.35f); fog = new Color(0.78f, 0.86f, 0.95f); break;
            }
            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetFloat("_SunSize", 0.04f);
                sky.SetFloat("_AtmosphereThickness", 0.9f);
                sky.SetColor("_SkyTint", skyTint);
                sky.SetColor("_GroundColor", ground);
                sky.SetFloat("_Exposure", 1.25f);
                RenderSettings.skybox = sky;
            }
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.7f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.55f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.25f, 0.22f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fog;
            RenderSettings.fogStartDistance = 180f;
            RenderSettings.fogEndDistance = 900f;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.25f;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            DynamicGI.UpdateEnvironment();
        }
    }
}
