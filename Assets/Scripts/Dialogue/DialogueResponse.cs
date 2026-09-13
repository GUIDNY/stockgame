using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Echobound.Dialogue
{
    /// <summary>Structured NPC reply. Produced by the LLM (validated) or by the mock provider.</summary>
    [Serializable]
    public class DialogueResponse
    {
        [JsonProperty("npc_line")] public string NpcLine = "";
        [JsonProperty("mood")] public string Mood = "CALM";
        [JsonProperty("relationship_change")] public int RelationshipChange = 0;
        [JsonProperty("fear_change")] public int FearChange = 0;
        [JsonProperty("intent_detected")] public string IntentDetected = "GREET";
        [JsonProperty("revealed_fact_ids")] public List<string> RevealedFactIds = new List<string>();
        [JsonProperty("new_memory_summary")] public string NewMemorySummary = "";
        [JsonProperty("suggested_options")] public List<string> SuggestedOptions = new List<string>();
        [JsonProperty("ends_conversation")] public bool EndsConversation = false;
        [JsonProperty("becomes_hostile")] public bool BecomesHostile = false;
        [JsonProperty("gives_item")] public string GivesItem = "";
        [JsonProperty("gives_item_label")] public string GivesItemLabel = "";
    }
}
