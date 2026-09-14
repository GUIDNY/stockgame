using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace StrikerFive.Core
{
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

        public static Material Textured(Texture2D tex, Vector2 tiling, float smoothness = 0.2f)
        {
            var m = new Material(Standard) { color = Color.white, mainTexture = tex, mainTextureScale = tiling };
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Glossiness", smoothness);
            return m;
        }

        public static Material Emissive(Color c, float strength = 2f)
        {
            string key = "e" + c + strength;
            if (Cache.TryGetValue(key, out var m)) return m;
            m = new Material(Standard) { color = c };
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * strength);
            Cache[key] = m;
            return m;
        }

        /// <summary>Mown-grass stripes with a little noise.</summary>
        public static Texture2D Grass(int size = 256, int stripes = 16)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var rng = new System.Random(4);
            var a = new Color(0.22f, 0.55f, 0.2f);
            var b = new Color(0.3f, 0.65f, 0.25f);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool light = (x * stripes / size) % 2 == 0;
                    float n = (float)rng.NextDouble() * 0.08f;
                    var c = light ? b : a;
                    pixels[y * size + x] = new Color(c.r + n, c.g + n, c.b + n * 0.5f);
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

        public static void SetupSky(Light sun)
        {
            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetFloat("_SunSize", 0.035f);
                sky.SetFloat("_AtmosphereThickness", 0.8f);
                sky.SetColor("_SkyTint", new Color(0.4f, 0.6f, 0.95f));
                sky.SetColor("_GroundColor", new Color(0.35f, 0.4f, 0.35f));
                sky.SetFloat("_Exposure", 1.2f);
                RenderSettings.skybox = sky;
            }
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.7f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.55f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.25f, 0.2f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.75f, 0.82f, 0.95f);
            RenderSettings.fogStartDistance = 150f;
            RenderSettings.fogEndDistance = 600f;
            sun.color = new Color(1f, 0.97f, 0.9f);
            sun.intensity = 1.3f;
            sun.transform.rotation = Quaternion.Euler(58f, 25f, 0f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.7f;
            DynamicGI.UpdateEnvironment();
        }
    }
}
