using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Echobound.World;

namespace Echobound.Quests
{
    /// <summary>One step of a quest expressed with a reusable building block (WorldBible.QuestActions).</summary>
    [Serializable]
    public class QuestObjective
    {
        [JsonProperty("id")] public string Id = "";
        [JsonProperty("action")] public string Action = "TALK";
        [JsonProperty("location")] public string Location = "";
        [JsonProperty("target_npc")] public string TargetNpc = "";
        [JsonProperty("target_item")] public string TargetItem = "";
        [JsonProperty("description")] public string Description = "";
        [JsonProperty("completed")] public bool Completed = false;
        [JsonProperty("optional")] public bool Optional = false;
        /// <summary>Hour of day window (inclusive start, exclusive end) when this objective can be done; -1 = any.</summary>
        [JsonProperty("available_from_hour")] public int AvailableFromHour = -1;
        [JsonProperty("available_until_hour")] public int AvailableUntilHour = -1;

        public bool IsAvailableAt(int hour)
        {
            if (AvailableFromHour < 0 || AvailableUntilHour < 0) return true;
            if (AvailableFromHour <= AvailableUntilHour) return hour >= AvailableFromHour && hour < AvailableUntilHour;
            return hour >= AvailableFromHour || hour < AvailableUntilHour; // wraps midnight
        }
    }

    /// <summary>A way the player can conclude the quest once its objectives are met.</summary>
    [Serializable]
    public class QuestResolution
    {
        [JsonProperty("id")] public string Id = "";
        [JsonProperty("summary")] public string Summary = "";
        [JsonProperty("requires_item")] public string RequiresItem = "";
        [JsonProperty("requires_npc_alive")] public string RequiresNpcAlive = "";
        [JsonProperty("consequences")] public List<WorldChange> Consequences = new List<WorldChange>();
        [JsonProperty("outcome_text")] public string OutcomeText = "";
        [JsonProperty("ending_id")] public string EndingId = "";
    }

    [Serializable]
    public class FailureCondition
    {
        /// <summary>NPC_DEAD or DEADLINE.</summary>
        [JsonProperty("type")] public string Type = "NPC_DEAD";
        [JsonProperty("npc_id")] public string NpcId = "";
        [JsonProperty("deadline_minute")] public int DeadlineMinute = -1;
        [JsonProperty("mutation_hint")] public string MutationHint = "";
    }

    /// <summary>
    /// A dynamic quest assembled from building blocks. Quests mutate instead of failing when the world changes under them.
    /// </summary>
    [Serializable]
    public class Quest
    {
        [JsonProperty("quest_id")] public string QuestId = "";
        [JsonProperty("title")] public string Title = "";
        [JsonProperty("narrative_reason")] public string NarrativeReason = "";
        [JsonProperty("giver_npc")] public string GiverNpc = "";
        [JsonProperty("trigger_event")] public string TriggerEvent = "";
        [JsonProperty("objectives")] public List<QuestObjective> Objectives = new List<QuestObjective>();
        [JsonProperty("sequential")] public bool Sequential = true;
        [JsonProperty("relevant_npcs")] public List<string> RelevantNpcs = new List<string>();
        [JsonProperty("resolutions")] public List<QuestResolution> Resolutions = new List<QuestResolution>();
        [JsonProperty("failure_conditions")] public List<FailureCondition> FailureConditions = new List<FailureCondition>();
        [JsonProperty("world_consequences")] public List<WorldChange> WorldConsequences = new List<WorldChange>();
        [JsonProperty("state")] public string State = "ACTIVE";
        [JsonProperty("history")] public List<string> History = new List<string>();
        [JsonProperty("chosen_resolution")] public string ChosenResolution = "";
        [JsonProperty("is_main")] public bool IsMain = false;
        [JsonProperty("created_minute")] public int CreatedMinute = 0;
        [JsonProperty("decision_pending")] public bool DecisionPending = false;

        [JsonIgnore] public bool IsActive => State == "ACTIVE" || State == "MUTATED";
        [JsonIgnore] public IEnumerable<string> Locations => Objectives.Select(o => o.Location).Where(WorldBible.IsValidLocation).Distinct();

        public QuestObjective CurrentObjective()
        {
            if (!Sequential) return Objectives.FirstOrDefault(o => !o.Completed && !o.Optional);
            return Objectives.FirstOrDefault(o => !o.Completed && !o.Optional);
        }

        public bool AllRequiredObjectivesDone() => Objectives.Where(o => !o.Optional).All(o => o.Completed);

        public string CurrentObjectiveText()
        {
            var o = CurrentObjective();
            if (o != null) return o.Description;
            if (DecisionPending) return "Decide how to resolve: " + Title;
            return State == "ACTIVE" ? Title : State + ": " + Title;
        }

        public void AddHistory(string text)
        {
            History.Add(text);
            if (History.Count > 12) History.RemoveAt(0);
        }
    }
}
