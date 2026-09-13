using System.Collections.Generic;
using Echobound.NPC;
using Echobound.Quests;
using Echobound.World;

namespace Echobound.AI
{
    /// <summary>Wrapper so payloads can carry the world rng without exposing it in prompts.</summary>
    public class SeededRandomHolder { public Core.SeededRandom Value; }

    /// <summary>Typed context attached to AIRequest.Payload. Used by the mock provider and by prompt builders.</summary>
    public class WorldGenPayload { public int SeedNumber; }

    public class DialoguePayload
    {
        public WorldState State;
        public NpcState Npc;
        public string Intent = "GREET";
        public string Text = "";
        public string Topic = "";
        public int CoinsOffered;
        public bool HasEvidenceAgainstNpc;
        public Quest QuestWithNpc;
        public List<Fact> RelevantKnownFacts = new List<Fact>();
        public List<Fact> RevealableFacts = new List<Fact>();
        public List<string> RumorsHere = new List<string>();
        public string PlayerLocation = "TOWN_SQUARE";
        public bool IsOpening;
        public SeededRandomHolder Rng;
    }

    public class DirectorEventPayload
    {
        public WorldState State;
        /// <summary>PLAYER_ACTION | NPC_DEATH | AMBIENT | OPPORTUNITY</summary>
        public string Trigger = "AMBIENT";
        public PlayerAction Action;
        public string DeadNpc = "";
        public string Killer = "";
        public int Hour;
    }

    public class QuestMutationPayload
    {
        public WorldState State;
        public Quest Quest;
        public string Reason = "";
    }
}
