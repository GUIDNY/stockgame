using System;
using System.Collections.Generic;
using System.Linq;
using Echobound.Core;
using Echobound.Quests;
using Echobound.World;

namespace Echobound.AI
{
    /// <summary>
    /// Deterministic, template-driven world generator. Given a seed number it produces a complete, valid WorldSeed:
    /// conflict archetype, villain, victim, faction alliances, personalities, secrets, relationships, leads,
    /// an opening quest with several resolutions, an opening opportunity and possible endings.
    /// Used by MockAIProvider (offline play) and as the fallback when the LLM world generation fails.
    /// Everything it emits is drawn from the World Bible.
    /// </summary>
    public static class NarrativeGenerator
    {
        private static readonly string[] Names =
        {
            "Mara Vell", "Tobias Reed", "Ysolde Kain", "Bram Holt", "Sefa Ondine", "Corin Ashby", "Nell Draper",
            "Osric Vane", "Petra Lind", "Jorah Quill", "Ilse Marrow", "Dain Corvo", "Wren Tallis", "Hale Brackett",
            "Sabine Roux", "Quen Marsh", "Edda Sorrel", "Lucan Pike", "Tamsin Grey", "Rook Adler"
        };

        private static readonly string[] Personalities =
        {
            "warm but evasive", "blunt and impatient", "pious and watchful", "cheerful, talks too much",
            "cold and precise", "anxious, eager to please", "proud, easily insulted", "quiet and observant",
            "charming and manipulative", "bitter and tired", "loyal to a fault", "greedy but honest about it",
            "dry humour, hides fear behind jokes", "stubborn, distrusts outsiders"
        };

        private static readonly string[] SpeechStyles =
        {
            "short clipped sentences", "formal and old-fashioned", "slang and jokes", "soft-spoken, trails off",
            "military, no wasted words", "sermon-like, fond of metaphors", "sarcastic", "nervous, repeats himself",
            "warm, uses your name a lot", "cold, answers questions with questions"
        };

        private class Ctx
        {
            public SeededRandom Rng;
            public WorldSeed Seed;
            public Dictionary<string, NpcSeed> Npc = new Dictionary<string, NpcSeed>();
            public string Archetype;
            public string Villain;
            public string Accomplice;
            public string Victim;
            public string Ally;
            public string Contact;
            public string Witness;
            public string EvidenceLocation;
            public string VictimLocation;
            public bool VictimDead;
            public string Town = "Greyhaven";
            public string N(string id) => Npc[id].DisplayName;
            public string ById(string role) => Npc.Values.First(n => n.Role == role).NpcId;
        }

        public static WorldSeed Generate(int seedNumber)
        {
            var rng = new SeededRandom(seedNumber);
            var ctx = new Ctx { Rng = rng, Seed = new WorldSeed { WorldId = "WORLD_" + seedNumber.ToString("X8"), WorldTension = rng.Next(15, 45) } };
            var seed = ctx.Seed;

            // 1. People: fixed slots, random identities.
            var names = rng.Shuffle(Names);
            int ni = 0;
            foreach (var id in WorldBible.NpcIds)
            {
                var role = WorldBible.RoleOf(id);
                var n = new NpcSeed
                {
                    NpcId = id, Role = role, DisplayName = names[ni++],
                    Personality = rng.Pick(Personalities), SpeechStyle = rng.Pick(SpeechStyles),
                    Mood = rng.Pick(new[] { "CALM", "NERVOUS", "SUSPICIOUS", "FRIENDLY", "CALM" }),
                    InitialLocation = WorldBible.RoleWorkplace[role],
                    InitialPlayerRelationship = rng.Next(-10, 11)
                };
                n.PublicGoal = DefaultGoal(role, ctx);
                ctx.Npc[id] = n;
                seed.NpcStates.Add(n);
            }

            // 2. Archetype and cast.
            ctx.Archetype = rng.Pick(new[] { "VANISHED_LEADER", "SMUGGLING_RING", "THE_COUP", "POISONED_QUARTER", "PROTECTOR_IN_SHADOW", "FALSE_PROPHET" });
            CastRoles(ctx);
            SetupFactions(ctx);
            BuildStory(ctx);
            AddSideSecrets(ctx);
            AddRelationships(ctx);
            BuildOpeningOpportunity(ctx);
            seed.IntroText = BuildIntro(ctx);
            if (string.IsNullOrEmpty(seed.Title)) seed.Title = "Echobound";
            return seed;
        }

        // ------------------------------------------------------------------ cast
        private static void CastRoles(Ctx c)
        {
            var r = c.Rng;
            string[] villainPool, victimPool;
            switch (c.Archetype)
            {
                case "VANISHED_LEADER": villainPool = new[] { "GUARD_COMMANDER", "DOCTOR", "FACTION_LEADER", "MERCHANT", "CRIMINAL" }; victimPool = new[] { "MAYOR", "MAYOR", "PRIEST" }; break;
                case "SMUGGLING_RING": villainPool = new[] { "SMUGGLER", "MERCHANT", "TAVERN_OWNER", "MAYOR" }; victimPool = new[] { "PRIEST", "DOCTOR", "STRANGER" }; break;
                case "THE_COUP": villainPool = new[] { "GUARD_COMMANDER", "FACTION_LEADER" }; victimPool = new[] { "MERCHANT", "PRIEST", "TAVERN_OWNER" }; break;
                case "POISONED_QUARTER": villainPool = new[] { "DOCTOR", "MERCHANT" }; victimPool = new[] { "PRIEST", "TAVERN_OWNER", "STRANGER" }; break;
                case "PROTECTOR_IN_SHADOW": villainPool = new[] { "MAYOR", "MAYOR", "GUARD_COMMANDER" }; victimPool = new[] { "SMUGGLER", "STRANGER", "CRIMINAL" }; break;
                default: villainPool = new[] { "PRIEST" }; victimPool = new[] { "MERCHANT", "TAVERN_OWNER", "STRANGER" }; break;
            }
            c.Villain = c.ById(r.Pick(villainPool));
            c.Victim = c.ById(r.PickExcept(victimPool, WorldBible.RoleOf(c.Villain)));
            var others = WorldBible.NpcIds.Where(id => id != c.Villain && id != c.Victim).ToList();
            c.Accomplice = r.Pick(others);
            others.Remove(c.Accomplice);
            c.Ally = r.Pick(others);
            others.Remove(c.Ally);
            c.Contact = others.Contains(c.ById("TAVERN_OWNER")) && r.Chance(0.6) ? c.ById("TAVERN_OWNER") : r.Pick(others);
            others.Remove(c.Contact);
            c.Witness = r.Pick(others);
            c.VictimDead = r.Chance(0.5);
            c.VictimLocation = r.Pick(new[] { "UNDERGROUND", "RUINS", "FOREST" });
            c.EvidenceLocation = r.Pick(new[] { "WAREHOUSE", "UNDERGROUND", "RUINS", "FACTION_BASE", "GUARD_STATION", "MARKET" });
            if (c.EvidenceLocation == c.VictimLocation) c.EvidenceLocation = "WAREHOUSE";
            c.Seed.VillainNpcId = c.Villain;
            c.Seed.VictimNpcId = c.Victim;
        }

        private static string DefaultGoal(string role, Ctx c)
        {
            switch (role)
            {
                case "MAYOR": return "Keep the town calm and keep my office";
                case "GUARD_COMMANDER": return "Keep order, whatever it costs";
                case "TAVERN_OWNER": return "Keep the tavern full and out of trouble";
                case "MERCHANT": return "Protect my trade routes and my margins";
                case "DOCTOR": return "Keep the residential quarter healthy";
                case "CRIMINAL": return "Make money and stay out of the guard station";
                case "SMUGGLER": return "Move goods through the forest without being seen";
                case "PRIEST": return "Guide the town through the coming hard times";
                case "STRANGER": return "Find out what happened here and leave";
                default: return "Increase the Iron Hand's grip on the town";
            }
        }

        // ------------------------------------------------------------------ factions
        private static void SetupFactions(Ctx c)
        {
            var r = c.Rng;
            var guard = new FactionSeed { Id = "TOWN_GUARD", Stance = "LAWFUL", Goal = "Hold the walls and the law", PublicFace = "Protectors of the town" };
            bool ironProtects = c.Archetype == "PROTECTOR_IN_SHADOW" || r.Chance(0.25);
            var iron = new FactionSeed
            {
                Id = "IRON_HAND", Stance = ironProtects ? "NEUTRAL" : "CRIMINAL",
                Goal = ironProtects ? "Quietly keep the town alive while the officials fail it" : "Control every deal that passes through the walls",
                PublicFace = "A gang of enforcers and smugglers"
            };
            var merchants = new FactionSeed { Id = "MERCHANT_CIRCLE", Stance = "NEUTRAL", Goal = "Keep trade flowing and taxes low", PublicFace = "The guild of shopkeepers and traders" };

            int pattern = r.Next(0, 4);
            switch (pattern)
            {
                case 0: Ally(guard, merchants); Hostile(guard, iron); Hostile(merchants, iron); break;
                case 1: Ally(merchants, iron); Hostile(guard, iron); break;
                case 2: Hostile(guard, iron); Hostile(iron, merchants); Hostile(guard, merchants); break;
                default: Ally(guard, iron); Hostile(merchants, iron); break; // a quiet arrangement between law and crime
            }
            guard.InitialPlayerReputation = r.Next(-5, 6);
            iron.InitialPlayerReputation = r.Next(-15, 1);
            merchants.InitialPlayerReputation = r.Next(-5, 11);
            c.Seed.Factions.AddRange(new[] { guard, iron, merchants });

            // Membership.
            c.Npc[c.ById("GUARD_COMMANDER")].Faction = "TOWN_GUARD";
            c.Npc[c.ById("FACTION_LEADER")].Faction = "IRON_HAND";
            c.Npc[c.ById("CRIMINAL")].Faction = "IRON_HAND";
            c.Npc[c.ById("SMUGGLER")].Faction = r.Chance(0.6) ? "IRON_HAND" : "MERCHANT_CIRCLE";
            c.Npc[c.ById("MERCHANT")].Faction = "MERCHANT_CIRCLE";
            c.Npc[c.ById("TAVERN_OWNER")].Faction = r.Chance(0.5) ? "MERCHANT_CIRCLE" : "NONE";
            c.Npc[c.ById("DOCTOR")].Faction = r.Chance(0.3) ? "MERCHANT_CIRCLE" : "NONE";
            c.Npc[c.ById("MAYOR")].Faction = r.Chance(0.4) ? "TOWN_GUARD" : "NONE";
            c.Npc[c.ById("PRIEST")].Faction = "NONE";
            c.Npc[c.ById("STRANGER")].Faction = r.Chance(0.3) ? "IRON_HAND" : "NONE";
            foreach (var f in c.Seed.Factions) c.Seed.PlayerReputation[f.Id] = f.InitialPlayerReputation;
        }

        private static void Ally(FactionSeed a, FactionSeed b) { a.AlliedWith.Add(b.Id); b.AlliedWith.Add(a.Id); }
        private static void Hostile(FactionSeed a, FactionSeed b) { a.HostileTo.Add(b.Id); b.HostileTo.Add(a.Id); }

        // ------------------------------------------------------------------ story
        private static void BuildStory(Ctx c)
        {
            var s = c.Seed;
            var r = c.Rng;
            string villain = c.N(c.Villain), victim = c.N(c.Victim), accomplice = c.N(c.Accomplice), ally = c.N(c.Ally), witness = c.N(c.Witness);
            string vRole = RoleWord(c.Villain), vicRole = RoleWord(c.Victim);
            string evLoc = WorldBible.LocationName(c.EvidenceLocation);
            string vicLoc = WorldBible.LocationName(c.VictimLocation);
            var vNpc = c.Npc[c.Villain];
            var aNpc = c.Npc[c.Accomplice];
            var victimNpc = c.Npc[c.Victim];

            string motive;
            switch (c.Archetype)
            {
                case "VANISHED_LEADER":
                    s.Title = "The Empty Chair";
                    s.MainConflict = $"{victim}, the {vicRole}, vanished three nights ago. The guards say {victim} fled the town. Nobody believes them.";
                    s.CentralMystery = $"What happened to {victim}, and who in {c.Town} needed it to happen?";
                    motive = MotiveFor(c, vRole);
                    vNpc.Secret = $"I made {victim} disappear. {motive}";
                    break;
                case "SMUGGLING_RING":
                    s.Title = "Crates at Midnight";
                    s.MainConflict = $"Crates move through {c.Town} after dark and people who ask about them stop asking. {victim}, the {vicRole}, asked too much.";
                    s.CentralMystery = $"Who runs the ring, and who among the town's officials lets it pass the walls?";
                    vNpc.Secret = $"I run the smuggling ring through the {evLoc}. {victim} found my ledger, so {victim} had to go.";
                    break;
                case "THE_COUP":
                    s.Title = "Before the Bell";
                    s.MainConflict = $"The {vRole} is quietly gathering loyal people. {victim}, the {vicRole}, refused to join and has not been seen since.";
                    s.CentralMystery = $"Is a takeover of {c.Town} coming, who is behind it, and how much time is left?";
                    vNpc.Secret = $"I am going to take {c.Town} by force within two days. {victim} refused to stand with me and knew too much.";
                    break;
                case "POISONED_QUARTER":
                    s.Title = "The Quiet Sickness";
                    s.MainConflict = $"People in the residential quarter are falling sick. The priest blames outsiders. {victim}, the {vicRole}, started asking where the medicine comes from.";
                    s.CentralMystery = $"What is really making the quarter sick, and who profits from it?";
                    vNpc.Secret = $"The 'medicine' I sell is cut with cheap poison from the forest. {victim} was about to prove it.";
                    break;
                case "PROTECTOR_IN_SHADOW":
                    s.Title = "The Price of the Walls";
                    s.MainConflict = $"Outsiders have been seen near the ruins. The Iron Hand, of all people, has been quietly keeping them out. {victim}, the {vicRole}, carried proof of a deal to sell the town.";
                    s.CentralMystery = $"Who inside {c.Town} is selling it to the outsiders, and why is a gang the only thing standing in the way?";
                    vNpc.Secret = $"I have promised {c.Town} to the outsiders in exchange for my own safety. {victim} carried the proof and had to be silenced.";
                    break;
                default:
                    s.Title = "Voices Under the Ruins";
                    s.MainConflict = $"People are disappearing, and the priest says they have 'gone to be saved'. {victim}, the {vicRole}, was the last to vanish.";
                    s.CentralMystery = $"Where are the missing going, and what is under the ruins?";
                    vNpc.Secret = $"I keep the 'saved' locked beneath the ruins. They fund my work and their fear keeps the town obedient. {victim} tried to leave.";
                    break;
            }
            vNpc.SecretIsCrime = true;
            vNpc.Mood = r.Pick(new[] { "CALM", "SUSPICIOUS", "FRIENDLY" });
            vNpc.PublicGoal = r.Chance(0.5) ? $"Find out what the newcomer wants in {c.Town}" : vNpc.PublicGoal;
            aNpc.Secret = $"I helped {villain} with what happened to {victim}. I was paid, and I am afraid.";
            aNpc.SecretIsCrime = true;
            aNpc.Mood = "NERVOUS";

            // Victim status.
            victimNpc.Alive = !c.VictimDead;
            victimNpc.InitialLocation = c.VictimLocation;
            victimNpc.Mood = "AFRAID";
            victimNpc.Secret = c.VictimDead ? "" : $"I know exactly what {villain} did. I am hiding at the {vicLoc}.";

            // Hidden truths (facts). Ids are stable so quests can reference them.
            s.HiddenTruths.Add(new HiddenTruth
            {
                Id = "TRUTH_CORE", IsCore = true,
                Summary = $"{villain} is behind what happened to {victim}. " + vNpc.Secret,
                KnownBy = c.VictimDead ? new List<string> { c.Villain, c.Accomplice } : new List<string> { c.Villain, c.Accomplice, c.Victim },
                EvidenceLocation = c.EvidenceLocation, EvidenceItem = c.Archetype == "SMUGGLING_RING" ? "LEDGER" : "LETTER",
                EvidenceLabel = c.Archetype == "SMUGGLING_RING" ? $"{villain}'s ledger" : $"A letter in {villain}'s hand"
            });
            s.HiddenTruths.Add(new HiddenTruth
            {
                Id = "TRUTH_VICTIM",
                Summary = c.VictimDead ? $"{victim}'s body is hidden at the {vicLoc}." : $"{victim} is alive, hiding at the {vicLoc}.",
                KnownBy = c.VictimDead ? new List<string> { c.Villain, c.Accomplice } : new List<string> { c.Victim, c.Ally },
                EvidenceLocation = c.VictimLocation, EvidenceItem = "EVIDENCE",
                EvidenceLabel = c.VictimDead ? $"{victim}'s belongings" : $"A recent campfire and {victim}'s coat"
            });
            s.HiddenTruths.Add(new HiddenTruth
            {
                Id = "TRUTH_LEAD_WITNESS",
                Summary = $"{witness} saw {villain} heading toward the {evLoc} the night {victim} vanished, carrying something heavy.",
                KnownBy = new List<string> { c.Witness },
                EvidenceLocation = c.EvidenceLocation, EvidenceItem = "EVIDENCE", EvidenceLabel = "Drag marks and a torn sleeve"
            });
            s.HiddenTruths.Add(new HiddenTruth
            {
                Id = "TRUTH_LEAD_CONTACT",
                Summary = $"{witness} was at the tavern the night {victim} vanished and left early, looking shaken. {witness} knows something.",
                KnownBy = new List<string> { c.Contact, c.ById("TAVERN_OWNER") },
                EvidenceLocation = "TAVERN", EvidenceItem = "EVIDENCE", EvidenceLabel = "A tab in the tavern book, unpaid, dated that night"
            });
            s.HiddenTruths.Add(new HiddenTruth
            {
                Id = "TRUTH_ACCOMPLICE",
                Summary = $"{accomplice} was paid by {villain} and helped. {accomplice} is terrified it will come out.",
                KnownBy = new List<string> { c.Villain, c.Accomplice, c.Ally },
                EvidenceLocation = WorldBible.RoleWorkplace[c.Npc[c.Accomplice].Role], EvidenceItem = "COIN_PURSE", EvidenceLabel = $"A purse of coins marked with {villain}'s seal"
            });
            // The ally wants the truth out and knows the victim's fate half-way.
            c.Npc[c.Ally].PublicGoal = $"Find out what really happened to {victim}";
            c.Npc[c.Ally].Mood = "GRIEVING";

            BuildOpeningQuest(c);
            BuildEndings(c);
        }

        private static string MotiveFor(Ctx c, string vRole)
        {
            string victim = c.N(c.Victim);
            switch (c.Npc[c.Villain].Role)
            {
                case "GUARD_COMMANDER": return $"{victim} found out I plan to take the town for myself.";
                case "DOCTOR": return $"{victim} discovered I have sold fake medicine for years.";
                case "FACTION_LEADER": return $"{victim} refused to pay the Iron Hand and threatened to bring in outside soldiers.";
                case "MERCHANT": return $"{victim} discovered my debts to outsiders and how I meant to pay them.";
                case "CRIMINAL": return "I was paid by someone in the guard station to do it. I do not know who gave the order.";
                default: return $"{victim} was about to expose me.";
            }
        }

        private static void BuildOpeningQuest(Ctx c)
        {
            var s = c.Seed;
            string villain = c.N(c.Villain), victim = c.N(c.Victim), contact = c.N(c.Contact), witness = c.N(c.Witness);
            string evLoc = WorldBible.LocationName(c.EvidenceLocation);
            string evidenceItem = s.HiddenTruths[0].EvidenceItem;
            bool villainIsGuard = c.Npc[c.Villain].Faction == "TOWN_GUARD";
            bool villainIsIron = c.Npc[c.Villain].Faction == "IRON_HAND";

            var q = new Quest
            {
                QuestId = "QUEST_OPENING", IsMain = true, Sequential = true,
                Title = "A Letter for a Dead Man",
                NarrativeReason = $"You carry a letter addressed to {victim}. Everyone says {victim} is gone. Someone paid you to deliver it anyway.",
                TriggerEvent = "GAME_START",
                RelevantNpcs = new List<string> { c.Contact, c.Witness, c.Villain, c.Victim, c.Ally }
            };
            q.Objectives.Add(new QuestObjective { Id = "OBJ_CONTACT", Action = "TALK", TargetNpc = c.Contact, Description = $"Ask {contact} at the {WorldBible.LocationName(WorldBible.RoleWorkplace[c.Npc[c.Contact].Role])} about {victim}." });
            q.Objectives.Add(new QuestObjective { Id = "OBJ_WITNESS", Action = "TALK", TargetNpc = c.Witness, Description = $"Find {witness} and learn what they saw that night." });
            q.Objectives.Add(new QuestObjective { Id = "OBJ_SEARCH", Action = "INVESTIGATE", Location = c.EvidenceLocation, TargetItem = evidenceItem, Description = $"Search the {evLoc} for proof of what happened to {victim}." });
            q.Objectives.Add(new QuestObjective { Id = "OBJ_VICTIM", Action = "SEARCH", Location = c.VictimLocation, Optional = true, Description = $"Optional: find where {victim} ended up." });

            q.FailureConditions.Add(new FailureCondition { Type = "NPC_DEAD", NpcId = c.Contact, MutationHint = "" });
            q.FailureConditions.Add(new FailureCondition { Type = "NPC_DEAD", NpcId = c.Witness, MutationHint = "" });
            if (c.Archetype == "THE_COUP")
                q.FailureConditions.Add(new FailureCondition { Type = "DEADLINE", DeadlineMinute = 36 * 60, MutationHint = $"The bell rang. {villain} has seized the guard station. Find out who still resists." });

            // Resolutions: same evidence, very different outcomes depending on who the villain is.
            q.Resolutions.Add(new QuestResolution
            {
                Id = "RES_GUARD", Summary = "Hand the proof to the Town Guard", RequiresItem = evidenceItem,
                Consequences = villainIsGuard
                    ? new List<WorldChange> { WorldChange.Reputation("TOWN_GUARD", -40), WorldChange.Tension(25), WorldChange.Hostile(c.Villain), WorldChange.SpawnEnemies("GUARD_STATION", 2, "TOWN_GUARD"), WorldChange.Rumor($"The newcomer accused {villain} in front of the guards and walked out alive. For now.") }
                    : new List<WorldChange> { WorldChange.Reputation("TOWN_GUARD", 30), WorldChange.Tension(-10), WorldChange.Hostile(c.Villain), WorldChange.MoveNpc(c.Villain, "GUARD_STATION"), WorldChange.Rumor($"{villain} was dragged to the guard station by the commander's people.") },
                OutcomeText = villainIsGuard ? $"You handed the proof to the very people {villain} commands. It vanished, and so did your welcome." : $"{villain} was arrested. The town breathed out.",
                EndingId = villainIsGuard ? "ENDING_BURIED" : "ENDING_JUSTICE"
            });
            q.Resolutions.Add(new QuestResolution
            {
                Id = "RES_IRON", Summary = "Sell the proof to the Iron Hand", RequiresItem = evidenceItem,
                Consequences = villainIsIron
                    ? new List<WorldChange> { WorldChange.Reputation("IRON_HAND", -30), WorldChange.Tension(20), WorldChange.Hostile(c.Villain), WorldChange.SpawnEnemies("FACTION_BASE", 2, "IRON_HAND") }
                    : new List<WorldChange> { WorldChange.Reputation("IRON_HAND", 35), WorldChange.Reputation("TOWN_GUARD", -15), WorldChange.Tension(10), WorldChange.Rumor($"The Iron Hand now owns {villain}. Nobody says it aloud.") },
                OutcomeText = villainIsIron ? "You tried to sell the Iron Hand its own secret. They did not take it well." : $"The Iron Hand paid well. {villain} now answers to them.",
                EndingId = villainIsIron ? "ENDING_BURIED" : "ENDING_IRON_RULE"
            });
            q.Resolutions.Add(new QuestResolution
            {
                Id = "RES_CONFRONT", Summary = $"Confront {villain} yourself", RequiresItem = evidenceItem, RequiresNpcAlive = c.Villain,
                Consequences = new List<WorldChange> { WorldChange.Hostile(c.Villain), WorldChange.Tension(15), WorldChange.SpawnEnemies(WorldBible.RoleWorkplace[c.Npc[c.Villain].Role], 2, c.Npc[c.Villain].Faction), WorldChange.SetMood(c.Villain, "ANGRY") },
                OutcomeText = $"You showed {villain} what you found. {villain} did not deny it. Then the knives came out.",
                EndingId = "ENDING_BLOOD"
            });
            q.Resolutions.Add(new QuestResolution
            {
                Id = "RES_BURN", Summary = "Burn the proof and walk away",
                Consequences = new List<WorldChange> { WorldChange.Tension(-15), WorldChange.Relationship(c.Ally, -40), WorldChange.Rumor("The newcomer knows something and is keeping it. People notice.") },
                OutcomeText = $"You burned it. {c.N(c.Ally)} will never forgive you. The town stays quiet, the way {villain} likes it.",
                EndingId = "ENDING_WALK_AWAY"
            });
            if (!c.VictimDead)
            {
                q.Resolutions.Add(new QuestResolution
                {
                    Id = "RES_RESCUE", Summary = $"Bring {victim} back into town openly", RequiresNpcAlive = c.Victim,
                    Consequences = new List<WorldChange> { WorldChange.MoveNpc(c.Victim, "TOWN_SQUARE"), WorldChange.Tension(20), WorldChange.Hostile(c.Villain), WorldChange.Reputation("TOWN_GUARD", 15), WorldChange.Reputation("MERCHANT_CIRCLE", 15), WorldChange.Rumor($"{victim} walked back through the gate with the newcomer. {villain} has gone very quiet.") },
                    OutcomeText = $"{victim} walked into the square alive. The lie collapsed in daylight.",
                    EndingId = "ENDING_RETURN"
                });
            }
            s.OpeningQuest = q;
        }

        private static void BuildEndings(Ctx c)
        {
            var s = c.Seed;
            string villain = c.N(c.Villain), victim = c.N(c.Victim);
            s.PossibleEndings.Add(new EndingSeed { Id = "ENDING_JUSTICE", Summary = $"{villain} answers for {victim} before the whole town.", ConditionHint = "Proof delivered to honest authorities." });
            s.PossibleEndings.Add(new EndingSeed { Id = "ENDING_BURIED", Summary = "The truth is buried by the people who should have acted on it.", ConditionHint = "Proof delivered to the wrong hands." });
            s.PossibleEndings.Add(new EndingSeed { Id = "ENDING_IRON_RULE", Summary = "The Iron Hand owns the town's secret and therefore the town.", ConditionHint = "Proof sold to the Iron Hand." });
            s.PossibleEndings.Add(new EndingSeed { Id = "ENDING_BLOOD", Summary = $"You settled it with {villain} personally.", ConditionHint = "Confront the villain with proof." });
            s.PossibleEndings.Add(new EndingSeed { Id = "ENDING_WALK_AWAY", Summary = "You knew, and you chose the quiet.", ConditionHint = "Destroy the proof." });
            if (!c.VictimDead) s.PossibleEndings.Add(new EndingSeed { Id = "ENDING_RETURN", Summary = $"{victim} returns and the lie collapses.", ConditionHint = "Find the victim alive and bring them back." });
        }

        // ------------------------------------------------------------------ texture
        private static void AddSideSecrets(Ctx c)
        {
            var r = c.Rng;
            var iron = c.N(c.ById("FACTION_LEADER"));
            var templates = new List<Func<NpcSeed, string>>
            {
                n => $"I owe {iron} more money than I will ever have.",
                n => "I report the tavern's gossip to the guard commander for coin.",
                n => "I deserted from a militia years ago under another name.",
                n => $"I am in love with {c.N(r.PickExcept(WorldBible.NpcIds.ToList(), n.NpcId))} and it is not returned.",
                n => "I water down everything I sell and the whole quarter suspects it.",
                n => "I know where the old vault under the ruins opens, and I have never told anyone.",
                n => "I have been hiding a sick relative in the residential quarter to avoid the doctor's questions.",
                n => "I forged the last three letters that went out under the mayor's seal.",
                n => "I sold the guard station's spare keys to the Iron Hand.",
                n => "I saw the outsiders at the ruins and said nothing because they paid me."
            };
            foreach (var n in c.Seed.NpcStates)
            {
                if (!string.IsNullOrEmpty(n.Secret)) continue;
                if (r.Chance(0.75))
                {
                    n.Secret = r.Pick(templates)(n);
                    n.SecretIsCrime = n.Secret.Contains("sold") || n.Secret.Contains("forged") || n.Secret.Contains("deserted");
                    c.Seed.HiddenTruths.Add(new HiddenTruth
                    {
                        Id = "TRUTH_" + n.NpcId, Summary = $"{n.DisplayName}: {n.Secret}", KnownBy = new List<string> { n.NpcId },
                        EvidenceLocation = WorldBible.RoleHome[n.Role], EvidenceItem = "EVIDENCE", EvidenceLabel = $"Something {n.DisplayName} would rather you had not found"
                    });
                }
            }
        }

        private static void AddRelationships(Ctx c)
        {
            var r = c.Rng;
            var s = c.Seed;
            s.Relationships.Add(new RelationshipSeed { A = c.Villain, B = c.Accomplice, Type = "BLACKMAIL", IsSecret = true, Summary = $"{c.N(c.Villain)} holds something over {c.N(c.Accomplice)}" });
            s.Relationships.Add(new RelationshipSeed { A = c.Victim, B = c.Ally, Type = r.Pick(new[] { "FAMILY", "LOVER", "ALLY" }), IsSecret = false, Summary = $"{c.N(c.Ally)} was closest to {c.N(c.Victim)} and wants the truth" });
            var used = new HashSet<string> { c.Villain + c.Accomplice, c.Victim + c.Ally };
            var ids = WorldBible.NpcIds.ToList();
            int extra = r.Next(3, 6);
            var typesPool = new[] { "RIVAL", "DEBTOR", "FAMILY", "EMPLOYER", "ALLY", "ENEMY", "LOVER" };
            for (int i = 0; i < extra; i++)
            {
                string a = r.Pick(ids), b = r.PickExcept(ids, a);
                if (used.Contains(a + b) || used.Contains(b + a)) continue;
                used.Add(a + b);
                string type = r.Pick(typesPool);
                string summary;
                switch (type)
                {
                    case "RIVAL": summary = $"{c.N(a)} and {c.N(b)} have hated each other since a deal went wrong"; break;
                    case "DEBTOR": summary = $"{c.N(a)} owes {c.N(b)} a debt that is overdue"; break;
                    case "FAMILY": summary = $"{c.N(a)} and {c.N(b)} are cousins who rarely admit it"; break;
                    case "EMPLOYER": summary = $"{c.N(a)} pays {c.N(b)} for small favours"; break;
                    case "ENEMY": summary = $"{c.N(a)} blames {c.N(b)} for a death in the family"; break;
                    case "LOVER": summary = $"{c.N(a)} and {c.N(b)} meet after dark and think nobody knows"; break;
                    default: summary = $"{c.N(a)} and {c.N(b)} trust each other more than they trust the town"; break;
                }
                s.Relationships.Add(new RelationshipSeed { A = a, B = b, Type = type, Summary = summary, IsSecret = type == "LOVER" || r.Chance(0.3) });
            }
        }

        private static void BuildOpeningOpportunity(Ctx c)
        {
            var r = c.Rng;
            string merchant = c.ById("MERCHANT"), leader = c.ById("FACTION_LEADER"), commander = c.ById("GUARD_COMMANDER"), smuggler = c.ById("SMUGGLER");
            int pick = r.Next(0, 3);
            Quest q;
            if (pick == 0)
            {
                q = new Quest
                {
                    QuestId = "QUEST_OPP_STEAL", Title = "A Ledger, Tonight", GiverNpc = leader, TriggerEvent = "OPENING_OPPORTUNITY",
                    NarrativeReason = $"{c.N(leader)} of the Iron Hand wants {c.N(merchant)}'s ledger, and wants it before the market opens.",
                    RelevantNpcs = new List<string> { leader, merchant }
                };
                q.Objectives.Add(new QuestObjective { Id = "OPP_STEAL", Action = "STEAL", Location = "MARKET", TargetItem = "LEDGER", AvailableFromHour = 21, AvailableUntilHour = 5, Description = $"Take {c.N(merchant)}'s ledger from the market between 21:00 and 05:00." });
                q.Objectives.Add(new QuestObjective { Id = "OPP_DELIVER", Action = "DELIVER", TargetNpc = leader, TargetItem = "LEDGER", Description = $"Bring the ledger to {c.N(leader)}." });
                q.FailureConditions.Add(new FailureCondition { Type = "DEADLINE", DeadlineMinute = 14 * 60, MutationHint = $"The market opened and the ledger is gone from its place. {c.N(leader)} will want to know why you were slow." });
                q.Resolutions.Add(new QuestResolution { Id = "OPP_RES_GIVE", Summary = "Hand it over and take the coin", Consequences = new List<WorldChange> { WorldChange.Reputation("IRON_HAND", 25), WorldChange.Reputation("MERCHANT_CIRCLE", -20) }, OutcomeText = "The Iron Hand pays its debts. So, eventually, will the merchants." });
                q.Resolutions.Add(new QuestResolution { Id = "OPP_RES_KEEP", Summary = "Keep the ledger for yourself", Consequences = new List<WorldChange> { WorldChange.Reputation("IRON_HAND", -30), WorldChange.Relationship(leader, -40), WorldChange.Tension(10) }, OutcomeText = "You kept it. The Iron Hand keeps score." });
                q.WorldConsequences.Add(WorldChange.Rumor("Someone was in the market after dark.", "MARKET"));
            }
            else if (pick == 1)
            {
                q = new Quest
                {
                    QuestId = "QUEST_OPP_FOLLOW", Title = "Eyes in the Forest", GiverNpc = commander, TriggerEvent = "OPENING_OPPORTUNITY",
                    NarrativeReason = $"{c.N(commander)} will pay a stranger to follow {c.N(smuggler)} into the forest tonight, since a guard would be noticed.",
                    RelevantNpcs = new List<string> { commander, smuggler }
                };
                q.Objectives.Add(new QuestObjective { Id = "OPP_FOLLOW", Action = "FOLLOW", Location = "FOREST", AvailableFromHour = 17, AvailableUntilHour = 23, Description = $"Follow {c.N(smuggler)} to the forest between 17:00 and 23:00." });
                q.Objectives.Add(new QuestObjective { Id = "OPP_REPORT", Action = "TALK", TargetNpc = commander, Description = $"Report to {c.N(commander)}." });
                q.FailureConditions.Add(new FailureCondition { Type = "DEADLINE", DeadlineMinute = 12 * 60, MutationHint = $"You missed the night run. {c.N(commander)} sent a guard instead, and the guard did not come back." });
                q.Resolutions.Add(new QuestResolution { Id = "OPP_RES_TRUE", Summary = "Tell the commander everything", Consequences = new List<WorldChange> { WorldChange.Reputation("TOWN_GUARD", 25), WorldChange.Reputation("IRON_HAND", -20), WorldChange.Hostile(smuggler) }, OutcomeText = "The guard moved on the forest path. The smuggler knows who talked." });
                q.Resolutions.Add(new QuestResolution { Id = "OPP_RES_LIE", Summary = "Lie: you saw nothing", Consequences = new List<WorldChange> { WorldChange.Reputation("TOWN_GUARD", -10), WorldChange.Reputation("IRON_HAND", 15), WorldChange.Rumor("The newcomer covered for the smugglers.") }, OutcomeText = "You lied to the guard. The forest remembers who kept its secret." });
            }
            else
            {
                q = new Quest
                {
                    QuestId = "QUEST_OPP_PROTECT", Title = "Trouble at the Stalls", GiverNpc = merchant, TriggerEvent = "OPENING_OPPORTUNITY",
                    NarrativeReason = $"{c.N(merchant)} expects thugs at the market tonight and the guard will not come. A stranger with a blade is worth paying.",
                    RelevantNpcs = new List<string> { merchant }
                };
                q.Objectives.Add(new QuestObjective { Id = "OPP_PROTECT", Action = "PROTECT", Location = "MARKET", TargetNpc = merchant, AvailableFromHour = 22, AvailableUntilHour = 3, Description = "Be at the market between 22:00 and 03:00 and stop the thugs." });
                q.Objectives.Add(new QuestObjective { Id = "OPP_COLLECT", Action = "TALK", TargetNpc = merchant, Description = $"Collect your pay from {c.N(merchant)}." });
                q.FailureConditions.Add(new FailureCondition { Type = "DEADLINE", DeadlineMinute = 10 * 60, MutationHint = "The stalls were wrecked in the night. The merchant wants to know who sent the thugs." });
                q.Resolutions.Add(new QuestResolution { Id = "OPP_RES_PAY", Summary = "Take the pay", Consequences = new List<WorldChange> { WorldChange.Reputation("MERCHANT_CIRCLE", 25), WorldChange.Reputation("IRON_HAND", -15) }, OutcomeText = "The merchants remember who stood with them." });
                q.Resolutions.Add(new QuestResolution { Id = "OPP_RES_FAVOR", Summary = "Refuse the coin, ask for a favour instead", Consequences = new List<WorldChange> { WorldChange.Reputation("MERCHANT_CIRCLE", 40), WorldChange.Relationship(merchant, 30) }, OutcomeText = "A favour owed by the Merchant Circle is worth more than coin." });
            }
            c.Seed.OpeningOpportunity = q;
        }

        private static string BuildIntro(Ctx c)
        {
            string victim = c.N(c.Victim);
            string hook;
            switch (c.Archetype)
            {
                case "VANISHED_LEADER": hook = $"Three nights ago, {victim} the {RoleWord(c.Victim)} disappeared.\nThe guards claim {victim} fled.\nNobody believes them."; break;
                case "SMUGGLING_RING": hook = $"Crates move through the streets after dark.\n{victim} asked where they came from.\n{victim} has not been seen since."; break;
                case "THE_COUP": hook = $"Soldiers are drilling at odd hours.\n{victim} refused to join something.\nNow {victim} is gone, and the bell has not rung in days."; break;
                case "POISONED_QUARTER": hook = $"The residential quarter is sick.\nThe priest blames outsiders.\n{victim} blamed the medicine, and then fell silent."; break;
                case "PROTECTOR_IN_SHADOW": hook = $"Strangers have been seen at the ruins.\nThe only people keeping them out are the ones the town calls criminals.\n{victim} carried proof of why, and vanished."; break;
                default: hook = $"People are disappearing.\nThe priest says they have been saved.\n{victim} was the last to be saved."; break;
            }
            return $"YEAR 2142\nFor twenty years the town of {c.Town} has survived behind its walls.\n{hook}\nYou arrive in town carrying a letter addressed to a dead man.\nBEGIN.";
        }

        private static string RoleWord(string npcId) => WorldBible.RoleOf(npcId).Replace('_', ' ').ToLowerInvariant();
    }
}
