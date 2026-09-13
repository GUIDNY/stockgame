using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Echobound.Core;
using Echobound.World;

namespace Echobound.UI
{
    /// <summary>
    /// AI Director debug panel (F1): world state, NPC internals, factions, events, AI request/response logs,
    /// token/cost information and development buttons.
    /// </summary>
    public class DebugPanelView : MonoBehaviour
    {
        private RectTransform _root, _worldContent, _npcContent, _aiContent, _npcList;
        private Text _tokens;
        private string _selectedNpc = "NPC_01";
        private float _refreshTimer;
        private int _repFaction;
        private bool _showResponses;

        public static DebugPanelView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<DebugPanelView>();
            v._root = UIFactory.Full(canvas, "DebugPanel", new Color(0.02f, 0.03f, 0.04f, 0.96f));
            UIFactory.Label(v._root, "AI DIRECTOR DEBUG  (F1 to close)", 22, UIFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold).rectTransform.offsetMin = new Vector2(20, 0);

            var cols = UIFactory.Panel(v._root, "Columns", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, new Vector2(16, 120), new Vector2(-16, -44));
            UIFactory.HLayout(cols, 10f);
            var c1 = UIFactory.Panel(cols, "World", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var c2 = UIFactory.Panel(cols, "NPC", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var c3 = UIFactory.Panel(cols, "AI", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            v._worldContent = UIFactory.ScrollView(c1, "WorldScroll", out _);
            var npcSplit = UIFactory.Panel(c2, "Split", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var listArea = UIFactory.Panel(npcSplit, "ListArea", new Color(0, 0, 0, 0), new Vector2(0, 0.72f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            v._npcList = UIFactory.ScrollView(listArea, "NpcList", out _);
            var detailArea = UIFactory.Panel(npcSplit, "DetailArea", new Color(0, 0, 0, 0), new Vector2(0, 0), new Vector2(1, 0.71f), Vector2.zero, Vector2.zero);
            v._npcContent = UIFactory.ScrollView(detailArea, "NpcDetail", out _);
            v._aiContent = UIFactory.ScrollView(c3, "AiScroll", out _);

            var bottom = UIFactory.Panel(v._root, "Buttons", new Color(0, 0, 0, 0), new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 16), new Vector2(-16, 110));
            UIFactory.VLayout(bottom, 6f, 0);
            var row1 = UIFactory.Panel(bottom, "Row1", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); UIFactory.HLayout(row1, 6f);
            var row2 = UIFactory.Panel(bottom, "Row2", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); UIFactory.HLayout(row2, 6f);
            UIFactory.Button(row1, "Generate Event", () => GameManager.Instance.Director.DebugGenerateEvent(), 18);
            UIFactory.Button(row1, "Advance 1 hour", () => GameManager.Instance.Clock.Advance(60), 18);
            UIFactory.Button(row1, "Advance 6 hours", () => GameManager.Instance.Clock.Advance(360), 18);
            UIFactory.Button(row1, "Rep +20 (cycle faction)", () => v.ChangeRep(20), 18);
            UIFactory.Button(row1, "Rep -20 (cycle faction)", () => v.ChangeRep(-20), 18);
            UIFactory.Button(row1, "Deliver opportunity", () => GameManager.Instance.Director.TryDeliverOpportunity(true), 18);
            UIFactory.Button(row2, "Kill selected NPC", () => GameManager.Instance.Director.DebugKillNpc(v._selectedNpc), 18, new Color(0.35f, 0.12f, 0.12f));
            UIFactory.Button(row2, "Trigger Rumor", () => GameManager.Instance.Director.DebugTriggerRumor("The stranger was seen digging behind the ruins at night."), 18);
            UIFactory.Button(row2, "Spawn thugs here", () => GameManager.Instance.Enemies.Spawn(GameManager.Instance.World.Player.Location, 2, "IRON_HAND"), 18);
            UIFactory.Button(row2, "Regenerate World", () => { v.Toggle(); GameManager.Instance.NewGame(); }, 18, new Color(0.12f, 0.25f, 0.35f));
            UIFactory.Button(row2, "Provider: mock", () => GameManager.Instance.SwitchProvider("mock"), 18);
            UIFactory.Button(row2, "Provider: anthropic", () => GameManager.Instance.SwitchProvider("anthropic"), 18);
            UIFactory.Button(row2, "Prompts / Responses", () => { v._showResponses = !v._showResponses; v.Refresh(); }, 18);
            v._tokens = UIFactory.Label(v._root, "", 16, UIFactory.TextDim, TextAnchor.UpperRight);
            v._tokens.rectTransform.offsetMax = new Vector2(-20, 0); v._tokens.rectTransform.offsetMin = new Vector2(0, 0);
            v._root.gameObject.SetActive(false);
            return v;
        }

        public void Toggle()
        {
            bool show = !_root.gameObject.activeSelf;
            _root.gameObject.SetActive(show);
            var gm = GameManager.Instance;
            if (show)
            {
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                if (gm.State == GameState.Playing) { gm.Player.InputEnabled = false; gm.PlayerCamera.InputEnabled = false; }
                Refresh();
            }
            else if (gm.State == GameState.Playing) gm.SetState(GameState.Playing);
        }

        private void ChangeRep(int delta)
        {
            var f = WorldBible.FactionIds[_repFaction % WorldBible.FactionIds.Length];
            _repFaction++;
            GameManager.Instance.Factions.ChangeReputation(f, delta);
        }

        private void Update()
        {
            if (!_root.gameObject.activeSelf) return;
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer <= 0f) { _refreshTimer = 1f; Refresh(); }
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm?.World == null) return;
            var s = gm.World;
            UIFactory.Clear(_worldContent);
            var sb = new StringBuilder();
            sb.AppendLine($"<b>{s.Seed.Title}</b>  seed {s.Seed.WorldId}  provider {gm.AI.Provider.Name}");
            sb.AppendLine($"Time {gm.Clock.Format()}   Tension {s.WorldTension}   Player @ {s.Player.Location}  HP {s.Player.Health}  coins {s.Player.Coins}");
            sb.AppendLine($"Conflict: {s.Seed.MainConflict}");
            sb.AppendLine($"Mystery: {s.Seed.CentralMystery}");
            sb.AppendLine($"Villain: {s.NpcName(s.Seed.VillainNpcId)} ({s.Seed.VillainNpcId})   Victim: {s.NpcName(s.Seed.VictimNpcId)} (alive={s.GetNpc(s.Seed.VictimNpcId)?.Alive})");
            sb.AppendLine($"Ending reached: {(string.IsNullOrEmpty(s.EndingReached) ? "-" : s.EndingReached)}   Pending offers: {s.PendingOffers.Count}   Director in flight: {gm.Director.InFlightRequests}");
            sb.AppendLine("\n<b>FACTIONS</b>");
            foreach (var f in s.Seed.Factions) sb.AppendLine($"{f.Id} [{f.Stance}] rep {gm.Factions.GetReputation(f.Id)}  allies {string.Join(",", f.AlliedWith)}  hostile {string.Join(",", f.HostileTo)}\n   goal: {f.Goal}");
            sb.AppendLine("\n<b>HIDDEN TRUTHS</b>");
            foreach (var h in s.Seed.HiddenTruths) sb.AppendLine($"{h.Id}{(h.IsCore ? "*" : "")} @{h.EvidenceLocation} known by {string.Join(",", h.KnownBy)}  player={(s.Facts.Get(h.Id)?.PlayerKnows ?? false)}\n   {h.Summary}");
            sb.AppendLine("\n<b>QUESTS</b>");
            foreach (var q in s.Quests) sb.AppendLine($"{q.QuestId} [{q.State}] {q.Title} -> {q.CurrentObjectiveText()}");
            sb.AppendLine("\n<b>ACTIVE / APPLIED EVENTS</b>");
            foreach (var e in Enumerable.Reverse(s.AppliedEvents).Take(8)) sb.AppendLine($"[{e.EventType}] u{e.Urgency} {e.Description}  changes={e.WorldChanges.Count}");
            sb.AppendLine("\n<b>RUMORS</b>");
            foreach (var r in s.Rumors) sb.AppendLine($"@{r.Location} {r.Text}");
            sb.AppendLine("\n<b>EVENT LOG</b>");
            foreach (var e in Enumerable.Reverse(s.EventLog).Take(15)) sb.AppendLine($"{e.Minute:0000} {e.Text}");
            UIFactory.Paragraph(_worldContent, sb.ToString(), 15);

            UIFactory.Clear(_npcList);
            foreach (var n in s.Npcs)
            {
                var id = n.NpcId;
                var b = UIFactory.Button(_npcList, $"{n.NpcId} {n.DisplayName} ({n.Role.ToLowerInvariant()}) {(n.Alive ? "@" + n.CurrentLocation : "DEAD")}", () => { _selectedNpc = id; Refresh(); }, 15, id == _selectedNpc ? new Color(0.2f, 0.4f, 0.3f) : (Color?)null, 30);
            }
            UIFactory.Clear(_npcContent);
            var npc = s.GetNpc(_selectedNpc);
            if (npc != null)
            {
                var nb = new StringBuilder();
                nb.AppendLine($"<b>{npc.DisplayName}</b> {npc.Role} faction {npc.Faction} alive={npc.Alive} hp={npc.Health} hostile={npc.HostileToPlayer}");
                nb.AppendLine($"Mood {npc.Mood}   Location {npc.CurrentLocation}{(npc.OverrideLocation != "" ? " (override " + npc.OverrideLocation + ")" : "")}");
                nb.AppendLine($"Relationship: {npc.Relationship.Label()}  trust {npc.Relationship.Trust} fear {npc.Relationship.Fear} respect {npc.Relationship.Respect} hostility {npc.Relationship.Hostility}");
                nb.AppendLine($"Personality: {npc.Personality} / {npc.SpeechStyle}");
                nb.AppendLine($"Goal: {npc.CurrentGoal}");
                nb.AppendLine($"Secret: {npc.Secret}");
                nb.AppendLine("<b>MEMORY (short)</b>");
                foreach (var m in npc.Memory.ShortTerm) nb.AppendLine($" [{m.Importance}] {m.Summary}");
                nb.AppendLine("<b>MEMORY (long)</b>");
                foreach (var m in npc.Memory.LongTerm) nb.AppendLine($" [{m.Importance}] {m.Summary}");
                nb.AppendLine("<b>KNOWLEDGE</b>");
                foreach (var k in npc.Knowledge) nb.AppendLine($" {k}: {s.Facts.Get(k)?.Summary}");
                UIFactory.Paragraph(_npcContent, nb.ToString(), 14);
            }

            UIFactory.Clear(_aiContent);
            var ab = new StringBuilder();
            ab.AppendLine($"<b>AI REQUEST LOG</b>  provider {gm.AI.Provider.Name} online={gm.AI.Provider.IsOnline}");
            foreach (var e in Enumerable.Reverse(gm.AI.Log).Take(25)) ab.AppendLine((e.Success ? "<color=#44e092>" : "<color=#ffb4aa>") + e + "</color>" + (string.IsNullOrEmpty(e.ResponsePreview) ? "" : "\n   → " + e.ResponsePreview));
            if (_showResponses)
            {
                ab.AppendLine("\n<b>LAST PROMPT</b>\n" + (gm.AI.PromptLog.LastOrDefault() ?? "-"));
                ab.AppendLine("\n<b>LAST RESPONSE</b>\n" + (gm.AI.ResponseLog.LastOrDefault() ?? "-"));
            }
            UIFactory.Paragraph(_aiContent, ab.ToString(), 14);
            _tokens.text = $"requests {gm.AI.TotalRequests}  failed {gm.AI.FailedRequests}  fallbacks {gm.AI.FallbacksUsed}  pending {gm.AI.PendingRequests}  tokens in {gm.AI.TotalInputTokens} / out {gm.AI.TotalOutputTokens}  est. cost ${gm.AI.EstimatedCostUsd:0.0000}";
        }
    }
}
