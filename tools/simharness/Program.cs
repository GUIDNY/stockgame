using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Echobound.AI;
using Echobound.AI.Providers;
using Echobound.AI.Schemas;
using Echobound.Core;
using Echobound.Dialogue;
using Echobound.Inventory;
using Echobound.Quests;
using Echobound.SaveSystem;
using Echobound.World;

namespace Echobound.SimHarness
{
    /// <summary>
    /// Headless smoke test of the narrative simulation. Exercises the same code paths the Unity build uses:
    /// world generation -> validation -> world state -> quests -> dialogue -> mutation -> consequences -> save/load.
    /// </summary>
    public static class Program
    {
        private static int _failures;

        public static async Task<int> Main(string[] args)
        {
            GameLog.InfoSink = s => { };
            GameLog.WarnSink = s => Console.WriteLine("  warn: " + s);
            GameLog.ErrorSink = s => { Console.WriteLine("  ERROR: " + s); _failures++; };

            int[] seeds = args.Length > 0 ? args.Select(int.Parse).ToArray() : new[] { 1, 2, 3, 42, 1337 };
            Console.WriteLine("=== ECHOBOUND simulation harness ===");
            foreach (var seed in seeds) await RunWorld(seed);
            Console.WriteLine(_failures == 0 ? "ALL CHECKS PASSED" : $"{_failures} FAILURE(S)");
            return _failures == 0 ? 0 : 1;
        }

        private static void Check(bool condition, string what)
        {
            if (!condition) { _failures++; Console.WriteLine("  FAIL: " + what); }
        }

        private static async Task RunWorld(int seedNumber)
        {
            Console.WriteLine($"\n--- seed {seedNumber} ---");
            GameEvents.ClearAll();
            var config = new AIConfig { Provider = "mock" };
            var ai = new AIRequestManager(config, new MockAIProvider(seedNumber) { SimulatedLatencyMs = 0 });

            // 1. World generation through the full request path (mock provider -> JSON -> validator).
            var gen = await AIWorldDirector.GenerateWorldAsync(ai, seedNumber);
            Check(gen.Value != null, "world generated");
            Check(gen.FromAI && !gen.FromFallback, "world came through the validated AI path, not the fallback");
            var seed = gen.Value;
            Console.WriteLine($"  {seed.Title}: {seed.MainConflict}");
            Console.WriteLine($"  villain={seed.VillainNpcId} ({seed.GetNpc(seed.VillainNpcId)?.DisplayName}) victim={seed.VictimNpcId} ({seed.GetNpc(seed.VictimNpcId)?.DisplayName}, alive={seed.GetNpc(seed.VictimNpcId)?.Alive})");
            Check(seed.NpcStates.Count == 10, "10 npcs");
            Check(seed.OpeningQuest != null && seed.OpeningQuest.Objectives.Count >= 3, "opening quest");
            Check(seed.OpeningQuest.Resolutions.Count >= 3, "3+ resolutions");
            Check(seed.PossibleEndings.Count >= 3, "3+ endings");
            Check(seed.HiddenTruths.Any(h => h.IsCore), "core truth");

            // Determinism: same seed => same story.
            var again = NarrativeGenerator.Generate(seedNumber);
            Check(again.MainConflict == seed.MainConflict && again.VillainNpcId == seed.VillainNpcId, "generation is deterministic");
            var other = NarrativeGenerator.Generate(seedNumber + 1);
            Check(other.MainConflict != seed.MainConflict || other.VillainNpcId != seed.VillainNpcId || other.GetNpc("NPC_01").DisplayName != seed.GetNpc("NPC_01").DisplayName, "different seed => different story");

            // 2. Build runtime systems.
            var state = WorldState.FromSeed(seed);
            var rng = new SeededRandom(seedNumber);
            var factions = new FactionSystem(state);
            var knowledge = new KnowledgeSystem(state, rng);
            var applier = new WorldChangeApplier(state, factions, knowledge);
            var inventory = new InventorySystem(state.Player);
            var quests = new QuestManager(state, applier, inventory);
            var director = new AIWorldDirector(state, ai, applier, knowledge, quests, rng);
            var dialogue = new DialogueManager(state, ai, knowledge, quests, inventory, director, rng);
            var clock = new WorldClock();
            GameEvents.TimeAdvanced += m => state.ElapsedMinutes = m;
            var notifications = new List<string>();
            GameEvents.Notification += notifications.Add;

            var villain = state.GetNpc(seed.VillainNpcId);
            var victim = state.GetNpc(seed.VictimNpcId);
            Check(villain.Knows("TRUTH_CORE"), "villain knows the core truth");
            Check(!state.GetNpc("NPC_09").Knows("TRUTH_CORE") || seed.VillainNpcId == "NPC_09" || seed.HiddenTruths[0].KnownBy.Contains("NPC_09"), "uninvolved npc does not know the core truth");
            var main = quests.MainQuest;
            Check(main != null && main.IsActive, "main quest active");
            Console.WriteLine("  first objective: " + main.CurrentObjectiveText());

            // 3. Dialogue with the first contact: TALK objective completes, NPC remembers.
            var contactId = main.Objectives[0].TargetNpc;
            var contact = state.GetNpc(contactId);
            state.Player.Location = contact.CurrentLocation;
            DialogueTurn last = null;
            dialogue.TurnReady += t => last = t;
            dialogue.Begin(contact);
            await WaitFor(() => last != null && !last.Waiting);
            Console.WriteLine($"  {contact.DisplayName}: {last.NpcLine}");
            var threaten = last.Options.First(o => o.Intent == "THREATEN");
            last = null; dialogue.Choose(threaten);
            await WaitFor(() => last != null && !last.Waiting);
            Console.WriteLine($"  [threaten] {contact.DisplayName}: {last.NpcLine}");
            Check(contact.Memory.HasMemoryOf("THREATENED"), "npc remembers being threatened");
            Check(contact.Relationship.Fear > 0 || contact.Relationship.Hostility > 0, "threat changed relationship");
            dialogue.End();
            Check(main.Objectives[0].Completed, "TALK objective completed by conversation");

            // Witness memory: someone else in the same location saw it.
            var witness = state.NpcsAt(contact.CurrentLocation).FirstOrDefault(n => n.NpcId != contactId);
            if (witness != null) Check(witness.Memory.ShortTerm.Concat(witness.Memory.LongTerm).Any(m => m.AboutPlayer), "bystander witnessed the threat");
            var farAway = state.AliveNpcs.FirstOrDefault(n => n.CurrentLocation != contact.CurrentLocation && n.Faction != contact.Faction);
            if (farAway != null) Check(!farAway.Memory.ShortTerm.Any(m => m.Event == "THREATENED"), "distant npc did not magically learn of the threat");

            // Second meeting: the opening line references the memory (the "it remembered" moment).
            last = null; dialogue.Begin(contact);
            await WaitFor(() => last != null && !last.Waiting);
            Console.WriteLine($"  [again] {contact.DisplayName}: {last.NpcLine}");
            Check(last.NpcLine.Contains("forgotten") || last.NpcLine.Contains("remember") || last.NpcLine.Contains("Please") || last.NpcLine.Contains("quick") || last.NpcLine.Contains("clear") || last.NpcLine.Contains("what do you need") || last.NpcLine.Contains("nothing to say"), "second greeting reflects the threat");
            dialogue.End();

            // 4. Time passes: schedules move, rumors spread.
            clock.Advance(60 * 6);
            Check(state.ElapsedMinutes == 360, "clock advanced state");
            Console.WriteLine($"  after 6h: rumors={state.Rumors.Count}, facts={state.Facts.Facts.Count}, tension={state.WorldTension}");

            // 5. Kill the witness NPC (second objective) -> quest must MUTATE, not fail.
            var witnessObj = main.Objectives.FirstOrDefault(o => !o.Completed && o.Action == "TALK");
            if (witnessObj != null)
            {
                var w = state.GetNpc(witnessObj.TargetNpc);
                state.Player.Location = w.CurrentLocation;
                applier.KillNpc(w, WorldBible.PlayerId);
                await Task.Delay(50);
                Check(main.State == "MUTATED", "quest mutated instead of failing (state=" + main.State + ")");
                Check(main.IsActive, "mutated quest is still playable");
                Check(main.Objectives.Any(o => !o.Completed && o.Action == "SEARCH"), "replacement SEARCH objective added");
                Console.WriteLine("  mutated -> " + main.CurrentObjectiveText());
                Check(main.History.Any(h => h.StartsWith("MUTATED")), "mutation recorded in history");
                Check(state.EventLog.Any(e => e.Text.Contains("died")), "death logged");
                await WaitFor(() => director.InFlightRequests == 0);
                Check(state.AppliedEvents.Count > 0, "director produced a consequence for the murder");
                Console.WriteLine("  consequence: " + state.AppliedEvents.Last().Description);
            }

            // 6. Play through remaining objectives with synthetic actions.
            int guard = 0;
            while (main.IsActive && !main.DecisionPending && guard++ < 10)
            {
                var o = main.CurrentObjective();
                if (o == null) break;
                var act = new PlayerAction { LocationId = o.Location, TargetNpcId = o.TargetNpc, GameMinute = state.ElapsedMinutes, Importance = 3 };
                switch (o.Action)
                {
                    case "TALK": act.Type = PlayerActionType.TALKED; break;
                    case "SEARCH": act.Type = PlayerActionType.SEARCHED; break;
                    case "INVESTIGATE":
                        act.Type = PlayerActionType.FOUND_EVIDENCE; act.Detail = o.TargetItem; act.Importance = 6;
                        inventory.Add(Item.Create(o.TargetItem, "proof", "", "TRUTH_CORE"));
                        break;
                    default: act.Type = PlayerActionType.SEARCHED; break;
                }
                state.Player.Location = o.Location ?? state.Player.Location;
                GameEvents.RaisePlayerActed(act);
            }
            Check(main.DecisionPending, "main quest reached its decision point");
            var options = quests.AvailableResolutions(main);
            Check(options.Count >= 3, "3+ resolutions available at decision time (" + options.Count + ")");
            Console.WriteLine("  decision options: " + string.Join(" | ", options.Select(r => r.Summary)));
            int repBefore = factions.GetReputation("TOWN_GUARD");
            quests.Resolve(main, options[0]);
            Check(main.State == "COMPLETED", "quest completed after resolution");
            Check(factions.GetReputation("TOWN_GUARD") != repBefore || options[0].Consequences.All(c => c.Faction != "TOWN_GUARD"), "resolution consequences applied");
            Check(!string.IsNullOrEmpty(state.EndingReached), "ending recorded: " + state.EndingReached);

            // 7. Opportunity delivery and offer acceptance.
            director.TryDeliverOpportunity(force: true);
            Check(state.PendingOffers.Count == 1 || seed.OpeningOpportunity == null || !state.GetNpc(seed.OpeningOpportunity.GiverNpc).Alive, "opportunity offered");
            Check(state.PendingOffers.All(o => state.GetNpc(o.GiverNpc).Alive), "no pending offer from a dead npc");
            if (state.PendingOffers.Count == 1)
            {
                var giver = state.GetNpc(state.PendingOffers[0].GiverNpc);
                last = null; dialogue.Begin(giver);
                await WaitFor(() => last != null);
                Check(last != null && last.Options.Any(o => o.Offer != null && o.AcceptOffer), $"offer presented in dialogue (giver {giver.DisplayName} alive={giver.Alive} hostile={giver.HostileToPlayer})");
                if (last != null && last.Options.Any(o => o.AcceptOffer)) dialogue.Choose(last.Options.First(o => o.AcceptOffer));
                Check(quests.Active.Any(q => q.TriggerEvent == "OPENING_OPPORTUNITY"), "accepted offer became an active quest");
            }

            // 8. Save / load round trip preserves the story.
            await WaitFor(() => director.InFlightRequests == 0);
            string json = SaveManager.Serialize(state, true);
            var loaded = SaveManager.Deserialize(json);
            Check(loaded.World.Seed.MainConflict == seed.MainConflict, "save keeps the seed");
            Check(loaded.World.GetNpc(contactId).Memory.HasMemoryOf("THREATENED"), "save keeps npc memory");
            Check(loaded.World.Quests.Count == state.Quests.Count && loaded.World.Quests[0].State == "COMPLETED", "save keeps quest state");
            Check(loaded.World.ElapsedMinutes == state.ElapsedMinutes, "save keeps time");
            Check(loaded.World.PlayerReputation["TOWN_GUARD"] == state.PlayerReputation["TOWN_GUARD"], "save keeps reputation");
            Console.WriteLine($"  save size: {json.Length / 1024} KB, AI requests: {ai.TotalRequests}, fallbacks: {ai.FallbacksUsed}, notifications: {notifications.Count}");
            Check(ai.FallbacksUsed == 0, "no validation fallbacks were needed with the mock provider");

            // 9. Validators reject invented content.
            var bad = new DirectorEventValidator().Validate("{\"event_type\":\"DRAGON_ATTACK\",\"location\":\"CASTLE\",\"description\":\"x\",\"world_changes\":[{\"type\":\"TELEPORT\"}]}");
            Check(!bad.Ok, "invented event/location rejected");
            var fixable = new DirectorEventValidator().Validate("```json\n{\"event_type\":\"rumor spread\",\"location\":\"the tavern\",\"description\":\"Talk\",\"urgency\":99,\"world_changes\":[{\"type\":\"reputation\",\"faction\":\"iron hand\",\"change\":-500}]}\n```");
            Check(fixable.Ok && fixable.Value.EventType == "RUMOR_SPREAD" && fixable.Value.Location == "TAVERN" && fixable.Value.WorldChanges[0].Change == -100, "near-miss identifiers auto-corrected");

            director.Dispose();
            quests.Dispose();
        }

        private static async Task WaitFor(Func<bool> cond, int timeoutMs = 3000)
        {
            int waited = 0;
            while (!cond() && waited < timeoutMs) { await Task.Delay(10); waited += 10; }
            if (!cond()) { _failures++; Console.WriteLine("  FAIL: timed out waiting"); }
        }
    }
}
