using System.Collections.Generic;
using System.Linq;
using Echobound.Dialogue;
using Echobound.World;

namespace Echobound.AI.Schemas
{
    /// <summary>Validates an NPC dialogue reply. Keeps lines short and identifiers valid.</summary>
    public class DialogueResponseValidator : IResponseValidator<DialogueResponse>
    {
        private readonly HashSet<string> _allowedFactIds;
        public const int MaxLineChars = 420;

        public DialogueResponseValidator(IEnumerable<string> allowedFactIds)
        {
            _allowedFactIds = new HashSet<string>(allowedFactIds ?? new string[0]);
        }

        public string SchemaName => "DialogueResponse";
        public string JsonSchema => @"{
 ""type"":""object"",""additionalProperties"":false,
 ""properties"":{
  ""npc_line"":{""type"":""string""},""mood"":{""type"":""string""},""relationship_change"":{""type"":""integer""},
  ""fear_change"":{""type"":""integer""},""intent_detected"":{""type"":""string""},
  ""revealed_fact_ids"":{""type"":""array"",""items"":{""type"":""string""}},""new_memory_summary"":{""type"":""string""},
  ""suggested_options"":{""type"":""array"",""items"":{""type"":""string""}},""ends_conversation"":{""type"":""boolean""},
  ""becomes_hostile"":{""type"":""boolean""},""gives_item"":{""type"":""string""},""gives_item_label"":{""type"":""string""}
 },
 ""required"":[""npc_line"",""mood"",""relationship_change"",""fear_change"",""intent_detected"",""revealed_fact_ids"",""new_memory_summary"",""suggested_options"",""ends_conversation"",""becomes_hostile"",""gives_item"",""gives_item_label""]
}";

        public ValidationResult<DialogueResponse> Validate(string rawText)
        {
            var o = JsonHelper.ExtractObject(rawText, out var err);
            if (o == null) return ValidationResult<DialogueResponse>.Failure(err);
            var corrections = new List<string>();
            var r = new DialogueResponse
            {
                NpcLine = JsonHelper.Str(o, "npc_line").Trim(),
                Mood = JsonHelper.Str(o, "mood", "CALM").ToUpperInvariant(),
                RelationshipChange = JsonHelper.Clamp(JsonHelper.Int(o, "relationship_change"), -30, 30),
                FearChange = JsonHelper.Clamp(JsonHelper.Int(o, "fear_change"), -30, 30),
                IntentDetected = JsonHelper.Str(o, "intent_detected", "FREE_TEXT").ToUpperInvariant(),
                NewMemorySummary = JsonHelper.Str(o, "new_memory_summary").Trim(),
                EndsConversation = JsonHelper.Bool(o, "ends_conversation"),
                BecomesHostile = JsonHelper.Bool(o, "becomes_hostile"),
                GivesItem = JsonHelper.Str(o, "gives_item").ToUpperInvariant(),
                GivesItemLabel = JsonHelper.Str(o, "gives_item_label")
            };
            if (string.IsNullOrWhiteSpace(r.NpcLine)) return ValidationResult<DialogueResponse>.Failure("empty npc_line");
            if (r.NpcLine.Length > MaxLineChars) { r.NpcLine = r.NpcLine.Substring(0, MaxLineChars - 1).TrimEnd() + "…"; corrections.Add("npc_line truncated"); }
            if (!WorldBible.IsValidMood(r.Mood)) { r.Mood = WorldBible.TryCorrect(r.Mood, WorldBible.Moods) ?? "CALM"; corrections.Add("mood corrected"); }
            if (!WorldBible.IsValidIntent(r.IntentDetected)) { r.IntentDetected = WorldBible.TryCorrect(r.IntentDetected, WorldBible.DialogueIntents) ?? "FREE_TEXT"; }
            foreach (var t in JsonHelper.Arr(o, "revealed_fact_ids"))
            {
                var id = t.ToString().Trim();
                if (_allowedFactIds.Contains(id)) r.RevealedFactIds.Add(id);
                else corrections.Add("dropped unknown/unknowable fact id " + id);
            }
            foreach (var t in JsonHelper.Arr(o, "suggested_options"))
            {
                var s = t.ToString().Trim();
                if (s.Length > 0 && s.Length <= 60) r.SuggestedOptions.Add(s);
            }
            if (r.SuggestedOptions.Count > 4) r.SuggestedOptions = r.SuggestedOptions.Take(4).ToList();
            if (!string.IsNullOrEmpty(r.GivesItem) && !WorldBible.IsValidItem(r.GivesItem))
            {
                r.GivesItem = WorldBible.TryCorrect(r.GivesItem, WorldBible.ItemTypes) ?? "";
                if (r.GivesItem == "") corrections.Add("gives_item cleared");
            }
            return ValidationResult<DialogueResponse>.Success(r, corrections);
        }
    }
}
