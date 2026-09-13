using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Echobound.World
{
    /// <summary>
    /// Structured output of the AI Director. Never executed directly: validated first, then applied by WorldChangeApplier.
    /// </summary>
    [Serializable]
    public class DirectorEvent
    {
        [JsonProperty("event_id")] public string EventId = "";
        [JsonProperty("event_type")] public string EventType = "QUIET";
        [JsonProperty("source_faction")] public string SourceFaction = "NONE";
        [JsonProperty("source_npc")] public string SourceNpc = "";
        [JsonProperty("target")] public string Target = "PLAYER";
        [JsonProperty("location")] public string Location = "TOWN_SQUARE";
        [JsonProperty("urgency")] public int Urgency = 3;
        [JsonProperty("description")] public string Description = "";
        [JsonProperty("player_notification")] public string PlayerNotification = "";
        [JsonProperty("world_changes")] public List<WorldChange> WorldChanges = new List<WorldChange>();
        [JsonProperty("created_minute")] public int CreatedMinute = 0;
        [JsonProperty("resolved")] public bool Resolved = false;
        /// <summary>Optional quest attached to a QUEST_OFFER event.</summary>
        [JsonProperty("quest")] public Quests.Quest Quest;
    }

    /// <summary>A single atomic change to world state. The type must be one of WorldBible.WorldChangeTypes.</summary>
    [Serializable]
    public class WorldChange
    {
        [JsonProperty("type")] public string Type = "";
        [JsonProperty("faction")] public string Faction = "";
        [JsonProperty("npc")] public string Npc = "";
        [JsonProperty("location")] public string Location = "";
        [JsonProperty("item")] public string Item = "";
        [JsonProperty("change")] public int Change = 0;
        [JsonProperty("count")] public int Count = 0;
        [JsonProperty("text")] public string Text = "";
        [JsonProperty("mood")] public string Mood = "";
        [JsonProperty("fact_id")] public string FactId = "";

        public static WorldChange Reputation(string faction, int delta) => new WorldChange { Type = "REPUTATION", Faction = faction, Change = delta };
        public static WorldChange Relationship(string npc, int delta) => new WorldChange { Type = "NPC_RELATIONSHIP", Npc = npc, Change = delta };
        public static WorldChange Tension(int delta) => new WorldChange { Type = "TENSION", Change = delta };
        public static WorldChange Rumor(string text, string location = "TAVERN") => new WorldChange { Type = "RUMOR", Text = text, Location = location };
        public static WorldChange SpawnEnemies(string location, int count, string faction) => new WorldChange { Type = "SPAWN_ENEMIES", Location = location, Count = count, Faction = faction };
        public static WorldChange MoveNpc(string npc, string location) => new WorldChange { Type = "NPC_LOCATION", Npc = npc, Location = location };
        public static WorldChange SetMood(string npc, string mood) => new WorldChange { Type = "NPC_MOOD", Npc = npc, Mood = mood };
        public static WorldChange Knowledge(string npc, string factId) => new WorldChange { Type = "NPC_KNOWLEDGE", Npc = npc, FactId = factId };
        public static WorldChange Goal(string npc, string text) => new WorldChange { Type = "NPC_GOAL", Npc = npc, Text = text };
        public static WorldChange Hostile(string npc) => new WorldChange { Type = "NPC_HOSTILE", Npc = npc };
        public static WorldChange PlantItem(string location, string item, string label) => new WorldChange { Type = "SPAWN_ITEM", Location = location, Item = item, Text = label };
    }
}
