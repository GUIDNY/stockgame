using System;
using System.Collections.Generic;
using System.Linq;
using Echobound.Core;
using Echobound.NPC;

namespace Echobound.World
{
    /// <summary>
    /// Records player actions as facts, gives them to witnesses, and spreads information through
    /// believable channels over time: witnesses -> faction members, guards -> guard commander,
    /// tavern regulars -> rumors, and rumors -> anyone who visits the tavern.
    /// An NPC never learns something without one of these channels.
    /// </summary>
    public class KnowledgeSystem
    {
        private readonly WorldState _state;
        private readonly SeededRandom _rng;

        public KnowledgeSystem(WorldState state, SeededRandom rng)
        {
            _state = state;
            _rng = rng;
        }

        /// <summary>Turns a player action into a fact, stores memories for every witness and returns the fact.</summary>
        public Fact RecordPlayerAction(PlayerAction action)
        {
            var fact = _state.Facts.Add(action.ToFactSummary(_state.NpcName), action.Importance, false,
                action.LocationId, _state.ElapsedMinutes, action.TargetNpcId, true,
                WorldState.TopicsFrom(action.Detail).Concat(new[] { "stranger", "newcomer" }));
            fact.PlayerKnows = true;

            // Witnesses: the target and every alive NPC at the same location.
            var witnesses = new HashSet<string>(action.Witnesses);
            if (!string.IsNullOrEmpty(action.TargetNpcId)) witnesses.Add(action.TargetNpcId);
            foreach (var npc in _state.NpcsAt(action.LocationId)) witnesses.Add(npc.NpcId);

            foreach (var w in witnesses)
            {
                var npc = _state.GetNpc(w);
                if (npc == null || !npc.Alive) continue;
                bool isTarget = w == action.TargetNpcId;
                npc.Learn(fact.Id);
                npc.Memory.Remember(action.Type.ToString(), action.Importance + (isTarget ? 1 : 0),
                    isTarget ? PersonalSummary(action, npc) : fact.Summary, _state.ElapsedMinutes, true, true);
                GameEvents.RaiseFactLearned(fact.Id, w);
            }
            _state.Log(fact.Summary, action.Importance);
            return fact;
        }

        private string PersonalSummary(PlayerAction a, NpcState me)
        {
            string where = WorldBible.LocationName(a.LocationId);
            switch (a.Type)
            {
                case PlayerActionType.THREATENED: return $"The stranger threatened me at the {where}.";
                case PlayerActionType.BRIBED: return $"The stranger paid me {a.Detail} at the {where}.";
                case PlayerActionType.ACCUSED: return $"The stranger accused me of {a.Detail} at the {where}.";
                case PlayerActionType.LIED: return $"The stranger told me: {a.Detail}. I later realised it was a lie.";
                case PlayerActionType.HELPED: return $"The stranger helped me: {a.Detail}.";
                case PlayerActionType.GAVE_ITEM: return $"The stranger gave me {a.Detail}.";
                case PlayerActionType.ATTACKED_NPC: return $"The stranger attacked me at the {where}!";
                case PlayerActionType.DELIVERED: return $"The stranger delivered {a.Detail} to me.";
                case PlayerActionType.TALKED: return $"The stranger came to talk to me at the {where} about {a.Detail}.";
                default: return a.ToFactSummary(_state.NpcName);
            }
        }

        /// <summary>Gives a fact to an NPC through a named channel, recording a "heard" memory.</summary>
        public bool Tell(NpcState npc, Fact fact, string channel)
        {
            if (npc == null || fact == null || !npc.Alive || npc.Knows(fact.Id)) return false;
            npc.Learn(fact.Id);
            npc.Memory.Remember("HEARD_" + fact.Id, Math.Max(1, fact.Importance - 1),
                $"{fact.Summary} (heard from {channel})", _state.ElapsedMinutes, fact.AboutPlayer, false);
            GameEvents.RaiseFactLearned(fact.Id, npc.NpcId);
            return true;
        }

        /// <summary>Called once per game hour. Spreads recent important facts through believable channels.</summary>
        public void HourlySpread(int hour)
        {
            var recent = _state.Facts.Facts
                .Where(f => f.Importance >= 4 && _state.ElapsedMinutes - f.CreatedMinute <= 24 * 60)
                .ToList();

            foreach (var fact in recent)
            {
                var knowers = _state.AliveNpcs.Where(n => n.Knows(fact.Id)).ToList();
                if (knowers.Count == 0) continue;

                foreach (var knower in knowers)
                {
                    // Secrets are guarded: only same-faction people who are not the subject spread them, rarely.
                    double baseChance = fact.IsSecret ? 0.05 : 0.35;

                    // Channel 1: faction communication.
                    if (knower.Faction != WorldBible.NoFaction)
                    {
                        foreach (var mate in _state.NpcsOfFaction(knower.Faction))
                            if (mate.NpcId != knower.NpcId && _rng.Chance(baseChance))
                                Tell(mate, fact, knower.DisplayName);
                    }

                    // Channel 2: guards report to the commander about crimes and violence.
                    if (fact.AboutPlayer && fact.Importance >= 6 && knower.Faction == "TOWN_GUARD")
                    {
                        var commander = _state.Npcs.FirstOrDefault(n => n.Role == "GUARD_COMMANDER" && n.Alive);
                        if (commander != null) Tell(commander, fact, "a guard report");
                    }

                    // Channel 3: people who share a location gossip (non-secret facts only).
                    if (!fact.IsSecret)
                    {
                        foreach (var other in _state.NpcsAt(knower.CurrentLocation))
                            if (other.NpcId != knower.NpcId && _rng.Chance(0.25))
                                Tell(other, fact, knower.DisplayName);
                    }
                }

                // Channel 4: the tavern turns important public facts into rumors.
                if (!fact.IsSecret && fact.Importance >= 5 && !_state.Rumors.Any(r => r.FactId == fact.Id)
                    && knowers.Any(k => k.CurrentLocation == "TAVERN" || k.Role == "TAVERN_OWNER"))
                {
                    _state.Rumors.Add(new Rumor
                    {
                        Text = "People are whispering: " + fact.Summary, Location = "TAVERN",
                        FactId = fact.Id, Minute = _state.ElapsedMinutes
                    });
                    _state.Log("A rumor started at the tavern: " + fact.Summary, 3);
                }
            }

            // Channel 5: anyone visiting the tavern hears the rumors.
            foreach (var rumor in _state.Rumors.Where(r => !string.IsNullOrEmpty(r.FactId)))
            {
                var fact = _state.Facts.Get(rumor.FactId);
                if (fact == null) continue;
                foreach (var visitor in _state.NpcsAt(rumor.Location))
                    if (_rng.Chance(0.4)) Tell(visitor, fact, "tavern gossip");
            }
        }

        /// <summary>Rumors the player can currently hear at a location, marking them as heard.</summary>
        public List<Rumor> HearRumorsAt(string location)
        {
            var list = _state.Rumors.Where(r => r.Location == location).ToList();
            foreach (var r in list)
            {
                r.HeardByPlayer = true;
                var f = string.IsNullOrEmpty(r.FactId) ? null : _state.Facts.Get(r.FactId);
                if (f != null) f.PlayerKnows = true;
            }
            return list;
        }

        /// <summary>Facts this NPC knows that match the player's question, most important first.</summary>
        public List<Fact> WhatNpcKnowsAbout(NpcState npc, string question, int max = 3)
        {
            var words = WorldState.TopicsFrom(question).ToList();
            var known = npc.Knowledge.Select(_state.Facts.Get).Where(f => f != null).ToList();
            if (words.Count == 0) return known.OrderByDescending(f => f.Importance).Take(max).ToList();
            return known.Select(f => (fact: f, score: f.Topics.Count(t => words.Any(w => t.Contains(w) || w.Contains(t)))))
                .Where(x => x.score > 0)
                .OrderByDescending(x => x.score).ThenByDescending(x => x.fact.Importance)
                .Select(x => x.fact).Take(max).ToList();
        }
    }
}
