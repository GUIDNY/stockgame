using System;
using System.Linq;
using Echobound.Core;
using Echobound.NPC;

namespace Echobound.World
{
    /// <summary>
    /// The ONLY place where validated AI output touches world state. Each WorldChange is applied
    /// deterministically; unknown types are ignored (they should have been rejected by validation already).
    /// </summary>
    public class WorldChangeApplier
    {
        private readonly WorldState _state;
        private readonly FactionSystem _factions;
        private readonly KnowledgeSystem _knowledge;

        public WorldChangeApplier(WorldState state, FactionSystem factions, KnowledgeSystem knowledge)
        {
            _state = state; _factions = factions; _knowledge = knowledge;
        }

        public void ApplyEvent(DirectorEvent ev)
        {
            if (ev == null) return;
            if (string.IsNullOrEmpty(ev.EventId)) ev.EventId = _state.NewEventId();
            ev.CreatedMinute = _state.ElapsedMinutes;
            foreach (var change in ev.WorldChanges) Apply(change, ev);
            _state.AppliedEvents.Add(ev);
            if (_state.AppliedEvents.Count > 60) _state.AppliedEvents.RemoveAt(0);
            _state.Log($"[{ev.EventType}] {ev.Description}", Math.Max(3, ev.Urgency));
            if (!string.IsNullOrWhiteSpace(ev.PlayerNotification)) GameEvents.RaiseNotification(ev.PlayerNotification);
            GameEvents.RaiseDirectorEventApplied(ev);
        }

        public void Apply(WorldChange c, DirectorEvent source = null)
        {
            if (c == null) return;
            NpcState npc = string.IsNullOrEmpty(c.Npc) ? null : _state.GetNpc(c.Npc);
            switch (c.Type)
            {
                case "REPUTATION":
                    _factions.ChangeReputation(c.Faction, c.Change);
                    break;
                case "NPC_RELATIONSHIP":
                    npc?.Relationship.ApplyOverall(c.Change);
                    break;
                case "NPC_MOOD":
                    if (npc != null && WorldBible.IsValidMood(c.Mood)) npc.Mood = c.Mood;
                    break;
                case "NPC_LOCATION":
                    if (npc != null && WorldBible.IsValidLocation(c.Location))
                    {
                        npc.OverrideLocation = c.Location;
                        npc.OverrideUntilMinute = _state.ElapsedMinutes + Math.Max(60, c.Change > 0 ? c.Change : 180);
                        npc.CurrentLocation = c.Location;
                        GameEvents.RaiseNpcLocationChanged(npc.NpcId, c.Location);
                    }
                    break;
                case "NPC_KNOWLEDGE":
                    if (npc != null)
                    {
                        var fact = _state.Facts.Get(c.FactId);
                        if (fact == null && !string.IsNullOrWhiteSpace(c.Text))
                            fact = _state.Facts.Add(c.Text, 5, false, c.Location, _state.ElapsedMinutes, topics: WorldState.TopicsFrom(c.Text));
                        if (fact != null) _knowledge.Tell(npc, fact, source?.SourceNpc is string s && !string.IsNullOrEmpty(s) ? _state.NpcName(s) : "someone");
                    }
                    break;
                case "NPC_GOAL":
                    if (npc != null && !string.IsNullOrWhiteSpace(c.Text)) npc.CurrentGoal = c.Text;
                    break;
                case "NPC_HOSTILE":
                    if (npc != null) { npc.HostileToPlayer = true; npc.Relationship.Apply(hostility: 40, trust: -30); }
                    break;
                case "NPC_DEATH":
                    if (npc != null && npc.Alive) KillNpc(npc, string.IsNullOrEmpty(c.Faction) ? "UNKNOWN" : c.Faction);
                    break;
                case "SPAWN_ENEMIES":
                    if (WorldBible.IsValidLocation(c.Location))
                        GameEvents.RaiseSpawnEnemies(c.Location, Math.Max(1, Math.Min(4, c.Count)), c.Faction);
                    break;
                case "SPAWN_ITEM":
                    if (WorldBible.IsValidLocation(c.Location) && WorldBible.IsValidItem(c.Item))
                        GameEvents.RaiseSpawnItem(c.Location, c.Item, string.IsNullOrWhiteSpace(c.Text) ? c.Item : c.Text);
                    break;
                case "TENSION":
                    _state.WorldTension = Math.Max(0, Math.Min(100, _state.WorldTension + c.Change));
                    break;
                case "RUMOR":
                    if (!string.IsNullOrWhiteSpace(c.Text))
                    {
                        var f = _state.Facts.Add(c.Text, 4, false, c.Location, _state.ElapsedMinutes, topics: WorldState.TopicsFrom(c.Text));
                        _state.Rumors.Add(new Rumor { Text = c.Text, Location = WorldBible.IsValidLocation(c.Location) ? c.Location : "TAVERN", FactId = f.Id, Minute = _state.ElapsedMinutes });
                    }
                    break;
                case "LOCK_LOCATION":
                    if (WorldBible.IsValidLocation(c.Location) && !_state.LockedLocations.Contains(c.Location)) _state.LockedLocations.Add(c.Location);
                    break;
                case "UNLOCK_LOCATION":
                    _state.LockedLocations.Remove(c.Location);
                    break;
                case "PRICE_MODIFIER":
                    if (WorldBible.IsValidFaction(c.Faction))
                        _state.PriceModifiers[c.Faction] = Math.Max(0.5f, Math.Min(2f, 1f + c.Change / 100f));
                    break;
                default:
                    GameLog.Warn("WorldChangeApplier: ignored unknown change type " + c.Type);
                    break;
            }
        }

        /// <summary>Marks an NPC dead and raises the death event. Both player kills and director kills go through here.</summary>
        public void KillNpc(NpcState npc, string killer)
        {
            if (npc == null || !npc.Alive) return;
            npc.Alive = false;
            npc.Health = 0;
            npc.Killer = killer;
            npc.DeathMinute = _state.ElapsedMinutes;
            _state.Log($"{npc.DisplayName} died. Killer: {killer}", 9);
            GameEvents.RaiseNpcDied(npc.NpcId, killer);
        }
    }
}
