using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Echobound.World
{
    /// <summary>
    /// A piece of information about the world. Facts are the currency of the knowledge system:
    /// an NPC only "knows" a fact if its id is in that NPC's knowledge set, and knowledge only
    /// arrives through believable channels (witnessing, faction communication, guards, rumors).
    /// </summary>
    [Serializable]
    public class Fact
    {
        [JsonProperty("id")] public string Id = "";
        [JsonProperty("summary")] public string Summary = "";
        [JsonProperty("importance")] public int Importance = 3;
        [JsonProperty("is_secret")] public bool IsSecret = false;
        [JsonProperty("about_npc")] public string AboutNpc = "";
        [JsonProperty("about_player")] public bool AboutPlayer = false;
        [JsonProperty("location")] public string Location = "";
        [JsonProperty("created_minute")] public int CreatedMinute = 0;
        [JsonProperty("player_knows")] public bool PlayerKnows = false;
        /// <summary>Topic keywords used to match player questions in dialogue.</summary>
        [JsonProperty("topics")] public List<string> Topics = new List<string>();
    }

    /// <summary>Registry of all facts in the current world.</summary>
    [Serializable]
    public class FactRegistry
    {
        [JsonProperty("facts")] public List<Fact> Facts = new List<Fact>();
        [JsonProperty("next_id")] public int NextId = 1;

        public Fact Get(string id) => Facts.Find(f => f.Id == id);

        public Fact Add(string summary, int importance, bool isSecret, string location, int minute,
            string aboutNpc = "", bool aboutPlayer = false, IEnumerable<string> topics = null, string forcedId = null)
        {
            var fact = new Fact
            {
                Id = forcedId ?? ("FACT_" + (NextId++).ToString("000")),
                Summary = summary,
                Importance = Math.Max(1, Math.Min(10, importance)),
                IsSecret = isSecret,
                Location = location ?? "",
                CreatedMinute = minute,
                AboutNpc = aboutNpc ?? "",
                AboutPlayer = aboutPlayer
            };
            if (topics != null) fact.Topics.AddRange(topics.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.ToLowerInvariant()));
            Facts.Add(fact);
            return fact;
        }

        public IEnumerable<Fact> KnownByPlayer() => Facts.Where(f => f.PlayerKnows);
    }
}
