using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Echobound.World;

namespace Echobound.NPC
{
    /// <summary>
    /// Complete dynamic state of one important NPC. Built from an NpcSeed at New Game and saved with the world.
    /// The Unity NPCController is a thin view over this object.
    /// </summary>
    [Serializable]
    public class NpcState
    {
        [JsonProperty("npc_id")] public string NpcId = "";
        [JsonProperty("display_name")] public string DisplayName = "";
        [JsonProperty("role")] public string Role = "";
        [JsonProperty("faction")] public string Faction = "NONE";
        [JsonProperty("personality")] public string Personality = "";
        [JsonProperty("speech_style")] public string SpeechStyle = "";
        [JsonProperty("public_goal")] public string PublicGoal = "";
        [JsonProperty("current_goal")] public string CurrentGoal = "";
        [JsonProperty("secret")] public string Secret = "";
        [JsonProperty("secret_is_crime")] public bool SecretIsCrime = false;
        [JsonProperty("mood")] public string Mood = "CALM";
        [JsonProperty("alive")] public bool Alive = true;
        [JsonProperty("health")] public int Health = 100;
        [JsonProperty("current_location")] public string CurrentLocation = "TOWN_SQUARE";
        [JsonProperty("override_location")] public string OverrideLocation = "";
        [JsonProperty("override_until_minute")] public int OverrideUntilMinute = -1;
        [JsonProperty("hostile_to_player")] public bool HostileToPlayer = false;
        [JsonProperty("met_player")] public bool MetPlayer = false;
        [JsonProperty("times_talked")] public int TimesTalked = 0;
        [JsonProperty("relationship")] public RelationshipState Relationship = new RelationshipState();
        [JsonProperty("memory")] public NPCMemory Memory = new NPCMemory();
        [JsonProperty("knowledge")] public List<string> Knowledge = new List<string>();
        [JsonProperty("inventory")] public List<string> Inventory = new List<string>();
        [JsonProperty("killer")] public string Killer = "";
        [JsonProperty("death_minute")] public int DeathMinute = -1;

        public static NpcState FromSeed(NpcSeed seed)
        {
            var s = new NpcState
            {
                NpcId = seed.NpcId,
                DisplayName = string.IsNullOrWhiteSpace(seed.DisplayName) ? seed.NpcId : seed.DisplayName,
                Role = string.IsNullOrWhiteSpace(seed.Role) ? WorldBible.RoleOf(seed.NpcId) : seed.Role,
                Faction = WorldBible.IsValidFaction(seed.Faction) ? seed.Faction : WorldBible.NoFaction,
                Personality = seed.Personality ?? "",
                SpeechStyle = seed.SpeechStyle ?? "",
                PublicGoal = seed.PublicGoal ?? "",
                CurrentGoal = seed.PublicGoal ?? "",
                Secret = seed.Secret ?? "",
                SecretIsCrime = seed.SecretIsCrime,
                Mood = WorldBible.IsValidMood(seed.Mood) ? seed.Mood : "CALM",
                Alive = seed.Alive,
                CurrentLocation = WorldBible.IsValidLocation(seed.InitialLocation) ? seed.InitialLocation : WorldBible.RoleWorkplace[WorldBible.RoleOf(seed.NpcId)],
                Knowledge = new List<string>(seed.Knowledge ?? new List<string>()),
                Inventory = new List<string>(seed.ImportantInventory ?? new List<string>())
            };
            s.Relationship.ApplyOverall(seed.InitialPlayerRelationship);
            return s;
        }

        public bool Knows(string factId) => Knowledge.Contains(factId);

        public void Learn(string factId)
        {
            if (!Knowledge.Contains(factId)) Knowledge.Add(factId);
        }

        /// <summary>Where this NPC should be at the given hour according to its role schedule (or an override).</summary>
        public string ScheduledLocation(int hour, int currentMinute)
        {
            if (!string.IsNullOrEmpty(OverrideLocation) && (OverrideUntilMinute < 0 || currentMinute < OverrideUntilMinute))
                return OverrideLocation;
            var role = WorldBible.RoleSchedules.ContainsKey(Role) ? Role : "STRANGER";
            var entries = WorldBible.RoleSchedules[role];
            // Entries are (hour, location) but not necessarily sorted from 0; find the latest entry <= hour, else wrap to the last one.
            string result = null;
            int bestHour = -1;
            foreach (var (h, loc) in entries)
            {
                if (h <= hour && h > bestHour) { bestHour = h; result = loc; }
            }
            if (result == null)
            {
                int maxHour = -1;
                foreach (var (h, loc) in entries) if (h > maxHour) { maxHour = h; result = loc; }
            }
            return result ?? WorldBible.RoleWorkplace[role];
        }

        public string DescribeForPrompt(bool includeSecret)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"{DisplayName} ({Role.Replace('_', ' ').ToLowerInvariant()}), faction {Faction}. ");
            sb.Append($"Personality: {Personality}. Speech: {SpeechStyle}. Mood: {Mood}. ");
            sb.Append($"Goal: {CurrentGoal}. ");
            if (includeSecret && !string.IsNullOrEmpty(Secret)) sb.Append($"SECRET (never reveal casually): {Secret}. ");
            return sb.ToString();
        }
    }
}
