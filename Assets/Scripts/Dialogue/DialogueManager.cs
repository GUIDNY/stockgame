using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Echobound.AI;
using Echobound.AI.Schemas;
using Echobound.Core;
using Echobound.Inventory;
using Echobound.NPC;
using Echobound.Quests;
using Echobound.World;

namespace Echobound.Dialogue
{
    /// <summary>One selectable line for the player. Either a mechanical intent or a free-text suggestion.</summary>
    public class DialogueOption
    {
        public string Label = "";
        public string Intent = "ASK_TOPIC";
        public string Topic = "";
        public string Text = "";
        public int Coins;
        public Item Item;
        public Quest Offer;
        public bool AcceptOffer;
        public QuestResolution Resolution;
        public Quest ResolutionQuest;
    }

    /// <summary>What the UI renders after each exchange.</summary>
    public class DialogueTurn
    {
        public string NpcId = "";
        public string NpcName = "";
        public string NpcLine = "";
        public string Mood = "CALM";
        public List<DialogueOption> Options = new List<DialogueOption>();
        public bool Ended;
        public bool Waiting;
        public bool AllowFreeText = true;
    }

    /// <summary>
    /// Runs conversations. Mechanical effects of intents are deterministic and immediate; the NPC's words come
    /// from the AI (validated) or the mock builder. Every exchange becomes memory and, where important, a fact.
    /// </summary>
    public class DialogueManager
    {
        private readonly WorldState _state;
        private readonly AIRequestManager _ai;
        private readonly KnowledgeSystem _knowledge;
        private readonly QuestManager _quests;
        private readonly InventorySystem _inventory;
        private readonly AIWorldDirector _director;
        private readonly SeededRandom _rng;

        public NpcState Current { get; private set; }
        public bool InConversation => Current != null;
        public event Action<DialogueTurn> TurnReady;
        public event Action ConversationEnded;
        private bool _openingDone;
        private bool _busy;

        public DialogueManager(WorldState state, AIRequestManager ai, KnowledgeSystem knowledge, QuestManager quests, InventorySystem inventory, AIWorldDirector director, SeededRandom rng)
        {
            _state = state; _ai = ai; _knowledge = knowledge; _quests = quests; _inventory = inventory; _director = director; _rng = rng;
        }

        public void Begin(NpcState npc)
        {
            if (npc == null || !npc.Alive || _busy) return;
            if (Current != null) End();
            Current = npc;
            _openingDone = false;
            var offer = _state.PendingOffers.FirstOrDefault(o => o.GiverNpc == npc.NpcId);
            if (offer != null)
            {
                npc.MetPlayer = true;
                var turn = new DialogueTurn
                {
                    NpcId = npc.NpcId, NpcName = npc.DisplayName, Mood = npc.Mood, AllowFreeText = false,
                    NpcLine = $"{Opening(npc)} I have work for someone nobody knows yet. {offer.NarrativeReason} Interested?"
                };
                turn.Options.Add(new DialogueOption { Label = "[Accept the job]", Intent = "OFFER_HELP", Offer = offer, AcceptOffer = true });
                turn.Options.Add(new DialogueOption { Label = "[Decline]", Intent = "LEAVE", Offer = offer, AcceptOffer = false });
                TurnReady?.Invoke(turn);
                return;
            }
            _ = ProcessAsync("GREET", "", "", 0, null);
        }

        private string Opening(NpcState npc) => npc.MetPlayer ? "Good, you're here." : $"You're the newcomer. I'm {npc.DisplayName}.";

        public void Choose(DialogueOption option)
        {
            if (Current == null || option == null || _busy) return;
            if (option.Offer != null)
            {
                _director.ResolveOffer(option.Offer, option.AcceptOffer);
                var npc = Current;
                var line = option.AcceptOffer ? "Good. Don't make me regret it." : "Your loss. Forget we spoke.";
                npc.Memory.Remember(option.AcceptOffer ? "HELPED" : "DECLINED_OFFER", 5, option.AcceptOffer ? $"The stranger accepted my job: {option.Offer.Title}." : $"The stranger turned down my offer: {option.Offer.Title}.", _state.ElapsedMinutes, true);
                if (option.AcceptOffer) GameEvents.RaisePlayerActed(new PlayerAction { Type = PlayerActionType.HELPED, TargetNpcId = npc.NpcId, LocationId = _state.Player.Location, Detail = "accepted " + option.Offer.Title, Importance = 3, GameMinute = _state.ElapsedMinutes });
                TurnReady?.Invoke(new DialogueTurn { NpcId = npc.NpcId, NpcName = npc.DisplayName, NpcLine = line, Mood = npc.Mood, Ended = true });
                return; // the UI calls End() when the player continues
            }
            if (option.Resolution != null && option.ResolutionQuest != null)
            {
                _quests.Resolve(option.ResolutionQuest, option.Resolution);
                TurnReady?.Invoke(new DialogueTurn { NpcId = Current.NpcId, NpcName = Current.DisplayName, NpcLine = option.Resolution.OutcomeText, Mood = Current.Mood, Ended = true });
                return;
            }
            if (option.Intent == "LEAVE") { End(); return; }
            _ = ProcessAsync(option.Intent, option.Text.Length > 0 ? option.Text : option.Label.Trim('[', ']'), option.Topic, option.Coins, option.Item);
        }

        public void Say(string freeText)
        {
            if (Current == null || _busy || string.IsNullOrWhiteSpace(freeText)) return;
            _ = ProcessAsync("FREE_TEXT", freeText.Trim(), freeText.Trim(), 0, null);
        }

        public void End()
        {
            if (Current == null) return;
            Current.TimesTalked++;
            Current = null;
            ConversationEnded?.Invoke();
        }

        private async Task ProcessAsync(string intent, string text, string topic, int coins, Item item)
        {
            var npc = Current;
            if (npc == null) return;
            _busy = true;
            TurnReady?.Invoke(new DialogueTurn { NpcId = npc.NpcId, NpcName = npc.DisplayName, NpcLine = "…", Mood = npc.Mood, Waiting = true, AllowFreeText = false });
            try
            {
                // Mechanical pre-conditions.
                if (intent == "BRIBE")
                {
                    if (!_inventory.TrySpend(coins))
                    {
                        Emit(npc, "You don't have that kind of coin.", false);
                        return;
                    }
                }
                if (intent == "GIVE_ITEM" && item != null) _inventory.Remove(item);

                var payload = new DialoguePayload
                {
                    State = _state, Npc = npc, Intent = intent, Text = text, Topic = string.IsNullOrEmpty(topic) ? text : topic,
                    CoinsOffered = coins, IsOpening = !_openingDone, PlayerLocation = _state.Player.Location,
                    HasEvidenceAgainstNpc = HasEvidenceAgainst(npc),
                    QuestWithNpc = _quests.Active.FirstOrDefault(q => q.GiverNpc == npc.NpcId || q.Objectives.Any(o => !o.Completed && o.TargetNpc == npc.NpcId)),
                    Rng = new SeededRandomHolder { Value = _rng }
                };
                payload.RevealableFacts = npc.Knowledge.Select(_state.Facts.Get).Where(f => f != null).OrderByDescending(f => f.Importance).Take(12).ToList();
                payload.RelevantKnownFacts = _knowledge.WhatNpcKnowsAbout(npc, payload.Topic);
                payload.RumorsHere = _state.Rumors.Where(r => r.Location == npc.CurrentLocation).Select(r => r.Text).Take(3).ToList();

                var request = new AIRequest
                {
                    Kind = AIRequestKind.DIALOGUE, SystemPrompt = PromptLibrary.DialogueSystem, UserPrompt = PromptLibrary.DialogueUser(payload),
                    MaxTokens = _ai.Config.MaxTokensDialogue, Temperature = 0.8f, Payload = payload, Priority = 8
                };
                var result = await _ai.RequestAsync(request, new DialogueResponseValidator(npc.Knowledge), () => MockDialogue.Build(payload, _rng));
                if (Current != npc) return; // conversation was closed meanwhile
                var response = result.Value ?? MockDialogue.Build(payload, _rng);
                string effectiveIntent = intent == "FREE_TEXT" ? response.IntentDetected : intent;
                ApplyMechanics(npc, effectiveIntent, text, coins, item, response);
                _openingDone = true;
                npc.MetPlayer = true;
                Emit(npc, response.NpcLine, response.EndsConversation || npc.HostileToPlayer, response.SuggestedOptions);
            }
            catch (Exception e)
            {
                GameLog.Error("Dialogue failed: " + e);
                Emit(npc, "…", true);
            }
            finally { _busy = false; }
        }

        private void ApplyMechanics(NpcState npc, string intent, string text, int coins, Item item, DialogueResponse r)
        {
            // Relationship + mood from the response, bounded by validation.
            npc.Relationship.ApplyOverall(r.RelationshipChange);
            npc.Relationship.Apply(fear: r.FearChange);
            if (WorldBible.IsValidMood(r.Mood)) npc.Mood = r.Mood;

            // Intent-specific deterministic effects; these also become facts/witness memories through the director.
            switch (intent)
            {
                case "THREATEN":
                    npc.Relationship.Apply(trust: -10, fear: 10, hostility: 10);
                    Record(PlayerActionType.THREATENED, npc, 6, text);
                    break;
                case "BRIBE":
                    npc.Relationship.Apply(trust: 5);
                    Record(PlayerActionType.BRIBED, npc, 4, coins + " coins");
                    break;
                case "ACCUSE":
                    npc.Relationship.Apply(trust: -10, hostility: 10);
                    Record(PlayerActionType.ACCUSED, npc, 6, text);
                    break;
                case "LIE":
                    Record(PlayerActionType.LIED, npc, 3, text);
                    break;
                case "GIVE_ITEM":
                    npc.Relationship.Apply(trust: 10, respect: 5);
                    if (item != null) { npc.Inventory.Add(item.Type); Record(PlayerActionType.DELIVERED, npc, 4, item.Type + " " + item.Label); }
                    break;
                case "OFFER_HELP":
                    npc.Relationship.Apply(trust: 3, respect: 3);
                    Record(PlayerActionType.TALKED, npc, 2, text);
                    break;
                default:
                    Record(PlayerActionType.TALKED, npc, 2, text);
                    break;
            }

            foreach (var id in r.RevealedFactIds)
            {
                var f = _state.Facts.Get(id);
                if (f == null || !npc.Knows(id)) continue;
                f.PlayerKnows = true;
                _state.Discover(f.Summary);
                GameEvents.RaiseNotification("Learned: " + Shorten(f.Summary, 70));
            }
            if (!string.IsNullOrWhiteSpace(r.NewMemorySummary))
                npc.Memory.Remember("CONVERSATION", 3, r.NewMemorySummary, _state.ElapsedMinutes, true);
            if (r.BecomesHostile && !npc.HostileToPlayer)
            {
                npc.HostileToPlayer = true;
                npc.Relationship.Apply(hostility: 40);
                GameEvents.RaiseNotification(npc.DisplayName + " has turned against you.");
            }
            if (!string.IsNullOrEmpty(r.GivesItem) && WorldBible.IsValidItem(r.GivesItem))
                _inventory.Add(Item.Create(r.GivesItem, string.IsNullOrEmpty(r.GivesItemLabel) ? r.GivesItem : r.GivesItemLabel, "Given by " + npc.DisplayName));
        }

        private void Record(PlayerActionType type, NpcState npc, int importance, string detail)
        {
            GameEvents.RaisePlayerActed(new PlayerAction
            {
                Type = type, TargetNpcId = npc.NpcId, LocationId = _state.Player.Location, Detail = detail ?? "",
                Importance = importance, GameMinute = _state.ElapsedMinutes
            });
        }

        private void Emit(NpcState npc, string line, bool ended, List<string> suggested = null)
        {
            var turn = new DialogueTurn { NpcId = npc.NpcId, NpcName = npc.DisplayName, NpcLine = line, Mood = npc.Mood, Ended = ended };
            if (!ended) turn.Options = BuildOptions(npc, suggested);
            TurnReady?.Invoke(turn);
            // When ended, the conversation stays open on the last line until End() is called by the UI (or a new Begin).
        }

        private List<DialogueOption> BuildOptions(NpcState npc, List<string> suggested)
        {
            var list = new List<DialogueOption>();
            // Quest decision through the relevant NPC.
            foreach (var q in _quests.Active.Where(q => q.DecisionPending && (q.GiverNpc == npc.NpcId || q.RelevantNpcs.Contains(npc.NpcId))))
                foreach (var res in _quests.AvailableResolutions(q))
                    list.Add(new DialogueOption { Label = "[" + res.Summary + "]", Intent = "OFFER_HELP", Resolution = res, ResolutionQuest = q });
            // Deliveries.
            foreach (var q in _quests.Active)
            {
                var o = q.CurrentObjective();
                if (o != null && o.Action == "DELIVER" && o.TargetNpc == npc.NpcId)
                {
                    var it = _inventory.Find(o.TargetItem);
                    if (it != null) list.Add(new DialogueOption { Label = $"[Give {it.Label}]", Intent = "GIVE_ITEM", Item = it });
                }
            }
            var victim = _state.GetNpc(_state.Seed.VictimNpcId);
            if (victim != null && victim.NpcId != npc.NpcId) list.Add(new DialogueOption { Label = $"[Ask about {victim.DisplayName}]", Intent = "ASK_TOPIC", Topic = victim.DisplayName + " " + _state.Seed.MainConflict });
            list.Add(new DialogueOption { Label = "[Ask what is going on in town]", Intent = "ASK_TOPIC", Topic = _state.Seed.CentralMystery + " " + _state.Seed.MainConflict });
            list.Add(new DialogueOption { Label = "[Ask about rumors]", Intent = "ASK_RUMORS" });
            if (HasEvidenceAgainst(npc)) list.Add(new DialogueOption { Label = "[Show the evidence]", Intent = "ACCUSE", Topic = npc.Secret });
            else list.Add(new DialogueOption { Label = "[Accuse them of hiding something]", Intent = "ACCUSE", Topic = npc.Secret });
            list.Add(new DialogueOption { Label = "[Threaten]", Intent = "THREATEN" });
            if (_inventory.Coins >= 10) list.Add(new DialogueOption { Label = "[Offer 10 coins]", Intent = "BRIBE", Coins = 10 });
            if (suggested != null)
                foreach (var s in suggested.Take(2)) list.Add(new DialogueOption { Label = s, Intent = "FREE_TEXT", Text = s });
            list.Add(new DialogueOption { Label = "[Leave]", Intent = "LEAVE" });
            return list;
        }

        private bool HasEvidenceAgainst(NpcState npc)
        {
            foreach (var it in _inventory.Items)
            {
                if (string.IsNullOrEmpty(it.FactId)) continue;
                var f = _state.Facts.Get(it.FactId);
                if (f == null) continue;
                if (it.FactId == "TRUTH_CORE" && npc.NpcId == _state.Seed.VillainNpcId) return true;
                if (it.FactId == "TRUTH_ACCOMPLICE" && f.Summary.StartsWith(npc.DisplayName)) return true;
                if (it.FactId == "TRUTH_" + npc.NpcId) return true;
            }
            return false;
        }

        private static string Shorten(string s, int max) => s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }
}
