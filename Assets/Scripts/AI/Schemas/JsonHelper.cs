using System;
using Newtonsoft.Json.Linq;

namespace Echobound.AI.Schemas
{
    /// <summary>Tolerant JSON extraction: strips markdown fences and prose around the first JSON object.</summary>
    public static class JsonHelper
    {
        public static JObject ExtractObject(string raw, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(raw)) { error = "empty response"; return null; }
            string text = raw.Trim();
            int fence = text.IndexOf("```", StringComparison.Ordinal);
            if (fence >= 0)
            {
                int start = text.IndexOf('\n', fence);
                int end = text.IndexOf("```", start + 1, StringComparison.Ordinal);
                if (start > 0 && end > start) text = text.Substring(start + 1, end - start - 1);
            }
            int first = text.IndexOf('{');
            int last = text.LastIndexOf('}');
            if (first < 0 || last <= first) { error = "no JSON object found"; return null; }
            text = text.Substring(first, last - first + 1);
            try { return JObject.Parse(text); }
            catch (Exception e) { error = "invalid JSON: " + e.Message; return null; }
        }

        public static string Str(JObject o, string key, string fallback = "")
        {
            var t = o?[key];
            return t == null || t.Type == JTokenType.Null ? fallback : t.ToString();
        }

        public static int Int(JObject o, string key, int fallback = 0)
        {
            var t = o?[key];
            if (t == null) return fallback;
            if (t.Type == JTokenType.Integer || t.Type == JTokenType.Float) return (int)Math.Round(t.Value<double>());
            return int.TryParse(t.ToString(), out var v) ? v : fallback;
        }

        public static bool Bool(JObject o, string key, bool fallback = false)
        {
            var t = o?[key];
            if (t == null) return fallback;
            if (t.Type == JTokenType.Boolean) return t.Value<bool>();
            return bool.TryParse(t.ToString(), out var v) ? v : fallback;
        }

        public static JArray Arr(JObject o, string key)
        {
            var t = o?[key];
            if (t is JArray a) return a;
            var created = new JArray();
            if (o != null) o[key] = created;
            return created;
        }

        public static int Clamp(int v, int min, int max) => Math.Max(min, Math.Min(max, v));
    }
}
