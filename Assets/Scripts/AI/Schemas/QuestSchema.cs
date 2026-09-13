using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Echobound.Quests;
using Echobound.World;

namespace Echobound.AI.Schemas
{
    /// <summary>Shared quest parsing/validation used by the world seed and quest validators.</summary>
    public static class QuestSchema
    {
        public const string JsonSchemaText = @"{
 ""type"":""object"",""additionalProperties"":false,
 ""properties"":{
  ""quest_id"":{""type"":""string""},""title"":{""type"":""string""},""narrative_reason"":{""type"":""string""},
  ""giver_npc"":{""type"":""string""},""trigger_event"":{""type"":""string""},""is_main"":{""type"":""boolean""},
  ""sequential"":{""type"":""boolean""},
  ""objectives"":{""type"":""array"",""items"":{""type"":""object"",""additionalProperties"":false,""properties"":{
    ""action"":{""type"":""string""},""location"":{""type"":""string""},""target_npc"":{""type"":""string""},
    ""target_item"":{""type"":""string""},""description"":{""type"":""string""},""optional"":{""type"":""boolean""},
    ""available_from_hour"":{""type"":""integer""},""available_until_hour"":{""type"":""integer""}},
    ""required"":[""action"",""location"",""target_npc"",""target_item"",""description"",""optional"",""available_from_hour"",""available_until_hour""]}},
  ""relevant_npcs"":{""type"":""array"",""items"":{""type"":""string""}},
  ""resolutions"":{""type"":""array"",""items"":{""type"":""object"",""additionalProperties"":false,""properties"":{
    ""summary"":{""type"":""string""},""requires_item"":{""type"":""string""},""requires_npc_alive"":{""type"":""string""},
    ""outcome_text"":{""type"":""string""},""ending_id"":{""type"":""string""},
    ""consequences"":{""type"":""array"",""items"":{""$ref"":""#/$defs/change""}}},
    ""required"":[""summary"",""requires_item"",""requires_npc_alive"",""outcome_text"",""ending_id"",""consequences""]}},
  ""failure_conditions"":{""type"":""array"",""items"":{""type"":""object"",""additionalProperties"":false,""properties"":{
    ""type"":{""type"":""string""},""npc_id"":{""type"":""string""},""deadline_minute"":{""type"":""integer""},""mutation_hint"":{""type"":""string""}},
    ""required"":[""type"",""npc_id"",""deadline_minute"",""mutation_hint""]}},
  ""world_consequences"":{""type"":""array"",""items"":{""$ref"":""#/$defs/change""}}
 },
 ""required"":[""quest_id"",""title"",""narrative_reason"",""giver_npc"",""trigger_event"",""is_main"",""sequential"",""objectives"",""relevant_npcs"",""resolutions"",""failure_conditions"",""world_consequences""],
 ""$defs"":{""change"":{""type"":""object"",""additionalProperties"":false,""properties"":{
    ""type"":{""type"":""string""},""faction"":{""type"":""string""},""npc"":{""type"":""string""},""location"":{""type"":""string""},
    ""item"":{""type"":""string""},""change"":{""type"":""integer""},""count"":{""type"":""integer""},""text"":{""type"":""string""},
    ""mood"":{""type"":""string""},""fact_id"":{""type"":""string""}},
    ""required"":[""type"",""faction"",""npc"",""location"",""item"",""change"",""count"",""text"",""mood"",""fact_id""]}}
}";

        public static WorldChange ParseChange(JObject c, List<string> errors, List<string> corrections)
        {
            if (c == null) return null;
            var change = new WorldChange
            {
                Type = JsonHelper.Str(c, "type").ToUpperInvariant(),
                Faction = JsonHelper.Str(c, "faction").ToUpperInvariant(),
                Npc = JsonHelper.Str(c, "npc").ToUpperInvariant(),
                Location = JsonHelper.Str(c, "location").ToUpperInvariant(),
                Item = JsonHelper.Str(c, "item").ToUpperInvariant(),
                Change = JsonHelper.Clamp(JsonHelper.Int(c, "change"), -100, 100),
                Count = JsonHelper.Clamp(JsonHelper.Int(c, "count"), 0, 4),
                Text = JsonHelper.Str(c, "text"),
                Mood = JsonHelper.Str(c, "mood").ToUpperInvariant(),
                FactId = JsonHelper.Str(c, "fact_id")
            };
            if (!WorldBible.IsValidWorldChange(change.Type))
            {
                var fixedType = WorldBible.TryCorrect(change.Type, WorldBible.WorldChangeTypes);
                if (fixedType == null) { errors.Add("invalid world change type: " + change.Type); return null; }
                corrections.Add($"change type {change.Type} -> {fixedType}"); change.Type = fixedType;
            }
            if (!string.IsNullOrEmpty(change.Faction) && !WorldBible.IsValidFaction(change.Faction))
            {
                var f = WorldBible.TryCorrect(change.Faction, WorldBible.FactionIds);
                if (f == null) { errors.Add("invalid faction in change: " + change.Faction); return null; }
                corrections.Add($"faction {change.Faction} -> {f}"); change.Faction = f;
            }
            if (!string.IsNullOrEmpty(change.Npc) && change.Npc != WorldBible.PlayerId && !WorldBible.IsValidNpc(change.Npc))
            {
                var n = WorldBible.TryCorrect(change.Npc, WorldBible.NpcIds);
                if (n == null) { errors.Add("invalid npc in change: " + change.Npc); return null; }
                corrections.Add($"npc {change.Npc} -> {n}"); change.Npc = n;
            }
            if (!string.IsNullOrEmpty(change.Location) && !WorldBible.IsValidLocation(change.Location))
            {
                var l = WorldBible.TryCorrect(change.Location, WorldBible.Locations);
                if (l == null) { errors.Add("invalid location in change: " + change.Location); return null; }
                corrections.Add($"location {change.Location} -> {l}"); change.Location = l;
            }
            if (!string.IsNullOrEmpty(change.Item) && !WorldBible.IsValidItem(change.Item))
            {
                var i = WorldBible.TryCorrect(change.Item, WorldBible.ItemTypes);
                if (i == null) { corrections.Add($"item {change.Item} -> EVIDENCE"); change.Item = "EVIDENCE"; }
                else { corrections.Add($"item {change.Item} -> {i}"); change.Item = i; }
            }
            if (!string.IsNullOrEmpty(change.Mood) && !WorldBible.IsValidMood(change.Mood))
            {
                var m = WorldBible.TryCorrect(change.Mood, WorldBible.Moods) ?? "NERVOUS";
                corrections.Add($"mood {change.Mood} -> {m}"); change.Mood = m;
            }
            // Type-specific sanity: SPAWN_ENEMIES needs count and a location.
            if (change.Type == "SPAWN_ENEMIES")
            {
                if (change.Count <= 0) change.Count = 2;
                if (string.IsNullOrEmpty(change.Location)) { errors.Add("SPAWN_ENEMIES without location"); return null; }
            }
            if (change.Type == "REPUTATION" && string.IsNullOrEmpty(change.Faction)) { errors.Add("REPUTATION without faction"); return null; }
            if ((change.Type == "NPC_RELATIONSHIP" || change.Type == "NPC_MOOD" || change.Type == "NPC_LOCATION" || change.Type == "NPC_KNOWLEDGE" || change.Type == "NPC_GOAL" || change.Type == "NPC_HOSTILE" || change.Type == "NPC_DEATH") && string.IsNullOrEmpty(change.Npc))
            { errors.Add(change.Type + " without npc"); return null; }
            return change;
        }

        public static List<WorldChange> ParseChanges(JArray arr, List<string> errors, List<string> corrections)
        {
            var list = new List<WorldChange>();
            if (arr == null) return list;
            foreach (var t in arr)
            {
                var c = ParseChange(t as JObject, errors, corrections);
                if (c != null) list.Add(c);
            }
            return list;
        }

        /// <summary>Parses and validates a quest. Returns null (with errors) when the quest is unusable.</summary>
        public static Quest ParseQuest(JObject q, List<string> errors, List<string> corrections)
        {
            if (q == null) { errors.Add("quest missing"); return null; }
            var quest = new Quest
            {
                QuestId = JsonHelper.Str(q, "quest_id"),
                Title = JsonHelper.Str(q, "title"),
                NarrativeReason = JsonHelper.Str(q, "narrative_reason"),
                GiverNpc = JsonHelper.Str(q, "giver_npc").ToUpperInvariant(),
                TriggerEvent = JsonHelper.Str(q, "trigger_event"),
                Sequential = JsonHelper.Bool(q, "sequential", true),
                IsMain = JsonHelper.Bool(q, "is_main", false)
            };
            if (string.IsNullOrWhiteSpace(quest.Title)) { errors.Add("quest without title"); return null; }
            if (!string.IsNullOrEmpty(quest.GiverNpc) && !WorldBible.IsValidNpc(quest.GiverNpc))
            {
                var n = WorldBible.TryCorrect(quest.GiverNpc, WorldBible.NpcIds);
                if (n == null) { corrections.Add("quest giver cleared: " + quest.GiverNpc); quest.GiverNpc = ""; }
                else quest.GiverNpc = n;
            }
            foreach (var t in JsonHelper.Arr(q, "objectives"))
            {
                var o = t as JObject; if (o == null) continue;
                var obj = new QuestObjective
                {
                    Action = JsonHelper.Str(o, "action").ToUpperInvariant(),
                    Location = JsonHelper.Str(o, "location").ToUpperInvariant(),
                    TargetNpc = JsonHelper.Str(o, "target_npc").ToUpperInvariant(),
                    TargetItem = JsonHelper.Str(o, "target_item").ToUpperInvariant(),
                    Description = JsonHelper.Str(o, "description"),
                    Optional = JsonHelper.Bool(o, "optional"),
                    AvailableFromHour = JsonHelper.Int(o, "available_from_hour", -1),
                    AvailableUntilHour = JsonHelper.Int(o, "available_until_hour", -1)
                };
                if (!WorldBible.IsValidQuestAction(obj.Action))
                {
                    var a = WorldBible.TryCorrect(obj.Action, WorldBible.QuestActions);
                    if (a == null) { errors.Add("invalid quest action: " + obj.Action); continue; }
                    corrections.Add($"action {obj.Action} -> {a}"); obj.Action = a;
                }
                if (!string.IsNullOrEmpty(obj.Location) && !WorldBible.IsValidLocation(obj.Location))
                {
                    var l = WorldBible.TryCorrect(obj.Location, WorldBible.Locations);
                    if (l == null) { errors.Add("invalid objective location: " + obj.Location); continue; }
                    corrections.Add($"location {obj.Location} -> {l}"); obj.Location = l;
                }
                if (!string.IsNullOrEmpty(obj.TargetNpc) && !WorldBible.IsValidNpc(obj.TargetNpc))
                {
                    var n = WorldBible.TryCorrect(obj.TargetNpc, WorldBible.NpcIds);
                    if (n == null) { errors.Add("invalid objective npc: " + obj.TargetNpc); continue; }
                    corrections.Add($"npc {obj.TargetNpc} -> {n}"); obj.TargetNpc = n;
                }
                if (!string.IsNullOrEmpty(obj.TargetItem) && !WorldBible.IsValidItem(obj.TargetItem))
                {
                    var i = WorldBible.TryCorrect(obj.TargetItem, WorldBible.ItemTypes) ?? "EVIDENCE";
                    corrections.Add($"item {obj.TargetItem} -> {i}"); obj.TargetItem = i;
                }
                if (obj.AvailableFromHour < -1 || obj.AvailableFromHour > 23 || obj.AvailableUntilHour < -1 || obj.AvailableUntilHour > 24)
                { obj.AvailableFromHour = -1; obj.AvailableUntilHour = -1; corrections.Add("objective time window cleared"); }
                // Every action needs its anchor: TALK/BRIBE/THREATEN/DELIVER need an npc, the rest need a location.
                bool needsNpc = obj.Action == "TALK" || obj.Action == "BRIBE" || obj.Action == "THREATEN" || obj.Action == "DELIVER";
                if (needsNpc && string.IsNullOrEmpty(obj.TargetNpc)) { errors.Add(obj.Action + " objective without target_npc"); continue; }
                if (!needsNpc && string.IsNullOrEmpty(obj.Location) && string.IsNullOrEmpty(obj.TargetNpc)) { errors.Add(obj.Action + " objective without location"); continue; }
                if (string.IsNullOrWhiteSpace(obj.Description)) obj.Description = DescribeObjective(obj);
                quest.Objectives.Add(obj);
            }
            if (quest.Objectives.Count == 0) { errors.Add("quest has no valid objectives"); return null; }

            foreach (var t in JsonHelper.Arr(q, "relevant_npcs"))
            {
                var id = t.ToString().ToUpperInvariant();
                if (WorldBible.IsValidNpc(id)) quest.RelevantNpcs.Add(id);
            }
            foreach (var t in JsonHelper.Arr(q, "resolutions"))
            {
                var r = t as JObject; if (r == null) continue;
                var res = new QuestResolution
                {
                    Id = JsonHelper.Str(r, "id"),
                    Summary = JsonHelper.Str(r, "summary"),
                    RequiresItem = JsonHelper.Str(r, "requires_item").ToUpperInvariant(),
                    RequiresNpcAlive = JsonHelper.Str(r, "requires_npc_alive").ToUpperInvariant(),
                    OutcomeText = JsonHelper.Str(r, "outcome_text"),
                    EndingId = JsonHelper.Str(r, "ending_id")
                };
                if (string.IsNullOrWhiteSpace(res.Summary)) continue;
                if (!string.IsNullOrEmpty(res.RequiresItem) && !WorldBible.IsValidItem(res.RequiresItem)) res.RequiresItem = WorldBible.TryCorrect(res.RequiresItem, WorldBible.ItemTypes) ?? "";
                if (!string.IsNullOrEmpty(res.RequiresNpcAlive) && !WorldBible.IsValidNpc(res.RequiresNpcAlive)) res.RequiresNpcAlive = WorldBible.TryCorrect(res.RequiresNpcAlive, WorldBible.NpcIds) ?? "";
                res.Consequences = ParseChanges(r["consequences"] as JArray, errors, corrections);
                quest.Resolutions.Add(res);
            }
            foreach (var t in JsonHelper.Arr(q, "failure_conditions"))
            {
                var f = t as JObject; if (f == null) continue;
                var fc = new FailureCondition
                {
                    Type = JsonHelper.Str(f, "type").ToUpperInvariant(),
                    NpcId = JsonHelper.Str(f, "npc_id").ToUpperInvariant(),
                    DeadlineMinute = JsonHelper.Int(f, "deadline_minute", -1),
                    MutationHint = JsonHelper.Str(f, "mutation_hint")
                };
                if (fc.Type != "NPC_DEAD" && fc.Type != "DEADLINE") continue;
                if (fc.Type == "NPC_DEAD" && !WorldBible.IsValidNpc(fc.NpcId)) continue;
                quest.FailureConditions.Add(fc);
            }
            quest.WorldConsequences = ParseChanges(q["world_consequences"] as JArray, errors, corrections);
            return quest;
        }

        public static string DescribeObjective(QuestObjective o)
        {
            string loc = WorldBible.LocationName(o.Location);
            switch (o.Action)
            {
                case "TALK": return $"Talk to {o.TargetNpc}.";
                case "SEARCH": return $"Search the {loc}.";
                case "INVESTIGATE": return $"Investigate the {loc}.";
                case "DELIVER": return $"Deliver the {o.TargetItem.ToLowerInvariant()} to {o.TargetNpc}.";
                case "FIGHT": return $"Deal with the trouble at the {loc}.";
                case "PROTECT": return $"Protect {o.TargetNpc} at the {loc}.";
                case "FOLLOW": return $"Follow the trail to the {loc}.";
                case "ESCAPE": return $"Get away to the {loc}.";
                case "STEAL": return $"Take the {o.TargetItem.ToLowerInvariant()} from the {loc}.";
                case "BRIBE": return $"Pay {o.TargetNpc} for information.";
                case "THREATEN": return $"Lean on {o.TargetNpc}.";
                default: return o.Action + " " + loc;
            }
        }
    }
}
