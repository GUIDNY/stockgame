using System;
using System.Collections.Generic;
using Echobound.Core;

namespace Echobound.World
{
    /// <summary>Faction reputation, alliances and price modifiers. Deterministic and local.</summary>
    public class FactionSystem
    {
        private readonly WorldState _state;
        public FactionSystem(WorldState state) { _state = state; }

        public int GetReputation(string factionId)
        {
            return _state.PlayerReputation.TryGetValue(factionId, out var v) ? v : 0;
        }

        public void ChangeReputation(string factionId, int delta, bool propagateToAllies = true)
        {
            if (!WorldBible.IsValidFaction(factionId) || factionId == WorldBible.NoFaction) return;
            int before = GetReputation(factionId);
            int after = Math.Max(-100, Math.Min(100, before + delta));
            _state.PlayerReputation[factionId] = after;
            GameEvents.RaiseReputationChanged(factionId, after, after - before);

            if (!propagateToAllies) return;
            var faction = _state.Seed.GetFaction(factionId);
            if (faction == null) return;
            // Allies are pleased/annoyed by half as much; enemies feel the opposite at a quarter.
            foreach (var ally in faction.AlliedWith) ChangeReputation(ally, delta / 2, false);
            foreach (var enemy in faction.HostileTo) ChangeReputation(enemy, -delta / 4, false);
        }

        public string StandingLabel(string factionId)
        {
            int r = GetReputation(factionId);
            if (r >= 60) return "Trusted";
            if (r >= 25) return "Friendly";
            if (r > -25) return "Neutral";
            if (r > -60) return "Disliked";
            return "Hunted";
        }

        public bool AreAllied(string a, string b)
        {
            var fa = _state.Seed.GetFaction(a);
            return fa != null && fa.AlliedWith.Contains(b);
        }

        public bool AreHostile(string a, string b)
        {
            var fa = _state.Seed.GetFaction(a);
            return fa != null && fa.HostileTo.Contains(b);
        }

        /// <summary>Price multiplier when trading with a member of the given faction.</summary>
        public float PriceMultiplier(string factionId)
        {
            float mod = _state.PriceModifiers.TryGetValue(factionId ?? "", out var m) ? m : 1f;
            int rep = GetReputation(factionId);
            float repMod = 1f - rep / 400f; // +100 rep => 25% discount, -100 => 25% markup
            return Math.Max(0.5f, Math.Min(2f, mod * repMod));
        }

        public Dictionary<string, int> Snapshot() => new Dictionary<string, int>(_state.PlayerReputation);
    }
}
