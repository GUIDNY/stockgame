using System;
using System.Collections.Generic;
using System.Linq;
using Echobound.Core;
using Echobound.Dialogue;
using Echobound.NPC;
using Echobound.World;

namespace Echobound.AI
{
    /// <summary>
    /// Deterministic NPC reply builder. Used by MockAIProvider and as the fallback when the LLM fails.
    /// It is deliberately simple, but it respects memory, knowledge, relationship and personality so that
    /// the "the game remembered that" moment works offline.
    /// </summary>
    public static class MockDialogue
    {
        private static readonly string[] BraveRoles = { "GUARD_COMMANDER", "CRIMINAL", "FACTION_LEADER", "SMUGGLER" };

        public static DialogueResponse Build(DialoguePayload p, SeededRandom rng)
        {
            var npc = p.Npc;
            var r = new DialogueResponse { Mood = npc.Mood, IntentDetected = p.Intent };
            string intent = p.Intent == "FREE_TEXT" ? Classify(p.Text, out p.Topic) : p.Intent;
            if (p.Intent == "FREE_TEXT" && string.IsNullOrEmpty(p.Topic)) p.Topic = p.Text;
            r.IntentDetected = intent;
            var parts = new List<string>();

            if (p.IsOpening) parts.Add(Greeting(p, rng));
            var memory = npc.Memory.MostImportantAboutPlayer();
            if (p.IsOpening && memory != null && memory.Importance >= 5) parts.Add(MemoryCallback(memory, npc));

            switch (intent)
            {
                case "GREET":
                    if (!p.IsOpening) parts.Add(rng.Pick(new[] { "Well?", "You were saying?", "Go on." }));
                    break;
                case "ASK_TOPIC": parts.Add(AnswerTopic(p, r, rng)); break;
                case "ASK_RUMORS": parts.Add(AnswerRumors(p, r, rng)); break;
                case "THREATEN": parts.Add(AnswerThreat(p, r, rng)); break;
                case "BRIBE": parts.Add(AnswerBribe(p, r, rng)); break;
                case "ACCUSE": parts.Add(AnswerAccusation(p, r, rng)); break;
                case "GIVE_ITEM": parts.Add(rng.Pick(new[] { "I'll take that. Thank you.", "Good. I owe you one.", "That changes things. My thanks." })); r.RelationshipChange = 10; break;
                case "OFFER_HELP":
                    parts.Add(p.QuestWithNpc != null ? $"Then you know what I need: {p.QuestWithNpc.CurrentObjectiveText()}" : rng.Pick(new[] { "Keep your eyes open. That is help enough.", "Careful who you say that to.", "Help? Start by not making enemies here." }));
                    r.RelationshipChange = 5; break;
                case "LIE": parts.Add(rng.Pick(new[] { "If you say so.", "Hm. I'll remember you said that.", "Interesting. We'll see." })); break;
                case "LEAVE": parts.Add(rng.Pick(new[] { "Go on, then.", "Mind the dark.", "Come back if you learn something." })); r.EndsConversation = true; break;
                default: parts.Add(rng.Pick(new[] { "Not sure what you mean by that.", "Say it plainly.", "I don't follow." })); break;
            }
            r.NpcLine = string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s)));
            r.SuggestedOptions = Suggestions(p, rng);
            if (npc.Relationship.Hostility + Math.Max(0, -r.RelationshipChange) >= 70 && Array.IndexOf(BraveRoles, npc.Role) >= 0) r.BecomesHostile = true;
            return r;
        }

        private static string Greeting(DialoguePayload p, SeededRandom rng)
        {
            var n = p.Npc;
            string label = n.Relationship.Label();
            if (n.HostileToPlayer) return rng.Pick(new[] { "You have some nerve showing your face.", "Say what you came to say and get out." });
            if (!n.MetPlayer) return rng.Pick(new[] { $"You're the newcomer. Everyone's talking about you. I'm {n.DisplayName}.", $"Don't know you. {n.DisplayName}. What do you want?", $"A new face. {n.DisplayName}. Keep your hands where I can see them." });
            switch (label)
            {
                case "Loyal": case "Friendly": return rng.Pick(new[] { "Good to see you again.", "Back again? Sit down.", "Ah, it's you. What's the news?" });
                case "Intimidated": return rng.Pick(new[] { "You again. I... what do you need?", "Please, I told you everything I know." });
                case "Cold": case "Hateful": return rng.Pick(new[] { "You. Make it quick.", "I've nothing to say to you.", "Thought I'd made myself clear." });
                default: return rng.Pick(new[] { "Back again.", "What is it this time?", "Yes?" });
            }
        }

        private static string MemoryCallback(MemoryEntry m, NpcState npc)
        {
            switch (m.Event)
            {
                case "THREATENED": return m.Witnessed ? "I haven't forgotten how you spoke to me." : "";
                case "BRIBED": return "Your coin was good last time.";
                case "ATTACKED_NPC": return m.Witnessed ? "You raised a hand to me. I remember that." : "";
                case "KILLED_NPC": return m.Witnessed ? "I saw what you did. Don't think I didn't." : "I heard what you did. The whole town has.";
                case "ACCUSED": return "Still throwing accusations around?";
                case "HELPED": return "You did right by me before. I remember.";
                default:
                    if (m.Event.StartsWith("HEARD_")) return "I heard something about you. " + Shorten(m.Summary, 90);
                    return "";
            }
        }

        private static string AnswerTopic(DialoguePayload p, DialogueResponse r, SeededRandom rng)
        {
            var npc = p.Npc;
            var facts = p.RelevantKnownFacts;
            if (facts.Count == 0)
            {
                if (npc.Secret.Length > 0 && Overlaps(p.Topic, npc.Secret)) { r.Mood = "SUSPICIOUS"; r.RelationshipChange = -5; return rng.Pick(new[] { "Why are you asking me that?", "I don't know anything about that. Who told you to ask?", "That's not a thing I talk about." }); }
                return rng.Pick(new[] { "Couldn't tell you.", "I don't know anything about that.", "Ask at the tavern. People talk there.", "Not my business, and it shouldn't be yours." });
            }
            var chosen = new List<Fact>();
            foreach (var f in facts)
            {
                bool aboutSelf = f.Id == "TRUTH_" + npc.NpcId || (f.Id == "TRUTH_CORE" && (npc.NpcId == p.State.Seed.VillainNpcId)) || f.Id == "TRUTH_ACCOMPLICE" && f.Summary.StartsWith(npc.DisplayName);
                if (!f.IsSecret) { chosen.Add(f); continue; }
                if (aboutSelf) { if (npc.Relationship.Fear >= 70) chosen.Add(f); continue; }
                if (npc.Relationship.Trust >= 35 || npc.Relationship.Fear >= 50 || npc.Relationship.Overall >= 30) chosen.Add(f);
            }
            if (chosen.Count == 0)
            {
                r.Mood = "NERVOUS";
                return rng.Pick(new[] { "I might know something. I don't know you well enough to say.", "People who talk about that stop being seen around here.", "Not for free, and not for a stranger." });
            }
            var f0 = chosen.OrderByDescending(f => f.Importance).First();
            r.RevealedFactIds.Add(f0.Id);
            r.RelationshipChange = 2;
            string prefix = f0.IsSecret ? rng.Pick(new[] { "Keep this between us. ", "You didn't hear it from me. ", "Fine. " }) : rng.Pick(new[] { "", "Everyone knows this: ", "Well, " });
            return prefix + f0.Summary;
        }

        private static string AnswerRumors(DialoguePayload p, DialogueResponse r, SeededRandom rng)
        {
            var npc = p.Npc;
            if (p.RumorsHere.Count > 0) return rng.Pick(new[] { "You hear things in here. ", "Rumor has it: ", "" }) + rng.Pick(p.RumorsHere);
            var known = npc.Knowledge.Select(p.State.Facts.Get).Where(f => f != null && !f.IsSecret).OrderByDescending(f => f.CreatedMinute).ThenByDescending(f => f.Importance).FirstOrDefault();
            if (known == null) return rng.Pick(new[] { "Nothing new. Which is its own kind of news.", "Quiet. Too quiet, some say." });
            r.RevealedFactIds.Add(known.Id);
            return rng.Pick(new[] { "Only what everyone says: ", "Here's one: ", "" }) + known.Summary;
        }

        private static string AnswerThreat(DialoguePayload p, DialogueResponse r, SeededRandom rng)
        {
            var npc = p.Npc;
            bool brave = Array.IndexOf(BraveRoles, npc.Role) >= 0 || npc.Personality.Contains("proud") || npc.Personality.Contains("stubborn");
            if (brave)
            {
                r.FearChange = 5; r.RelationshipChange = -20; r.Mood = "ANGRY";
                return rng.Pick(new[] { "Try it. See what happens.", "You're new here, so I'll say it once: don't.", "People who threaten me end up in the forest." });
            }
            r.FearChange = 25; r.RelationshipChange = -15; r.Mood = "AFRAID";
            int fearAfter = npc.Relationship.Fear + 25;
            var secret = npc.Knowledge.Select(p.State.Facts.Get).Where(f => f != null && f.IsSecret && f.Id != "TRUTH_" + npc.NpcId).OrderByDescending(f => f.Importance).FirstOrDefault();
            if (fearAfter >= 50 && secret != null)
            {
                r.RevealedFactIds.Add(secret.Id);
                return rng.Pick(new[] { "All right! All right. ", "Don't. Please. ", "You want to know? " }) + secret.Summary;
            }
            return rng.Pick(new[] { "I don't want trouble. I don't know anything, I swear.", "Please. I have a family.", "You'd hurt me? For what?" });
        }

        private static string AnswerBribe(DialoguePayload p, DialogueResponse r, SeededRandom rng)
        {
            var npc = p.Npc;
            bool refuses = (npc.Role == "PRIEST" || (npc.Role == "GUARD_COMMANDER" && !npc.Personality.Contains("greedy"))) && !npc.Personality.Contains("greedy");
            if (refuses || p.CoinsOffered <= 0)
            {
                r.RelationshipChange = -10; r.Mood = "ANGRY";
                return rng.Pick(new[] { "Keep your coin. I'm not for sale.", "Put that away before someone sees.", "You insult me." });
            }
            r.RelationshipChange = 10; r.Mood = "FRIENDLY";
            var secret = npc.Knowledge.Select(p.State.Facts.Get).Where(f => f != null && f.IsSecret && f.Id != "TRUTH_" + npc.NpcId && !(npc.NpcId == p.State.Seed.VillainNpcId && f.Id == "TRUTH_CORE")).OrderByDescending(f => f.Importance).FirstOrDefault()
                         ?? npc.Knowledge.Select(p.State.Facts.Get).Where(f => f != null).OrderByDescending(f => f.Importance).FirstOrDefault();
            if (secret == null) return "Thanks for the coin. I've nothing worth selling, but I'll remember you paid.";
            r.RevealedFactIds.Add(secret.Id);
            return rng.Pick(new[] { "Coin talks. ", "Since you asked so nicely: ", "This buys you one thing. " }) + secret.Summary;
        }

        private static string AnswerAccusation(DialoguePayload p, DialogueResponse r, SeededRandom rng)
        {
            var npc = p.Npc;
            bool guilty = npc.NpcId == p.State.Seed.VillainNpcId || npc.SecretIsCrime;
            if (p.HasEvidenceAgainstNpc && guilty)
            {
                r.FearChange = 20; r.RelationshipChange = -25; r.Mood = npc.Personality.Contains("anxious") ? "AFRAID" : "ANGRY";
                if (npc.NpcId == p.State.Seed.VillainNpcId && !npc.Personality.Contains("anxious")) r.BecomesHostile = true;
                return rng.Pick(new[] { "Where did you get that?", "You should have burned that.", "You don't understand what you're holding." });
            }
            r.RelationshipChange = -12; r.Mood = "ANGRY";
            return rng.Pick(new[] { "You have nothing. Get out.", "Say that in the square and see who believes a stranger.", "Careful. Accusations have weight here." });
        }

        private static List<string> Suggestions(DialoguePayload p, SeededRandom rng)
        {
            var list = new List<string>();
            var victim = p.State.GetNpc(p.State.Seed.VictimNpcId);
            if (victim != null) list.Add($"What do you know about {victim.DisplayName}?");
            list.Add("What's really going on in this town?");
            if (p.Npc.Role == "TAVERN_OWNER") list.Add("Who was here the night it happened?");
            return list.Take(3).ToList();
        }

        public static string Classify(string text, out string topic)
        {
            topic = text ?? "";
            string t = (text ?? "").ToLowerInvariant();
            if (t.Contains("kill") || t.Contains("hurt") || t.Contains("or else") || t.Contains("break your")) return "THREATEN";
            if (t.Contains("coin") || t.Contains("pay") || t.Contains("gold") || t.Contains("money")) return "BRIBE";
            if (t.Contains("rumor") || t.Contains("rumour") || t.Contains("news") || t.Contains("heard anything") || t.Contains("gossip")) return "ASK_RUMORS";
            if (t.Contains("you did") || t.Contains("murder") || t.Contains("you killed") || t.Contains("liar") || t.Contains("it was you")) return "ACCUSE";
            if (t.Contains("help you") || t.Contains("can i help") || t.Contains("let me help")) return "OFFER_HELP";
            if (t.Contains("bye") || t.Contains("farewell") || t == "leave") return "LEAVE";
            if (t.Length < 8 && (t.Contains("hi") || t.Contains("hello") || t.Contains("hey"))) return "GREET";
            return "ASK_TOPIC";
        }

        private static bool Overlaps(string a, string b)
        {
            var wa = new HashSet<string>(WorldState.TopicsFrom(a));
            return WorldState.TopicsFrom(b).Any(wa.Contains);
        }

        private static string Shorten(string s, int max) => s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }

    /// <summary>Deterministic quest text after a mutation (fallback for the LLM narration).</summary>
    public static class MockQuestText
    {
        public static Schemas.QuestMutationText Build(QuestMutationPayload p)
        {
            var q = p.Quest;
            var t = new Schemas.QuestMutationText
            {
                Title = q.Title.Contains("(changed)") ? q.Title : q.Title + " (changed)",
                NarrativeReason = q.NarrativeReason + " " + p.Reason,
                PlayerNotification = "The situation has changed: " + p.Reason
            };
            foreach (var o in q.Objectives) t.ObjectiveDescriptions.Add(o.Description);
            return t;
        }
    }
}
