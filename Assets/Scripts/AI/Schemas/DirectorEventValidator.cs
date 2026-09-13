using System.Collections.Generic;
using Echobound.World;

namespace Echobound.AI.Schemas
{
    /// <summary>Validates a DirectorEvent JSON produced by the AI. Auto-corrects near-miss identifiers, rejects invented ones.</summary>
    public class DirectorEventValidator : IResponseValidator<DirectorEvent>
    {
        public string SchemaName => "DirectorEvent";
        public string JsonSchema => @"{
 ""type"":""object"",""additionalProperties"":false,
 ""properties"":{
  ""event_type"":{""type"":""string""},""source_faction"":{""type"":""string""},""source_npc"":{""type"":""string""},
  ""target"":{""type"":""string""},""location"":{""type"":""string""},""urgency"":{""type"":""integer""},
  ""description"":{""type"":""string""},""player_notification"":{""type"":""string""},
  ""world_changes"":{""type"":""array"",""items"":{""type"":""object"",""additionalProperties"":false,""properties"":{
    ""type"":{""type"":""string""},""faction"":{""type"":""string""},""npc"":{""type"":""string""},""location"":{""type"":""string""},
    ""item"":{""type"":""string""},""change"":{""type"":""integer""},""count"":{""type"":""integer""},""text"":{""type"":""string""},
    ""mood"":{""type"":""string""},""fact_id"":{""type"":""string""}},
    ""required"":[""type"",""faction"",""npc"",""location"",""item"",""change"",""count"",""text"",""mood"",""fact_id""]}}
 },
 ""required"":[""event_type"",""source_faction"",""source_npc"",""target"",""location"",""urgency"",""description"",""player_notification"",""world_changes""]
}";

        public ValidationResult<DirectorEvent> Validate(string rawText)
        {
            var o = JsonHelper.ExtractObject(rawText, out var err);
            if (o == null) return ValidationResult<DirectorEvent>.Failure(err);
            var errors = new List<string>();
            var corrections = new List<string>();
            var ev = new DirectorEvent
            {
                EventType = JsonHelper.Str(o, "event_type").ToUpperInvariant(),
                SourceFaction = JsonHelper.Str(o, "source_faction", "NONE").ToUpperInvariant(),
                SourceNpc = JsonHelper.Str(o, "source_npc").ToUpperInvariant(),
                Target = JsonHelper.Str(o, "target", "PLAYER").ToUpperInvariant(),
                Location = JsonHelper.Str(o, "location").ToUpperInvariant(),
                Urgency = JsonHelper.Clamp(JsonHelper.Int(o, "urgency", 3), 1, 10),
                Description = JsonHelper.Str(o, "description"),
                PlayerNotification = JsonHelper.Str(o, "player_notification")
            };
            if (!WorldBible.IsValidEventType(ev.EventType))
            {
                var t = WorldBible.TryCorrect(ev.EventType, WorldBible.EventTypes);
                if (t == null) errors.Add("invalid event_type: " + ev.EventType);
                else { corrections.Add($"event_type {ev.EventType} -> {t}"); ev.EventType = t; }
            }
            if (!WorldBible.IsValidFaction(ev.SourceFaction))
            {
                var f = WorldBible.TryCorrect(ev.SourceFaction, WorldBible.FactionIds);
                if (f == null) { corrections.Add("source_faction -> NONE"); ev.SourceFaction = "NONE"; } else ev.SourceFaction = f;
            }
            if (!string.IsNullOrEmpty(ev.SourceNpc) && !WorldBible.IsValidNpc(ev.SourceNpc))
            {
                var n = WorldBible.TryCorrect(ev.SourceNpc, WorldBible.NpcIds);
                if (n == null) { corrections.Add("source_npc cleared"); ev.SourceNpc = ""; } else ev.SourceNpc = n;
            }
            if (ev.Target != "PLAYER" && !WorldBible.IsValidNpc(ev.Target) && !WorldBible.IsValidFaction(ev.Target))
            {
                var n = WorldBible.TryCorrect(ev.Target, WorldBible.NpcIds) ?? WorldBible.TryCorrect(ev.Target, WorldBible.FactionIds);
                if (n == null) { corrections.Add("target -> PLAYER"); ev.Target = "PLAYER"; } else ev.Target = n;
            }
            if (!WorldBible.IsValidLocation(ev.Location))
            {
                var l = WorldBible.TryCorrect(ev.Location, WorldBible.Locations);
                if (l == null) errors.Add("invalid location: " + ev.Location);
                else { corrections.Add($"location {ev.Location} -> {l}"); ev.Location = l; }
            }
            if (string.IsNullOrWhiteSpace(ev.Description)) errors.Add("missing description");
            ev.WorldChanges = QuestSchema.ParseChanges(o["world_changes"] as Newtonsoft.Json.Linq.JArray, errors, corrections);
            if (ev.WorldChanges.Count > 8) { ev.WorldChanges = ev.WorldChanges.GetRange(0, 8); corrections.Add("trimmed world_changes to 8"); }
            if (o["quest"] is Newtonsoft.Json.Linq.JObject qo)
            {
                var questErrors = new List<string>();
                ev.Quest = QuestSchema.ParseQuest(qo, questErrors, corrections);
                if (ev.Quest == null) errors.AddRange(questErrors);
            }
            if (ev.EventType == "QUEST_OFFER" && ev.Quest == null) errors.Add("QUEST_OFFER without a valid quest");
            if (errors.Count > 0) return new ValidationResult<DirectorEvent> { Ok = false, Errors = errors, Corrections = corrections };
            return ValidationResult<DirectorEvent>.Success(ev, corrections);
        }
    }
}
