using System;
using System.Collections.Generic;
using System.Linq;
using Echobound.Core;
using Echobound.NPC;
using Echobound.World;

namespace Echobound.Quests
{
    /// <summary>
    /// Tracks active quests against player actions, and MUTATES quests instead of failing them when the world
    /// changes underneath (an NPC dies, a deadline passes). All logic is deterministic; the AI Director may later
    /// enrich mutated text but the game never waits for it.
    /// </summary>
    public class QuestManager
    {
        private readonly WorldState _state;
        private readonly WorldChangeApplier _applier;
        private readonly Inventory.InventorySystem _inventory;

        public event Action<Quest, string> QuestMutated;

        public QuestManager(WorldState state, WorldChangeApplier applier, Inventory.InventorySystem inventory)
        {
            _state = state; _applier = applier; _inventory = inventory;
            GameEvents.PlayerActed += OnPlayerActed;
            GameEvents.NpcDied += OnNpcDied;
            GameEvents.HourStarted += OnHourStarted;
        }

        public IEnumerable<Quest> All => _state.Quests;
        public IEnumerable<Quest> Active => _state.Quests.Where(q => q.IsActive);
        public IEnumerable<Quest> Completed => _state.Quests.Where(q => q.State == "COMPLETED");
        public IEnumerable<Quest> FailedOrMutated => _state.Quests.Where(q => q.State == "FAILED" || q.State == "MUTATED");

        public Quest MainQuest => _state.Quests.FirstOrDefault(q => q.IsMain && q.IsActive) ?? _state.Quests.FirstOrDefault(q => q.IsActive);

        public string CurrentObjectiveText()
        {
            var q = MainQuest;
            return q == null ? "Explore the town." : q.CurrentObjectiveText();
        }

        public Quest AddQuest(Quest quest, bool notify = true)
        {
            if (quest == null) return null;
            if (string.IsNullOrEmpty(quest.QuestId) || _state.Quests.Any(q => q.QuestId == quest.QuestId))
                quest.QuestId = _state.NewQuestId();
            for (int i = 0; i < quest.Objectives.Count; i++)
                if (string.IsNullOrEmpty(quest.Objectives[i].Id)) quest.Objectives[i].Id = quest.QuestId + "_OBJ" + (i + 1);
            for (int i = 0; i < quest.Resolutions.Count; i++)
                if (string.IsNullOrEmpty(quest.Resolutions[i].Id)) quest.Resolutions[i].Id = quest.QuestId + "_RES" + (i + 1);
            quest.CreatedMinute = _state.ElapsedMinutes;
            if (quest.State != "ACTIVE" && quest.State != "MUTATED") quest.State = "ACTIVE";
            _state.Quests.Add(quest);
            _state.Log("New quest: " + quest.Title, 5);
            if (notify) GameEvents.RaiseNotification("New quest: " + quest.Title);
            GameEvents.RaiseQuestChanged(quest);
            return quest;
        }

        // ------------------------------------------------------------------ objective tracking
        private void OnPlayerActed(PlayerAction a)
        {
            int hour = ((WorldClock.StartMinuteOfDay + _state.ElapsedMinutes) % WorldClock.MinutesPerDay) / 60;
            foreach (var quest in Active.ToList())
            {
                bool changed = false;
                var candidates = quest.Sequential
                    ? new List<QuestObjective> { quest.CurrentObjective() }.Where(o => o != null).Concat(quest.Objectives.Where(o => o.Optional && !o.Completed))
                    : quest.Objectives.Where(o => !o.Completed);
                foreach (var obj in candidates.ToList())
                {
                    if (obj == null || obj.Completed || !obj.IsAvailableAt(hour)) continue;
                    if (Matches(obj, a))
                    {
                        obj.Completed = true;
                        quest.AddHistory($"Done: {obj.Description}");
                        changed = true;
                        GameEvents.RaiseNotification("Objective complete: " + obj.Description);
                    }
                }
                if (changed) OnQuestProgress(quest);
            }
        }

        private static bool Matches(QuestObjective o, PlayerAction a)
        {
            bool npcOk = string.IsNullOrEmpty(o.TargetNpc) || o.TargetNpc == a.TargetNpcId;
            bool locOk = string.IsNullOrEmpty(o.Location) || o.Location == a.LocationId;
            bool itemOk = string.IsNullOrEmpty(o.TargetItem) || (a.Detail != null && a.Detail.ToUpperInvariant().Contains(o.TargetItem));
            switch (o.Action)
            {
                case "TALK": return a.Type == PlayerActionType.TALKED && npcOk;
                case "THREATEN": return a.Type == PlayerActionType.THREATENED && npcOk;
                case "BRIBE": return a.Type == PlayerActionType.BRIBED && npcOk;
                case "SEARCH": return (a.Type == PlayerActionType.SEARCHED || a.Type == PlayerActionType.FOUND_EVIDENCE) && locOk;
                case "INVESTIGATE":
                    return (a.Type == PlayerActionType.FOUND_EVIDENCE && locOk && itemOk)
                        || (!string.IsNullOrEmpty(o.TargetNpc) && a.Type == PlayerActionType.TALKED && npcOk);
                case "STEAL": return (a.Type == PlayerActionType.STOLE || a.Type == PlayerActionType.FOUND_EVIDENCE) && locOk && itemOk;
                case "DELIVER": return (a.Type == PlayerActionType.DELIVERED || a.Type == PlayerActionType.GAVE_ITEM) && npcOk && itemOk;
                case "FOLLOW":
                case "ESCAPE": return a.Type == PlayerActionType.ENTERED_LOCATION && locOk;
                case "FIGHT": return a.Type == PlayerActionType.KILLED_ENEMY && locOk || (a.Type == PlayerActionType.ATTACKED_NPC && npcOk && !string.IsNullOrEmpty(o.TargetNpc));
                case "PROTECT": return a.Type == PlayerActionType.KILLED_ENEMY && locOk;
                default: return false;
            }
        }

        private void OnQuestProgress(Quest quest)
        {
            if (quest.AllRequiredObjectivesDone())
            {
                var available = AvailableResolutions(quest);
                if (available.Count > 0)
                {
                    quest.DecisionPending = true;
                    GameEvents.RaiseQuestChanged(quest);
                    GameEvents.RaiseQuestDecisionReady(quest);
                    return;
                }
                Complete(quest, null);
                return;
            }
            GameEvents.RaiseQuestChanged(quest);
        }

        public List<QuestResolution> AvailableResolutions(Quest quest)
        {
            return quest.Resolutions.Where(r =>
                (string.IsNullOrEmpty(r.RequiresItem) || _inventory.Has(r.RequiresItem)) &&
                (string.IsNullOrEmpty(r.RequiresNpcAlive) || (_state.GetNpc(r.RequiresNpcAlive)?.Alive ?? false))
            ).ToList();
        }

        /// <summary>The player chose how to resolve a quest. Applies consequences and completes it.</summary>
        public void Resolve(Quest quest, QuestResolution resolution)
        {
            if (quest == null || !quest.IsActive) return;
            quest.ChosenResolution = resolution?.Id ?? "";
            quest.DecisionPending = false;
            if (resolution != null)
            {
                if (!string.IsNullOrEmpty(resolution.RequiresItem))
                {
                    var item = _inventory.Find(resolution.RequiresItem);
                    if (item != null) _inventory.Remove(item);
                }
                foreach (var c in resolution.Consequences) _applier.Apply(c);
                quest.AddHistory("Resolved: " + resolution.Summary);
                if (!string.IsNullOrWhiteSpace(resolution.OutcomeText)) _state.Discover(resolution.OutcomeText);
                if (!string.IsNullOrEmpty(resolution.EndingId)) _state.EndingReached = resolution.EndingId;
                GameEvents.RaisePlayerActed(new PlayerAction
                {
                    Type = PlayerActionType.RESOLVED_QUEST, LocationId = _state.Player.Location,
                    Detail = resolution.Summary, Importance = 7, GameMinute = _state.ElapsedMinutes
                });
            }
            Complete(quest, resolution);
        }

        private void Complete(Quest quest, QuestResolution resolution)
        {
            quest.State = "COMPLETED";
            foreach (var c in quest.WorldConsequences) _applier.Apply(c);
            _state.Log("Quest completed: " + quest.Title + (resolution != null ? " (" + resolution.Summary + ")" : ""), 6);
            GameEvents.RaiseNotification("Quest completed: " + quest.Title);
            GameEvents.RaiseQuestChanged(quest);
        }

        public void Fail(Quest quest, string reason)
        {
            quest.State = "FAILED";
            quest.AddHistory("Failed: " + reason);
            _state.Log("Quest failed: " + quest.Title + " - " + reason, 5);
            GameEvents.RaiseNotification("Quest lost: " + quest.Title);
            GameEvents.RaiseQuestChanged(quest);
        }

        // ------------------------------------------------------------------ mutation
        private void OnNpcDied(string npcId, string killer)
        {
            foreach (var quest in Active.ToList())
            {
                bool referenced = quest.GiverNpc == npcId
                    || quest.Objectives.Any(o => !o.Completed && o.TargetNpc == npcId)
                    || quest.FailureConditions.Any(f => f.Type == "NPC_DEAD" && f.NpcId == npcId)
                    || quest.Resolutions.Any(r => r.RequiresNpcAlive == npcId);
                if (referenced) MutateForDeath(quest, npcId, killer);
            }
        }

        private void MutateForDeath(Quest quest, string deadNpcId, string killer)
        {
            var dead = _state.GetNpc(deadNpcId);
            string deadName = dead?.DisplayName ?? deadNpcId;
            string deathLocation = dead?.CurrentLocation ?? "TOWN_SQUARE";
            bool playerKilled = killer == WorldBible.PlayerId;
            var reasons = new List<string>();

            // 1. Objectives that needed the dead NPC are replaced.
            foreach (var obj in quest.Objectives.Where(o => !o.Completed && o.TargetNpc == deadNpcId).ToList())
            {
                int index = quest.Objectives.IndexOf(obj);
                quest.Objectives.Remove(obj);
                QuestObjective replacement;
                if (obj.Action == "TALK" || obj.Action == "INVESTIGATE" || obj.Action == "BRIBE" || obj.Action == "THREATEN")
                {
                    string workplace = dead != null && WorldBible.RoleWorkplace.ContainsKey(dead.Role) ? WorldBible.RoleWorkplace[dead.Role] : deathLocation;
                    replacement = new QuestObjective
                    {
                        Action = "SEARCH", Location = workplace,
                        Description = $"{deadName} is dead. Search the {WorldBible.LocationName(workplace)} for what they knew."
                    };
                }
                else if (obj.Action == "PROTECT")
                {
                    replacement = new QuestObjective
                    {
                        Action = "INVESTIGATE", Location = deathLocation,
                        Description = $"You failed to protect {deadName}. Find out who is responsible at the {WorldBible.LocationName(deathLocation)}."
                    };
                }
                else if (obj.Action == "DELIVER")
                {
                    var successor = FindSuccessor(deadNpcId);
                    replacement = successor != null
                        ? new QuestObjective { Action = "DELIVER", TargetNpc = successor.NpcId, TargetItem = obj.TargetItem, Description = $"{deadName} is dead. Bring the {obj.TargetItem.ToLowerInvariant()} to {successor.DisplayName} instead." }
                        : new QuestObjective { Action = "SEARCH", Location = deathLocation, Description = $"{deadName} is dead. Decide what to do with the {obj.TargetItem.ToLowerInvariant()}." };
                }
                else
                {
                    replacement = new QuestObjective { Action = "SEARCH", Location = deathLocation, Description = $"{deadName} is dead. Search the {WorldBible.LocationName(deathLocation)} for a lead." };
                }
                replacement.Id = quest.QuestId + "_MUT" + (quest.History.Count + 1);
                quest.Objectives.Insert(Math.Min(index, quest.Objectives.Count), replacement);
                reasons.Add($"objective '{obj.Description}' replaced");
            }

            // 2. If the quest giver died, someone believable takes over, or the quest becomes an investigation.
            if (quest.GiverNpc == deadNpcId)
            {
                var successor = FindSuccessor(deadNpcId);
                if (successor != null)
                {
                    quest.GiverNpc = successor.NpcId;
                    quest.Objectives.Insert(0, new QuestObjective
                    {
                        Id = quest.QuestId + "_SUCC", Action = "TALK", TargetNpc = successor.NpcId,
                        Description = $"{deadName} is dead. {successor.DisplayName} may know what they wanted from you."
                    });
                    if (!quest.RelevantNpcs.Contains(successor.NpcId)) quest.RelevantNpcs.Add(successor.NpcId);
                    successor.CurrentGoal = $"Find out what happened to {deadName}";
                    reasons.Add($"{successor.DisplayName} takes over");
                }
                else quest.GiverNpc = "";
            }

            // 3. If someone else killed an important NPC, the question of who did it enters the quest.
            if (!playerKilled && !quest.Objectives.Any(o => !o.Completed && o.Action == "INVESTIGATE" && o.Location == deathLocation))
            {
                quest.Objectives.Add(new QuestObjective
                {
                    Id = quest.QuestId + "_WHO", Action = "INVESTIGATE", Location = deathLocation,
                    Description = $"Discover who killed {deadName}."
                });
                var f = _state.Facts.Add($"{deadName} was killed at the {WorldBible.LocationName(deathLocation)}.", 8, false, deathLocation, _state.ElapsedMinutes, deadNpcId, false, new[] { "murder", "killed", "dead", deadName.ToLowerInvariant() });
                f.PlayerKnows = true;
                reasons.Add("new lead: who killed " + deadName);
            }

            // 4. Resolutions that needed the dead NPC alive disappear; make sure something remains.
            quest.Resolutions.RemoveAll(r => r.RequiresNpcAlive == deadNpcId);
            if (quest.Resolutions.Count == 0)
            {
                quest.Resolutions.Add(new QuestResolution
                {
                    Id = quest.QuestId + "_RES_TRUTH", Summary = "Make what you learned public",
                    Consequences = new List<WorldChange> { WorldChange.Tension(10), WorldChange.Reputation("TOWN_GUARD", 5) },
                    OutcomeText = $"The truth about {deadName} spread through the town."
                });
                quest.Resolutions.Add(new QuestResolution
                {
                    Id = quest.QuestId + "_RES_BURY", Summary = "Keep it to yourself",
                    Consequences = new List<WorldChange> { WorldChange.Tension(-5) },
                    OutcomeText = $"Whatever {deadName} knew died with them."
                });
            }
            quest.FailureConditions.RemoveAll(f => f.Type == "NPC_DEAD" && f.NpcId == deadNpcId);
            if (quest.DecisionPending && AvailableResolutions(quest).Count == 0) quest.DecisionPending = false;

            quest.State = "MUTATED";
            string reason = $"{deadName} died ({(playerKilled ? "killed by you" : "killed by " + (dead?.Killer == null || dead.Killer == "UNKNOWN" ? "someone" : WorldBible.FactionName(dead.Killer)))}): " + string.Join("; ", reasons);
            quest.AddHistory("MUTATED: " + reason);
            _state.Log($"Quest mutated: {quest.Title} - {reason}", 6);
            GameEvents.RaiseNotification("Quest changed: " + quest.Title);
            GameEvents.RaiseQuestChanged(quest);
            QuestMutated?.Invoke(quest, reason);
        }

        /// <summary>Who would plausibly pick up a dead NPC's business: a faction-mate, then anyone with a known relationship.</summary>
        private NpcState FindSuccessor(string deadNpcId)
        {
            var dead = _state.GetNpc(deadNpcId);
            if (dead == null) return null;
            var related = _state.Seed.Relationships
                .Where(r => r.A == deadNpcId || r.B == deadNpcId)
                .Select(r => r.A == deadNpcId ? r.B : r.A)
                .Select(_state.GetNpc).Where(n => n != null && n.Alive).ToList();
            var mate = dead.Faction != WorldBible.NoFaction ? _state.NpcsOfFaction(dead.Faction).FirstOrDefault(n => n.NpcId != deadNpcId) : null;
            return mate ?? related.FirstOrDefault();
        }

        private void OnHourStarted(int hour)
        {
            foreach (var quest in Active.ToList())
            {
                foreach (var fc in quest.FailureConditions.Where(f => f.Type == "DEADLINE" && f.DeadlineMinute >= 0 && _state.ElapsedMinutes >= f.DeadlineMinute).ToList())
                {
                    quest.FailureConditions.Remove(fc);
                    if (string.IsNullOrWhiteSpace(fc.MutationHint)) { Fail(quest, "You were too late."); break; }
                    // Too late: the opportunity is gone; the quest becomes about the consequences.
                    foreach (var o in quest.Objectives.Where(o => !o.Completed)) o.Completed = true;
                    quest.Objectives.Add(new QuestObjective { Id = quest.QuestId + "_LATE", Action = "INVESTIGATE", Location = quest.Locations.FirstOrDefault() ?? "TOWN_SQUARE", Description = fc.MutationHint });
                    quest.State = "MUTATED";
                    quest.AddHistory("MUTATED: too late - " + fc.MutationHint);
                    GameEvents.RaiseNotification("Too late: " + quest.Title);
                    GameEvents.RaiseQuestChanged(quest);
                    QuestMutated?.Invoke(quest, "deadline passed");
                }
            }
        }

        public void Dispose()
        {
            GameEvents.PlayerActed -= OnPlayerActed;
            GameEvents.NpcDied -= OnNpcDied;
            GameEvents.HourStarted -= OnHourStarted;
        }
    }
}
