using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Echobound.AI.Schemas;
using Echobound.Core;
using Echobound.NPC;
using Echobound.Quests;
using Echobound.World;

namespace Echobound.AI
{
    /// <summary>
    /// The Dungeon Master. Knows the world state, decides WHEN narrative events happen, asks the AI only for
    /// meaningful situations, validates everything and applies results through WorldChangeApplier.
    /// Deterministic systems (schedules, rumor spread, quest tracking) run without the AI.
    /// </summary>
    public class AIWorldDirector
    {
        private readonly WorldState _state;
        private readonly AIRequestManager _ai;
        private readonly WorldChangeApplier _applier;
        private readonly KnowledgeSystem _knowledge;
        private readonly QuestManager _quests;
        private readonly SeededRandom _rng;

        public int MinMinutesBetweenAmbientEvents = 90;
        public int OpportunityDeliveryMinute = 12;
        private int _lastAmbientMinute = -9999;
        private int _inFlight;
        private bool _disposed;

        public bool OpportunityDelivered { get; private set; }
        public int InFlightRequests => _inFlight;
        public event Action<DirectorEvent> EventApplied;

        public AIWorldDirector(WorldState state, AIRequestManager ai, WorldChangeApplier applier, KnowledgeSystem knowledge, QuestManager quests, SeededRandom rng)
        {
            _state = state; _ai = ai; _applier = applier; _knowledge = knowledge; _quests = quests; _rng = rng;
            GameEvents.PlayerActed += OnPlayerActed;
            GameEvents.NpcDied += OnNpcDied;
            GameEvents.HourStarted += OnHourStarted;
            _quests.QuestMutated += OnQuestMutated;
        }

        public void Dispose()
        {
            _disposed = true;
            GameEvents.PlayerActed -= OnPlayerActed;
            GameEvents.NpcDied -= OnNpcDied;
            GameEvents.HourStarted -= OnHourStarted;
            _quests.QuestMutated -= OnQuestMutated;
        }

        // ------------------------------------------------------------------ world generation
        /// <summary>Creates a brand new world. Uses the AI when available, the deterministic generator otherwise.</summary>
        public static async Task<AIResult<WorldSeed>> GenerateWorldAsync(AIRequestManager ai, int seedNumber)
        {
            var example = JsonConvert.SerializeObject(NarrativeGenerator.Generate(seedNumber ^ 0x5eed), Formatting.None);
            var request = new AIRequest
            {
                Kind = AIRequestKind.WORLD_GENERATION,
                SystemPrompt = PromptLibrary.WorldGenSystem,
                UserPrompt = PromptLibrary.WorldGenUser(seedNumber, example),
                MaxTokens = ai.Config.MaxTokensWorld,
                Temperature = 1.0f,
                Payload = new WorldGenPayload { SeedNumber = seedNumber },
                Priority = 10
            };
            var result = await ai.RequestAsync(request, new WorldSeedValidator(), () => NarrativeGenerator.Generate(seedNumber));
            if (result.Value != null && string.IsNullOrEmpty(result.Value.WorldId)) result.Value.WorldId = "WORLD_" + seedNumber.ToString("X8");
            return result;
        }

        // ------------------------------------------------------------------ reactions
        private void OnPlayerActed(PlayerAction a)
        {
            if (_disposed) return;
            if (a.Type != PlayerActionType.ENTERED_LOCATION && a.Type != PlayerActionType.SLEPT) _knowledge.RecordPlayerAction(a);
            if (a.Importance >= 6 && a.Type != PlayerActionType.KILLED_NPC && a.Type != PlayerActionType.RESOLVED_QUEST)
                _ = RequestConsequenceAsync(new DirectorEventPayload { State = _state, Trigger = "PLAYER_ACTION", Action = a, Hour = Hour });
        }

        private void OnNpcDied(string npcId, string killer)
        {
            if (_disposed) return;
            var npc = _state.GetNpc(npcId);
            // Offers die with the person who made them.
            int dropped = _state.PendingOffers.RemoveAll(o => o.GiverNpc == npcId);
            if (dropped > 0) _state.Log($"{npc?.DisplayName ?? npcId}'s offer died with them.", 4);
            if (npc != null && killer == WorldBible.PlayerId)
            {
                _knowledge.RecordPlayerAction(new PlayerAction { Type = PlayerActionType.KILLED_NPC, TargetNpcId = npcId, LocationId = npc.CurrentLocation, Importance = 10, GameMinute = _state.ElapsedMinutes });
            }
            _ = RequestConsequenceAsync(new DirectorEventPayload { State = _state, Trigger = "NPC_DEATH", DeadNpc = npcId, Killer = killer, Hour = Hour });
        }

        private int Hour => ((WorldClock.StartMinuteOfDay + _state.ElapsedMinutes) % WorldClock.MinutesPerDay) / 60;

        private void OnHourStarted(int hour)
        {
            if (_disposed) return;
            UpdateSchedules(hour);
            _knowledge.HourlySpread(hour);
            ScheduledThreats(hour);
            TryDeliverOpportunity();
            // Ambient events scale with tension; strictly rate limited and never stacked.
            double chance = 0.1 + _state.WorldTension / 100.0 * 0.4;
            if (_inFlight == 0 && _state.ElapsedMinutes - _lastAmbientMinute >= MinMinutesBetweenAmbientEvents && _rng.Chance(chance))
            {
                _lastAmbientMinute = _state.ElapsedMinutes;
                _ = RequestConsequenceAsync(new DirectorEventPayload { State = _state, Trigger = "AMBIENT", Hour = hour });
            }
        }

        /// <summary>Moves NPCs along their schedules (or director overrides). The Unity layer animates the walk.</summary>
        public void UpdateSchedules(int hour)
        {
            foreach (var npc in _state.AliveNpcs)
            {
                if (npc.OverrideUntilMinute >= 0 && _state.ElapsedMinutes >= npc.OverrideUntilMinute) { npc.OverrideLocation = ""; npc.OverrideUntilMinute = -1; }
                // The victim of the main plot stays hidden until found or freed.
                if (npc.NpcId == _state.Seed.VictimNpcId && string.IsNullOrEmpty(npc.OverrideLocation)) continue;
                string target = npc.ScheduledLocation(hour, _state.ElapsedMinutes);
                if (_state.IsLocked(target)) continue;
                if (target != npc.CurrentLocation)
                {
                    npc.CurrentLocation = target;
                    GameEvents.RaiseNpcLocationChanged(npc.NpcId, target);
                }
            }
        }

        /// <summary>PROTECT / FIGHT objectives whose time window opens now get their enemies.</summary>
        private void ScheduledThreats(int hour)
        {
            foreach (var q in _quests.Active)
            {
                var o = q.CurrentObjective();
                if (o == null || (o.Action != "PROTECT" && o.Action != "FIGHT")) continue;
                if (o.AvailableFromHour == hour && WorldBible.IsValidLocation(o.Location))
                {
                    GameEvents.RaiseSpawnEnemies(o.Location, 2, "IRON_HAND");
                    GameEvents.RaiseNotification("Trouble at the " + WorldBible.LocationName(o.Location) + ".");
                }
            }
        }

        /// <summary>The first dynamic opportunity: an NPC seeks the player out with an offer.</summary>
        public void TryDeliverOpportunity(bool force = false)
        {
            if (OpportunityDelivered) return;
            if (!force && _state.ElapsedMinutes < OpportunityDeliveryMinute) return;
            var offer = _state.Seed.OpeningOpportunity;
            if (offer == null) { OpportunityDelivered = true; return; }
            var giver = _state.GetNpc(offer.GiverNpc);
            if (giver == null || !giver.Alive)
            {
                // The giver is dead: the offer dies with them, believably.
                OpportunityDelivered = true;
                _state.Log("The opening opportunity died with " + (giver?.DisplayName ?? offer.GiverNpc), 4);
                return;
            }
            OpportunityDelivered = true;
            _state.PendingOffers.Add(offer);
            var ev = new DirectorEvent
            {
                EventType = "QUEST_OFFER", SourceNpc = giver.NpcId, SourceFaction = giver.Faction, Urgency = 5,
                Location = _state.Player.Location,
                Description = $"{giver.DisplayName} has come looking for the stranger with a proposition.",
                PlayerNotification = $"{giver.DisplayName} is looking for you and wants a word.",
                WorldChanges = new List<WorldChange> { new WorldChange { Type = "NPC_LOCATION", Npc = giver.NpcId, Location = _state.Player.Location, Change = 120 } }
            };
            _applier.ApplyEvent(ev);
            EventApplied?.Invoke(ev);
        }

        /// <summary>Called from dialogue when the player accepts or declines a pending offer.</summary>
        public void ResolveOffer(Quest offer, bool accepted)
        {
            _state.PendingOffers.Remove(offer);
            var giver = _state.GetNpc(offer.GiverNpc);
            if (accepted)
            {
                foreach (var fc in offer.FailureConditions.Where(f => f.Type == "DEADLINE" && f.DeadlineMinute >= 0)) fc.DeadlineMinute += _state.ElapsedMinutes;
                _quests.AddQuest(offer);
                giver?.Relationship.Apply(trust: 10, respect: 5);
            }
            else
            {
                giver?.Relationship.Apply(trust: -5, respect: -10);
                _state.Log($"The stranger declined {giver?.DisplayName}'s offer: {offer.Title}", 4);
                if (giver != null && giver.Faction != WorldBible.NoFaction) _applier.Apply(WorldChange.Reputation(giver.Faction, -5));
            }
        }

        // ------------------------------------------------------------------ AI requests
        public async Task RequestConsequenceAsync(DirectorEventPayload payload)
        {
            if (_disposed) return;
            _inFlight++;
            try
            {
                var request = new AIRequest
                {
                    Kind = AIRequestKind.DIRECTOR_EVENT,
                    SystemPrompt = PromptLibrary.DirectorSystem,
                    UserPrompt = PromptLibrary.DirectorUser(payload),
                    MaxTokens = _ai.Config.MaxTokensEvent,
                    Temperature = 0.9f,
                    Payload = payload,
                    Priority = payload.Trigger == "NPC_DEATH" ? 9 : payload.Trigger == "PLAYER_ACTION" ? 7 : 3
                };
                var result = await _ai.RequestAsync(request, new DirectorEventValidator(), () => FallbackEventFactory.Create(payload, _rng));
                if (_disposed || result.Value == null) return;
                ApplyDirectorEvent(result.Value);
            }
            catch (Exception e) { GameLog.Error("Director consequence failed: " + e); }
            finally { _inFlight--; }
        }

        public void ApplyDirectorEvent(DirectorEvent ev)
        {
            if (ev == null) return;
            if (ev.EventType == "QUIET" && ev.WorldChanges.Count == 0) { _state.Log("[QUIET] " + ev.Description, 1); return; }
            _applier.ApplyEvent(ev);
            if (ev.Quest != null)
            {
                if (!string.IsNullOrEmpty(ev.Quest.GiverNpc) && _state.GetNpc(ev.Quest.GiverNpc)?.Alive == true) _state.PendingOffers.Add(ev.Quest);
                else _quests.AddQuest(ev.Quest);
            }
            EventApplied?.Invoke(ev);
        }

        private void OnQuestMutated(Quest quest, string reason)
        {
            if (_disposed) return;
            _ = NarrateMutationAsync(quest, reason);
        }

        private async Task NarrateMutationAsync(Quest quest, string reason)
        {
            var payload = new QuestMutationPayload { State = _state, Quest = quest, Reason = reason };
            var request = new AIRequest
            {
                Kind = AIRequestKind.QUEST_MUTATION, SystemPrompt = PromptLibrary.QuestMutationSystem,
                UserPrompt = PromptLibrary.QuestMutationUser(payload), MaxTokens = 400, Temperature = 0.7f, Payload = payload, Priority = 6
            };
            var result = await _ai.RequestAsync(request, new QuestMutationValidator(), () => MockQuestText.Build(payload));
            if (_disposed || result.Value == null || !quest.IsActive) return;
            var t = result.Value;
            if (!string.IsNullOrWhiteSpace(t.Title)) quest.Title = t.Title;
            if (!string.IsNullOrWhiteSpace(t.NarrativeReason)) quest.NarrativeReason = t.NarrativeReason;
            if (t.ObjectiveDescriptions.Count == quest.Objectives.Count)
                for (int i = 0; i < quest.Objectives.Count; i++)
                    if (!string.IsNullOrWhiteSpace(t.ObjectiveDescriptions[i])) quest.Objectives[i].Description = t.ObjectiveDescriptions[i];
            if (!string.IsNullOrWhiteSpace(t.PlayerNotification)) GameEvents.RaiseNotification(t.PlayerNotification);
            GameEvents.RaiseQuestChanged(quest);
        }

        // ------------------------------------------------------------------ debug controls
        public void DebugGenerateEvent() => _ = RequestConsequenceAsync(new DirectorEventPayload { State = _state, Trigger = "AMBIENT", Hour = Hour });

        public void DebugTriggerRumor(string text)
        {
            _applier.ApplyEvent(new DirectorEvent { EventType = "RUMOR_SPREAD", Location = "TAVERN", Urgency = 2, Description = "Debug rumor", WorldChanges = new List<WorldChange> { WorldChange.Rumor(text) } });
        }

        public void DebugKillNpc(string npcId)
        {
            var npc = _state.GetNpc(npcId);
            if (npc != null) _applier.KillNpc(npc, "UNKNOWN");
        }
    }
}
