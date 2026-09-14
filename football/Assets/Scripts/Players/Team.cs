using System.Collections.Generic;
using UnityEngine;
using StrikerFive.Core;

namespace StrikerFive.Players
{
    /// <summary>A team during the match: definition, side of play, score, players and the controlled player.</summary>
    public class TeamRuntime
    {
        public TeamDef Def;
        public int Side;               // +1 attacks +x, -1 attacks -x
        public int Score;
        public bool IsHuman;
        public readonly List<PlayerAgent> Players = new List<PlayerAgent>();
        public PlayerAgent Goalkeeper;
        public PlayerAgent Controlled;
        public TeamAI AI;

        public Vector3 AttackGoal => new Vector3(PitchGeometry.HalfLength * Side, 0f, 0f);
        public Vector3 OwnGoal => new Vector3(-PitchGeometry.HalfLength * Side, 0f, 0f);

        public PlayerAgent ClosestTo(Vector3 point, bool includeKeeper, PlayerAgent exclude = null)
        {
            PlayerAgent best = null; float bestD = float.MaxValue;
            foreach (var p in Players)
            {
                if (p == exclude || (!includeKeeper && p.Role == Role.Goalkeeper)) continue;
                float d = (p.Position - point).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }
    }
}
