using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Echobound.NPC;
using Echobound.Quests;

namespace Echobound.World
{
    /// <summary>Everything the player owns and is; kept small and serializable.</summary>
    [Serializable]
    public class PlayerState
    {
        [JsonProperty("health")] public int Health = 100;
        [JsonProperty("max_health")] public int MaxHealth = 100;
        [JsonProperty("coins")] public int Coins = 40;
        [JsonProperty("items")] public List<Inventory.Item> Items = new List<Inventory.Item>();
        [JsonProperty("location")] public string Location = "TOWN_SQUARE";
        [JsonProperty("position")] public float[] Position = { 0f, 0f, 0f };
        [JsonProperty("rotation_y")] public float RotationY = 0f;
    }

    [Serializable]
    public class Rumor
    {
        [JsonProperty("text")] public string Text = "";
        [JsonProperty("location")] public string Location = "TAVERN";
        [JsonProperty("fact_id")] public string FactId = "";
        [JsonProperty("minute")] public int Minute = 0;
        [JsonProperty("heard_by_player")] public bool HeardByPlayer = false;
    }

    [Serializable]
    public class PlacedItem
    {
        [JsonProperty("location")] public string Location = "";
        [JsonProperty("item")] public Inventory.Item Item;
    }

    [Serializable]
    public class EventLogEntry
    {
        [JsonProperty("minute")] public int Minute;
        [JsonProperty("text")] public string Text = "";
        [JsonProperty("importance")] public int Importance = 3;
    }

    /// <summary>
    /// The complete DYNAMIC state of a playthrough: seed + everything that changed since.
    /// This is the object the save system serializes and the AI Director summarizes.
    /// </summary>
    [Serializable]
    public class WorldState
    {
        [JsonProperty("seed")] public WorldSeed Seed = new WorldSeed();
        [JsonProperty("npcs")] public List<NpcState> Npcs = new List<NpcState>();
        [JsonProperty("facts")] public FactRegistry Facts = new FactRegistry();
        [JsonProperty("player_reputation")] public Dictionary<string, int> PlayerReputation = new Dictionary<string, int>();
        [JsonProperty("price_modifiers")] public Dictionary<string, float> PriceModifiers = new Dictionary<string, float>();
        [JsonProperty("locked_locations")] public List<string> LockedLocations = new List<string>();
        [JsonProperty("rumors")] public List<Rumor> Rumors = new List<Rumor>();
        [JsonProperty("event_log")] public List<EventLogEntry> EventLog = new List<EventLogEntry>();
        [JsonProperty("applied_events")] public List<DirectorEvent> AppliedEvents = new List<DirectorEvent>();
        [JsonProperty("quests")] public List<Quest> Quests = new List<Quest>();
        /// <summary>Quests offered by an NPC but not yet accepted; delivered through dialogue.</summary>
        [JsonProperty("pending_offers")] public List<Quest> PendingOffers = new List<Quest>();
        [JsonProperty("player")] public PlayerState Player = new PlayerState();
        [JsonProperty("world_tension")] public int WorldTension = 20;
        [JsonProperty("elapsed_minutes")] public int ElapsedMinutes = 0;
        [JsonProperty("discovered_info")] public List<string> DiscoveredInfo = new List<string>();
        [JsonProperty("next_quest_number")] public int NextQuestNumber = 1;
        [JsonProperty("next_event_number")] public int NextEventNumber = 1;
        [JsonProperty("ending_reached")] public string EndingReached = "";
        /// <summary>Items physically present in search spots, keyed by location. Saved so evidence survives load.</summary>
        [JsonProperty("placed_items")] public List<PlacedItem> PlacedItems = new List<PlacedItem>();

        public NpcState GetNpc(string id) => Npcs.Find(n => n.NpcId == id);
        public IEnumerable<NpcState> AliveNpcs => Npcs.Where(n => n.Alive);
        public IEnumerable<NpcState> NpcsAt(string location) => Npcs.Where(n => n.Alive && n.CurrentLocation == location);
        public IEnumerable<NpcState> NpcsOfFaction(string faction) => Npcs.Where(n => n.Alive && n.Faction == faction);
        public string NpcName(string id) => GetNpc(id)?.DisplayName ?? (id == WorldBible.PlayerId ? "the stranger" : id);
        public bool IsLocked(string location) => LockedLocations.Contains(location);

        public string NewQuestId() => "QUEST_" + (NextQuestNumber++).ToString("000");
        public string NewEventId() => "EVENT_" + (NextEventNumber++).ToString("000");

        public void Log(string text, int importance = 3)
        {
            EventLog.Add(new EventLogEntry { Minute = ElapsedMinutes, Text = text, Importance = importance });
            if (EventLog.Count > 200) EventLog.RemoveAt(0);
        }

        public void Discover(string info)
        {
            if (string.IsNullOrWhiteSpace(info) || DiscoveredInfo.Contains(info)) return;
            DiscoveredInfo.Add(info);
            Core.GameEvents.RaiseWorldInfoDiscovered(info);
        }

        /// <summary>Builds the runtime state from a freshly generated seed.</summary>
        public static WorldState FromSeed(WorldSeed seed)
        {
            var ws = new WorldState { Seed = seed, WorldTension = seed.WorldTension, ElapsedMinutes = seed.ElapsedTime };
            foreach (var f in seed.Factions) ws.PlayerReputation[f.Id] = f.InitialPlayerReputation;
            foreach (var fid in WorldBible.FactionIds) if (!ws.PlayerReputation.ContainsKey(fid)) ws.PlayerReputation[fid] = 0;
            foreach (var kv in seed.PlayerReputation) ws.PlayerReputation[kv.Key] = kv.Value;

            // Hidden truths become secret facts known by the listed NPCs.
            foreach (var truth in seed.HiddenTruths)
            {
                ws.Facts.Add(truth.Summary, truth.IsCore ? 9 : 6, true, truth.EvidenceLocation, 0,
                    topics: TopicsFrom(truth.Summary), forcedId: truth.Id);
            }
            // Relationships that are public knowledge become facts every NPC knows; secret ones are known only to the pair.
            foreach (var rel in seed.Relationships)
            {
                var f = ws.Facts.Add($"{seed.GetNpc(rel.A)?.DisplayName ?? rel.A} and {seed.GetNpc(rel.B)?.DisplayName ?? rel.B}: {rel.Summary}",
                    rel.IsSecret ? 6 : 3, rel.IsSecret, "", 0, topics: TopicsFrom(rel.Summary));
                rel.Summary = rel.Summary ?? "";
                foreach (var npc in seed.NpcStates)
                {
                    if (!rel.IsSecret || npc.NpcId == rel.A || npc.NpcId == rel.B) npc.Knowledge.Add(f.Id);
                }
            }
            foreach (var n in seed.NpcStates) ws.Npcs.Add(NpcState.FromSeed(n));
            foreach (var truth in seed.HiddenTruths)
                foreach (var knower in truth.KnownBy) ws.GetNpc(knower)?.Learn(truth.Id);

            if (seed.OpeningQuest != null) ws.Quests.Add(seed.OpeningQuest);
            return ws;
        }

        private static readonly string[] StopWords = { "the", "a", "an", "and", "of", "to", "in", "is", "was", "for", "with", "that", "who", "has", "been", "are", "his", "her", "their", "from", "at", "on", "by", "it", "as", "he", "she", "they", "them", "this", "but", "not", "be" };

        public static IEnumerable<string> TopicsFrom(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;
            foreach (var raw in text.Split(new[] { ' ', ',', '.', ';', ':', '\'', '"', '(', ')', '!', '?' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var w = raw.ToLowerInvariant();
                if (w.Length < 4 || Array.IndexOf(StopWords, w) >= 0) continue;
                yield return w;
            }
        }
    }
}
