using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Echobound.World
{
    /// <summary>
    /// DYNAMIC narrative content generated once per New Game by the AI Director (or the mock generator).
    /// Serialized with the save file so the same story is reconstructed on load.
    /// Field names use snake_case to match the JSON contract with the AI.
    /// </summary>
    [Serializable]
    public class WorldSeed
    {
        [JsonProperty("world_id")] public string WorldId = "";
        [JsonProperty("title")] public string Title = "";
        [JsonProperty("intro_text")] public string IntroText = "";
        [JsonProperty("main_conflict")] public string MainConflict = "";
        [JsonProperty("central_mystery")] public string CentralMystery = "";
        [JsonProperty("villain_npc_id")] public string VillainNpcId = "";
        [JsonProperty("victim_npc_id")] public string VictimNpcId = "";
        [JsonProperty("factions")] public List<FactionSeed> Factions = new List<FactionSeed>();
        [JsonProperty("npc_states")] public List<NpcSeed> NpcStates = new List<NpcSeed>();
        [JsonProperty("relationships")] public List<RelationshipSeed> Relationships = new List<RelationshipSeed>();
        [JsonProperty("hidden_truths")] public List<HiddenTruth> HiddenTruths = new List<HiddenTruth>();
        [JsonProperty("opening_quest")] public Quests.Quest OpeningQuest;
        [JsonProperty("opening_opportunity")] public Quests.Quest OpeningOpportunity;
        [JsonProperty("possible_endings")] public List<EndingSeed> PossibleEndings = new List<EndingSeed>();
        [JsonProperty("active_events")] public List<DirectorEvent> ActiveEvents = new List<DirectorEvent>();
        [JsonProperty("world_tension")] public int WorldTension = 20;
        [JsonProperty("player_reputation")] public Dictionary<string, int> PlayerReputation = new Dictionary<string, int>();
        [JsonProperty("elapsed_time")] public int ElapsedTime = 0;

        public NpcSeed GetNpc(string id) => NpcStates.Find(n => n.NpcId == id);
        public FactionSeed GetFaction(string id) => Factions.Find(f => f.Id == id);
    }

    [Serializable]
    public class FactionSeed
    {
        [JsonProperty("id")] public string Id = "";
        [JsonProperty("stance")] public string Stance = "NEUTRAL";
        [JsonProperty("goal")] public string Goal = "";
        [JsonProperty("public_face")] public string PublicFace = "";
        [JsonProperty("allied_with")] public List<string> AlliedWith = new List<string>();
        [JsonProperty("hostile_to")] public List<string> HostileTo = new List<string>();
        [JsonProperty("initial_player_reputation")] public int InitialPlayerReputation = 0;
    }

    [Serializable]
    public class NpcSeed
    {
        [JsonProperty("npc_id")] public string NpcId = "";
        [JsonProperty("display_name")] public string DisplayName = "";
        [JsonProperty("role")] public string Role = "";
        [JsonProperty("faction")] public string Faction = "NONE";
        [JsonProperty("personality")] public string Personality = "";
        [JsonProperty("speech_style")] public string SpeechStyle = "";
        [JsonProperty("public_goal")] public string PublicGoal = "";
        [JsonProperty("secret")] public string Secret = "";
        [JsonProperty("secret_is_crime")] public bool SecretIsCrime = false;
        [JsonProperty("mood")] public string Mood = "CALM";
        [JsonProperty("alive")] public bool Alive = true;
        [JsonProperty("initial_location")] public string InitialLocation = "";
        /// <summary>Fact ids this NPC knows at game start.</summary>
        [JsonProperty("knowledge")] public List<string> Knowledge = new List<string>();
        [JsonProperty("initial_player_relationship")] public int InitialPlayerRelationship = 0;
        [JsonProperty("important_inventory")] public List<string> ImportantInventory = new List<string>();
    }

    [Serializable]
    public class RelationshipSeed
    {
        [JsonProperty("a")] public string A = "";
        [JsonProperty("b")] public string B = "";
        [JsonProperty("type")] public string Type = "ALLY";
        [JsonProperty("summary")] public string Summary = "";
        [JsonProperty("is_secret")] public bool IsSecret = false;
    }

    /// <summary>A secret about the world that can be discovered through dialogue, evidence, or rumor.</summary>
    [Serializable]
    public class HiddenTruth
    {
        [JsonProperty("id")] public string Id = "";
        [JsonProperty("summary")] public string Summary = "";
        [JsonProperty("known_by")] public List<string> KnownBy = new List<string>();
        [JsonProperty("evidence_location")] public string EvidenceLocation = "";
        [JsonProperty("evidence_item")] public string EvidenceItem = "EVIDENCE";
        [JsonProperty("evidence_label")] public string EvidenceLabel = "";
        [JsonProperty("is_core")] public bool IsCore = false;
    }

    [Serializable]
    public class EndingSeed
    {
        [JsonProperty("id")] public string Id = "";
        [JsonProperty("summary")] public string Summary = "";
        [JsonProperty("condition_hint")] public string ConditionHint = "";
    }
}
