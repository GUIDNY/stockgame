using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Echobound.Core;
using Echobound.World;

namespace Echobound.UI
{
    /// <summary>Tabbed player menu: Inventory · Journal · Relationships · World. Shows discovered information only.</summary>
    public class PlayerMenuView : MonoBehaviour
    {
        private RectTransform _root, _content;
        private Button[] _tabs;
        private int _tab;
        private static readonly string[] TabNames = { "Inventory", "Journal", "Relationships", "World" };

        public static PlayerMenuView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<PlayerMenuView>();
            v._root = UIFactory.Panel(canvas, "PlayerMenu", UIFactory.Bg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-560, -360), new Vector2(560, 360));
            var tabs = UIFactory.Panel(v._root, "Tabs", new Color(0, 0, 0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -70), new Vector2(-20, -16));
            UIFactory.HLayout(tabs, 8f);
            v._tabs = new Button[TabNames.Length];
            for (int i = 0; i < TabNames.Length; i++) { int idx = i; v._tabs[i] = UIFactory.Button(tabs, TabNames[i], () => v.Show(idx), 22); }
            UIFactory.Button(tabs, "Close (Tab)", () => GameManager.Instance.Resume(), 20, new Color(0.25f, 0.12f, 0.12f));
            var area = UIFactory.Panel(v._root, "Area", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -80));
            v._content = UIFactory.ScrollView(area, "Content", out _);
            v._root.gameObject.SetActive(false);
            return v;
        }

        public void Show(int tab)
        {
            _tab = Mathf.Clamp(tab, 0, TabNames.Length - 1);
            _root.gameObject.SetActive(true);
            for (int i = 0; i < _tabs.Length; i++) _tabs[i].image.color = i == _tab ? new Color(0.2f, 0.4f, 0.3f) : UIFactory.ButtonBg;
            Refresh();
        }

        public void Hide() => _root.gameObject.SetActive(false);

        private void Refresh()
        {
            var gm = GameManager.Instance;
            UIFactory.Clear(_content);
            if (gm?.World == null) return;
            switch (_tab)
            {
                case 0: RenderInventory(gm); break;
                case 1: RenderJournal(gm); break;
                case 2: RenderRelationships(gm); break;
                default: RenderWorld(gm); break;
            }
        }

        private void H(string text) => UIFactory.Paragraph(_content, text, 24, UIFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
        private void P(string text, Color? c = null) => UIFactory.Paragraph(_content, text, 19, c);

        private void RenderInventory(GameManager gm)
        {
            H($"Coins: {gm.World.Player.Coins}     Health: {gm.World.Player.Health}/{gm.World.Player.MaxHealth}");
            if (gm.Inventory.Items.Count == 0) { P("Your pockets are empty.", UIFactory.TextDim); return; }
            foreach (var item in gm.Inventory.Items.ToList())
            {
                var it = item;
                var row = UIFactory.Panel(_content, "Row", new Color(1, 1, 1, 0.04f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                UIFactory.HLayout(row, 8f, 6);
                var le = row.gameObject.AddComponent<LayoutElement>(); le.minHeight = 60;
                var t = UIFactory.Label(row, $"<b>{it.Label}</b>  <color=#aaa>({it.Type.ToLowerInvariant().Replace('_', ' ')})</color>\n<size=16>{it.Description}</size>", 19, UIFactory.TextColor, TextAnchor.MiddleLeft);
                t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 4;
                var b = UIFactory.Button(row, it.Type == "MEDICINE" ? "Use" : it.Type == "LETTER" ? "Read" : "Inspect", () => { gm.UseItem(it); Refresh(); }, 18);
                var ble = b.GetComponent<LayoutElement>(); ble.flexibleWidth = 0; ble.minWidth = 110;
            }
        }

        private void RenderJournal(GameManager gm)
        {
            H("Active");
            var active = gm.Quests.Active.ToList();
            if (active.Count == 0) P("Nothing pressing. Explore, talk, listen.", UIFactory.TextDim);
            foreach (var q in active)
            {
                P($"<b>{q.Title}</b>{(q.State == "MUTATED" ? "  <color=#ffb4aa>(changed)</color>" : "")}{(q.DecisionPending ? "  <color=#44e092>(decision pending)</color>" : "")}");
                P(q.NarrativeReason, UIFactory.TextDim);
                foreach (var o in q.Objectives) P($"   {(o.Completed ? "[x]" : "[ ]")} {o.Description}{(o.Optional ? " (optional)" : "")}{TimeWindow(o)}", o.Completed ? UIFactory.TextDim : UIFactory.TextColor);
                foreach (var h in q.History.Where(h => h.StartsWith("MUTATED"))) P("   ↳ " + h, UIFactory.AccentRed);
                if (q.DecisionPending) { var quest = q; UIFactory.Button(_content, "Decide how to resolve: " + q.Title, () => gm.OpenDecision(quest), 20, new Color(0.15f, 0.35f, 0.25f)); }
            }
            H("Completed");
            foreach (var q in gm.Quests.Completed) P($"<b>{q.Title}</b> — {(q.Resolutions.FirstOrDefault(r => r.Id == q.ChosenResolution)?.Summary ?? "done")}", UIFactory.TextDim);
            H("Failed");
            foreach (var q in gm.Quests.All.Where(q => q.State == "FAILED")) P($"<b>{q.Title}</b> — {q.History.LastOrDefault()}", UIFactory.TextDim);
        }

        private static string TimeWindow(Quests.QuestObjective o) => o.AvailableFromHour >= 0 ? $" <color=#c1c1ff>({o.AvailableFromHour:00}:00–{o.AvailableUntilHour:00}:00)</color>" : "";

        private void RenderRelationships(GameManager gm)
        {
            H("Factions");
            foreach (var f in gm.World.Seed.Factions)
            {
                int rep = gm.Factions.GetReputation(f.Id);
                P($"<b>{WorldBible.FactionName(f.Id)}</b>   {gm.Factions.StandingLabel(f.Id)} ({rep:+0;-0;0})   <size=16><color=#aaa>{f.PublicFace}</color></size>");
            }
            H("People");
            foreach (var n in gm.World.Npcs.OrderBy(n => !n.MetPlayer))
            {
                string status = !n.Alive ? "<color=#ffb4aa>dead</color>" : n.HostileToPlayer ? "<color=#ffb4aa>hostile</color>" : n.MetPlayer ? n.Relationship.Label() : "not met";
                string where = n.Alive && n.MetPlayer ? $"  <size=16><color=#aaa>last seen: {WorldBible.LocationName(n.CurrentLocation)}</color></size>" : "";
                P($"<b>{n.DisplayName}</b>  {n.Role.Replace('_', ' ').ToLowerInvariant()}  ·  {WorldBible.FactionName(n.Faction == WorldBible.NoFaction ? "" : n.Faction)}  ·  {status}{where}");
            }
        }

        private void RenderWorld(GameManager gm)
        {
            H(gm.World.Seed.Title);
            P(gm.World.Seed.MainConflict);
            P(gm.Clock.Format() + "  ·  Tension in town: " + Tension(gm.World.WorldTension), UIFactory.TextDim);
            H("What you know");
            if (gm.World.DiscoveredInfo.Count == 0) P("Nothing yet. Talk to people. Search places. Listen at the tavern.", UIFactory.TextDim);
            foreach (var d in Enumerable.Reverse(gm.World.DiscoveredInfo)) P("• " + d);
            H("Rumors you have heard");
            foreach (var r in gm.World.Rumors.Where(r => r.HeardByPlayer)) P("• " + r.Text, UIFactory.TextDim);
        }

        private static string Tension(int t) => t >= 75 ? "boiling" : t >= 50 ? "high" : t >= 25 ? "uneasy" : "quiet";
    }
}
