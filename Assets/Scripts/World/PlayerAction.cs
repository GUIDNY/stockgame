using System;
using System.Collections.Generic;

namespace Echobound.World
{
    /// <summary>Kinds of player actions the simulation records. These feed facts, memories and the director.</summary>
    public enum PlayerActionType
    {
        TALKED, THREATENED, BRIBED, ACCUSED, LIED, HELPED, GAVE_ITEM,
        SEARCHED, FOUND_EVIDENCE, STOLE, ATTACKED_NPC, KILLED_NPC, KILLED_ENEMY,
        DELIVERED, ENTERED_LOCATION, RESOLVED_QUEST, SLEPT
    }

    /// <summary>A single recorded player action with enough context for witnesses and the AI director.</summary>
    [Serializable]
    public class PlayerAction
    {
        public PlayerActionType Type;
        public string TargetNpcId;
        public string LocationId;
        public string Detail;
        public int Importance = 3; // 1..10
        public int GameMinute;
        public List<string> Witnesses = new List<string>();

        public string ToFactSummary(Func<string, string> npcName)
        {
            string target = string.IsNullOrEmpty(TargetNpcId) ? "" : npcName(TargetNpcId);
            string where = WorldBible.LocationName(LocationId);
            switch (Type)
            {
                case PlayerActionType.THREATENED: return $"The stranger threatened {target} at the {where}.";
                case PlayerActionType.BRIBED: return $"The stranger paid {target} for information at the {where}.";
                case PlayerActionType.ACCUSED: return $"The stranger accused {target} openly at the {where}.";
                case PlayerActionType.LIED: return $"The stranger lied to {target} at the {where}.";
                case PlayerActionType.HELPED: return $"The stranger helped {target} at the {where}.";
                case PlayerActionType.GAVE_ITEM: return $"The stranger gave {target} {Detail} at the {where}.";
                case PlayerActionType.SEARCHED: return $"The stranger was seen searching the {where}.";
                case PlayerActionType.FOUND_EVIDENCE: return $"The stranger found {Detail} at the {where}.";
                case PlayerActionType.STOLE: return $"The stranger stole {Detail} from the {where}.";
                case PlayerActionType.ATTACKED_NPC: return $"The stranger attacked {target} at the {where}.";
                case PlayerActionType.KILLED_NPC: return $"The stranger killed {target} at the {where}.";
                case PlayerActionType.KILLED_ENEMY: return $"The stranger fought off {Detail} at the {where}.";
                case PlayerActionType.DELIVERED: return $"The stranger delivered {Detail} to {target}.";
                case PlayerActionType.RESOLVED_QUEST: return $"The stranger {Detail}.";
                case PlayerActionType.TALKED: return $"The stranger spoke with {target} at the {where}.";
                default: return $"The stranger did something ({Type}) at the {where}.";
            }
        }
    }
}
