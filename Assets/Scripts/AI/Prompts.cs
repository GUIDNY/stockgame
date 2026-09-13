using System.Linq;
using System.Text;
using Echobound.NPC;
using Echobound.Quests;
using Echobound.World;

namespace Echobound.AI
{
    /// <summary>Compact world state summaries for prompts. Never the whole history: only what the model needs now.</summary>
    public static class WorldSummarizer
    {
        public static string Summarize(WorldState s, int maxLogLines = 5)
        {
            var sb = new StringBuilder();
            var clock = new Core.WorldClock(); clock.Reset(s.ElapsedMinutes);
            sb.Append("TIME: ").Append(clock.Format()).Append(clock.IsNight ? " (night)" : " (day)").Append('\n');
            sb.Append("CONFLICT: ").Append(s.Seed.MainConflict).Append('\n');
            sb.Append("MYSTERY: ").Append(s.Seed.CentralMystery).Append('\n');
            sb.Append("TENSION: ").Append(s.WorldTension).Append("/100\n");
            sb.Append("REPUTATION: ").Append(string.Join(", ", s.PlayerReputation.Select(kv => kv.Key + "=" + kv.Value))).Append('\n');
            sb.Append("FACTIONS: ").Append(string.Join("; ", s.Seed.Factions.Select(f => $"{f.Id} ({f.Stance}) allies[{string.Join(",", f.AlliedWith)}] hostile[{string.Join(",", f.HostileTo)}]"))).Append('\n');
            sb.Append("NPCS: ").Append(string.Join("; ", s.Npcs.Select(n => $"{n.NpcId} {n.DisplayName} {n.Role} {n.Faction} {(n.Alive ? "alive@" + n.CurrentLocation : "DEAD")} rel={n.Relationship.Overall}{(n.HostileToPlayer ? " HOSTILE" : "")}"))).Append('\n');
            sb.Append("PLAYER: at ").Append(s.Player.Location).Append(", coins ").Append(s.Player.Coins).Append(", items [").Append(string.Join(",", s.Player.Items.Select(i => i.Type + ":" + i.Label))).Append("]\n");
            sb.Append("ACTIVE QUESTS: ").Append(string.Join("; ", s.Quests.Where(q => q.IsActive).Select(q => $"{q.QuestId} '{q.Title}' next: {q.CurrentObjectiveText()}"))).Append('\n');
            sb.Append("RECENT EVENTS:\n");
            foreach (var e in s.EventLog.OrderByDescending(e => e.Importance).ThenByDescending(e => e.Minute).Take(maxLogLines)) sb.Append("- ").Append(e.Text).Append('\n');
            sb.Append("PLAYER KNOWS: ").Append(string.Join(" | ", s.Facts.KnownByPlayer().OrderByDescending(f => f.Importance).Take(6).Select(f => f.Summary))).Append('\n');
            return sb.ToString();
        }

        public static string SummarizeQuest(Quest q)
        {
            var sb = new StringBuilder();
            sb.Append($"{q.QuestId} '{q.Title}' [{q.State}] giver={q.GiverNpc}\nReason: {q.NarrativeReason}\nObjectives:\n");
            foreach (var o in q.Objectives) sb.Append($"- [{(o.Completed ? "x" : " ")}] {o.Action} {o.Location} {o.TargetNpc} {o.TargetItem}: {o.Description}\n");
            sb.Append("History: ").Append(string.Join(" / ", q.History.Skip(System.Math.Max(0, q.History.Count - 3)))).Append('\n');
            return sb.ToString();
        }
    }

    /// <summary>
    /// Cached prompt templates. System prompts are stable strings (cheap to cache server-side);
    /// per-request user prompts carry only the current situation.
    /// </summary>
    public static class PromptLibrary
    {
        public const string JsonOnly = "Respond with a single JSON object and nothing else. No markdown, no commentary.";

        public static readonly string WorldGenSystem =
            "You are the AI Director of ECHOBOUND, a narrative RPG set in the walled town of Greyhaven, year 2142. " +
            "You create a NEW story for a fixed set of places and character slots. Your job is a fresh main conflict, a hidden antagonist, " +
            "a victim, alliances, secrets, relationships, an opening quest with several resolutions and possible endings. " +
            "Style: morally ambiguous, grounded, no obvious good/evil labels. Keep prose short.\n" +
            "HARD RULES:\n" + WorldBible.ToPromptText() + "\n" +
            "Every NPC slot NPC_01..NPC_10 must appear exactly once with its fixed role. Quest objectives use only VALID_QUEST_ACTIONS. " +
            "TALK/BRIBE/THREATEN/DELIVER objectives need target_npc; other objectives need location. Resolutions use only VALID_WORLD_CHANGE_TYPES. " +
            JsonOnly;

        public static string WorldGenUser(int seedNumber, string example)
        {
            return "Generate a complete world. Seed number for variety: " + seedNumber + ".\n" +
                   "Return JSON with EXACTLY this shape (values are an example; write your own story, different from it):\n" + example;
        }

        public static readonly string DialogueSystem =
            "You voice one NPC in ECHOBOUND, a grounded narrative RPG. Stay strictly in character. " +
            "Reply in 1 to 4 short sentences, game-like, no essays. Never reveal a secret unless trust, fear or a bribe makes it believable, " +
            "and only reveal facts listed as KNOWN (reference them by id in revealed_fact_ids). You cannot know anything not listed. " +
            "React to memories: if the player did something to you or you heard about it, let it colour the reply. " +
            "Set intent_detected to one of: " + string.Join(", ", WorldBible.DialogueIntents) + ". " +
            "mood must be one of: " + string.Join(", ", WorldBible.Moods) + ". relationship_change and fear_change are integers -30..30. " +
            "suggested_options: 2 to 4 short things the player could say next. gives_item only from: " + string.Join(", ", WorldBible.ItemTypes) + " or empty. " +
            JsonOnly;

        public static string DialogueUser(DialoguePayload p)
        {
            var n = p.Npc;
            var sb = new StringBuilder();
            sb.Append("NPC: ").Append(n.DescribeForPrompt(includeSecret: true)).Append('\n');
            sb.Append("RELATIONSHIP WITH PLAYER: ").Append(n.Relationship.Label()).Append($" (trust {n.Relationship.Trust}, fear {n.Relationship.Fear}, respect {n.Relationship.Respect}, hostility {n.Relationship.Hostility})\n");
            sb.Append("MEMORIES:\n").Append(n.Memory.ToPromptDigest()).Append('\n');
            sb.Append("KNOWN FACTS (only these can be revealed):\n");
            foreach (var f in p.RevealableFacts) sb.Append("- ").Append(f.Id).Append(f.IsSecret ? " [secret] " : " ").Append(f.Summary).Append('\n');
            if (p.RumorsHere.Count > 0) sb.Append("RUMORS HEARD HERE: ").Append(string.Join(" | ", p.RumorsHere)).Append('\n');
            if (p.QuestWithNpc != null) sb.Append("PLAYER'S CURRENT BUSINESS WITH YOU: ").Append(p.QuestWithNpc.Title).Append(" - ").Append(p.QuestWithNpc.CurrentObjectiveText()).Append('\n');
            sb.Append("WORLD:\n").Append(WorldSummarizer.Summarize(p.State, 3));
            sb.Append("PLAYER INTENT: ").Append(p.Intent);
            if (p.CoinsOffered > 0) sb.Append(" (offers ").Append(p.CoinsOffered).Append(" coins)");
            if (p.HasEvidenceAgainstNpc) sb.Append(" (player holds evidence against you)");
            sb.Append('\n');
            sb.Append("PLAYER SAYS: \"").Append(p.Text).Append("\"\n");
            sb.Append("Return JSON: {\"npc_line\":\"\",\"mood\":\"\",\"relationship_change\":0,\"fear_change\":0,\"intent_detected\":\"\",\"revealed_fact_ids\":[],\"new_memory_summary\":\"\",\"suggested_options\":[],\"ends_conversation\":false,\"becomes_hostile\":false,\"gives_item\":\"\",\"gives_item_label\":\"\"}");
            return sb.ToString();
        }

        public static readonly string DirectorSystem =
            "You are the AI Director (a Dungeon Master) for ECHOBOUND. You decide what the world does next in response to the player. " +
            "Consequences must be logical, sometimes unexpected, never random. Prefer reactions from factions and NPCs who could plausibly know. " +
            "Do not punish or reward moralistically. Keep the description to 1-2 sentences and player_notification to one short line (or empty if the player would not notice yet).\n" +
            "HARD RULES:\n" + WorldBible.ToPromptText() + "\n" +
            "event_type from VALID_EVENT_TYPES. world_changes: 1 to 6 entries with type from VALID_WORLD_CHANGE_TYPES. " +
            "REPUTATION needs faction+change; NPC_* need npc; SPAWN_ENEMIES needs location+count(1-3)+faction; RUMOR needs text; NPC_KNOWLEDGE needs npc + fact_id (from PLAYER KNOWS/known facts) or text. " +
            "Only NPC_DEATH an NPC if it is truly the consequence of events; never kill the quest's only remaining lead. " +
            JsonOnly;

        public static string DirectorUser(DirectorEventPayload p)
        {
            var sb = new StringBuilder();
            sb.Append(WorldSummarizer.Summarize(p.State));
            sb.Append("TRIGGER: ").Append(p.Trigger).Append('\n');
            if (p.Action != null) sb.Append("PLAYER ACTION: ").Append(p.Action.ToFactSummary(p.State.NpcName)).Append(" (importance ").Append(p.Action.Importance).Append(", witnesses: ").Append(string.Join(",", p.Action.Witnesses)).Append(")\n");
            if (!string.IsNullOrEmpty(p.DeadNpc)) sb.Append("NPC DEATH: ").Append(p.State.NpcName(p.DeadNpc)).Append(" killed by ").Append(p.Killer).Append('\n');
            sb.Append("Decide the world's response now. Return JSON: {\"event_type\":\"\",\"source_faction\":\"\",\"source_npc\":\"\",\"target\":\"PLAYER\",\"location\":\"\",\"urgency\":1,\"description\":\"\",\"player_notification\":\"\",\"world_changes\":[{\"type\":\"\",\"faction\":\"\",\"npc\":\"\",\"location\":\"\",\"item\":\"\",\"change\":0,\"count\":0,\"text\":\"\",\"mood\":\"\",\"fact_id\":\"\"}]}");
            return sb.ToString();
        }

        public static readonly string QuestMutationSystem =
            "You rewrite quest text for ECHOBOUND after the world changed underneath a quest. The mechanics were already decided; " +
            "you only write a new title, a one-sentence narrative_reason, one description per objective (same order, same count) and a one-line player_notification. " +
            "Grounded, short, no melodrama. " + JsonOnly;

        public static string QuestMutationUser(QuestMutationPayload p)
        {
            return "WHAT HAPPENED: " + p.Reason + "\nQUEST NOW:\n" + WorldSummarizer.SummarizeQuest(p.Quest) +
                   "Return JSON: {\"title\":\"\",\"narrative_reason\":\"\",\"objective_descriptions\":[" + string.Join(",", p.Quest.Objectives.Select(o => "\"\"")) + "],\"player_notification\":\"\"}";
        }
    }
}
