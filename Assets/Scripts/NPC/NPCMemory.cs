using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Echobound.NPC
{
    [Serializable]
    public class MemoryEntry
    {
        [JsonProperty("event")] public string Event = "";
        [JsonProperty("importance")] public int Importance = 3;
        [JsonProperty("summary")] public string Summary = "";
        [JsonProperty("minute")] public int Minute = 0;
        [JsonProperty("about_player")] public bool AboutPlayer = false;
        /// <summary>True when the NPC saw it happen; false when they heard about it.</summary>
        [JsonProperty("witnessed")] public bool Witnessed = true;
    }

    /// <summary>
    /// Two-tier NPC memory. Short term holds recent raw events; long term holds important or compressed memories.
    /// Compression keeps prompts small: the LLM only ever sees a short digest.
    /// </summary>
    [Serializable]
    public class NPCMemory
    {
        public const int ShortTermCapacity = 8;
        public const int LongTermCapacity = 12;
        public const int LongTermImportanceThreshold = 6;

        [JsonProperty("short_term")] public List<MemoryEntry> ShortTerm = new List<MemoryEntry>();
        [JsonProperty("long_term")] public List<MemoryEntry> LongTerm = new List<MemoryEntry>();

        public void Remember(string eventName, int importance, string summary, int minute, bool aboutPlayer, bool witnessed = true)
        {
            // Do not store exact duplicates.
            if (ShortTerm.Any(m => m.Summary == summary) || LongTerm.Any(m => m.Summary == summary)) return;
            ShortTerm.Add(new MemoryEntry
            {
                Event = eventName, Importance = Math.Max(1, Math.Min(10, importance)),
                Summary = summary, Minute = minute, AboutPlayer = aboutPlayer, Witnessed = witnessed
            });
            if (ShortTerm.Count > ShortTermCapacity) Compress();
        }

        /// <summary>Moves important short-term memories to long term and merges the rest into a single digest entry.</summary>
        public void Compress()
        {
            var important = ShortTerm.Where(m => m.Importance >= LongTermImportanceThreshold).ToList();
            var trivial = ShortTerm.Where(m => m.Importance < LongTermImportanceThreshold).ToList();
            LongTerm.AddRange(important);
            if (trivial.Count > 0)
            {
                var digest = new MemoryEntry
                {
                    Event = "DIGEST",
                    Importance = 3,
                    Minute = trivial.Max(m => m.Minute),
                    AboutPlayer = trivial.Any(m => m.AboutPlayer),
                    Witnessed = false,
                    Summary = "Earlier: " + string.Join(" ", trivial.Take(3).Select(m => Shorten(m.Summary, 60)))
                };
                LongTerm.Add(digest);
            }
            ShortTerm.Clear();
            // Keep long term bounded: drop least important, oldest first.
            while (LongTerm.Count > LongTermCapacity)
            {
                var victim = LongTerm.OrderBy(m => m.Importance).ThenBy(m => m.Minute).First();
                LongTerm.Remove(victim);
            }
        }

        public MemoryEntry MostImportantAboutPlayer()
        {
            return ShortTerm.Concat(LongTerm).Where(m => m.AboutPlayer)
                .OrderByDescending(m => m.Importance).ThenByDescending(m => m.Minute).FirstOrDefault();
        }

        public bool HasMemoryOf(string eventName) => ShortTerm.Concat(LongTerm).Any(m => m.Event == eventName);

        /// <summary>Digest for prompts: at most N lines, most important first.</summary>
        public string ToPromptDigest(int maxLines = 6)
        {
            var all = ShortTerm.Concat(LongTerm)
                .OrderByDescending(m => m.Importance).ThenByDescending(m => m.Minute).Take(maxLines).ToList();
            if (all.Count == 0) return "(no notable memories)";
            var sb = new StringBuilder();
            foreach (var m in all) sb.Append("- ").Append(m.Witnessed ? "" : "(heard) ").Append(m.Summary).Append('\n');
            return sb.ToString().TrimEnd();
        }

        private static string Shorten(string s, int max) => s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }

    /// <summary>Multi-axis relationship with the player. Overall is derived, not stored separately.</summary>
    [Serializable]
    public class RelationshipState
    {
        [JsonProperty("trust")] public int Trust = 0;
        [JsonProperty("fear")] public int Fear = 0;
        [JsonProperty("respect")] public int Respect = 0;
        [JsonProperty("hostility")] public int Hostility = 0;

        [JsonIgnore] public int Overall => Clamp((Trust + Respect - Hostility) / 2);

        public void Apply(int trust = 0, int fear = 0, int respect = 0, int hostility = 0)
        {
            Trust = Clamp(Trust + trust); Fear = Clamp(Fear + fear);
            Respect = Clamp(Respect + respect); Hostility = Clamp(Hostility + hostility);
        }

        /// <summary>Applies a generic +/- change the way an LLM would express it ("relationship_change": -15).</summary>
        public void ApplyOverall(int delta)
        {
            if (delta > 0) Apply(trust: delta, respect: delta / 2, hostility: -delta / 2);
            else if (delta < 0) Apply(trust: delta, hostility: -delta / 2);
        }

        public string Label()
        {
            if (Hostility >= 60) return "Hostile";
            if (Fear >= 50 && Trust < 20) return "Intimidated";
            int o = Overall;
            if (o >= 50) return "Loyal";
            if (o >= 20) return "Friendly";
            if (o > -20) return "Neutral";
            if (o > -50) return "Cold";
            return "Hateful";
        }

        private static int Clamp(int v) => Math.Max(-100, Math.Min(100, v));
    }
}
