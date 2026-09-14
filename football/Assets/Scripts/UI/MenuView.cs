using UnityEngine;
using UnityEngine.UI;
using StrikerFive.Core;

namespace StrikerFive.UI
{
    public class MenuView : MonoBehaviour
    {
        private RectTransform _root;
        private Button[] _home, _away, _half, _diff;
        private static readonly float[] HalfOptions = { 2f, 3f, 5f };
        private static readonly string[] DiffNames = { "Easy", "Normal", "Hard" };

        public static MenuView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<MenuView>();
            var mm = MatchManager.Instance;
            v._root = UIFactory.Full(canvas, "Menu", new Color(0, 0, 0, 0.2f));
            var panel = UIFactory.Panel(v._root, "Panel", UIFactory.Bg, new Vector2(0, 0), new Vector2(0, 1), new Vector2(40, 40), new Vector2(700, -40));
            UIFactory.VLayout(panel, 8f, 26);
            UIFactory.Paragraph(panel, "STRIKER FIVE", 64, UIFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold, 80);
            UIFactory.Paragraph(panel, "Five-a-side arcade football.", 20, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Normal, 30);

            UIFactory.Paragraph(panel, "YOUR TEAM", 18, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold, 28);
            v._home = TeamGrid(panel, i => { MatchSettings.HomeTeam = i; if (MatchSettings.AwayTeam == i) MatchSettings.AwayTeam = (i + 1) % MatchSettings.Teams.Length; v.Refresh(); });
            UIFactory.Paragraph(panel, "OPPONENT", 18, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold, 28);
            v._away = TeamGrid(panel, i => { if (i != MatchSettings.HomeTeam) { MatchSettings.AwayTeam = i; v.Refresh(); } });

            UIFactory.Paragraph(panel, "MINUTES PER HALF", 18, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold, 28);
            var halfRow = UIFactory.Row(panel); v._half = new Button[HalfOptions.Length];
            for (int i = 0; i < HalfOptions.Length; i++) { float m = HalfOptions[i]; v._half[i] = UIFactory.Button(halfRow, m.ToString("0"), () => { MatchSettings.HalfMinutes = m; v.Refresh(); }, 22); }
            UIFactory.Paragraph(panel, "DIFFICULTY", 18, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold, 28);
            var diffRow = UIFactory.Row(panel); v._diff = new Button[3];
            for (int i = 0; i < 3; i++) { int d = i; v._diff[i] = UIFactory.Button(diffRow, DiffNames[i], () => { MatchSettings.Difficulty = d; v.Refresh(); }, 22); }

            UIFactory.Paragraph(panel, "", 10, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Normal, 8);
            UIFactory.Button(panel, "KICK OFF", () => mm.StartMatch(), 30, new Color(0.1f, 0.6f, 0.3f), 66);
            UIFactory.Button(panel, "Quit", () => mm.Quit(), 20, new Color(0.3f, 0.12f, 0.12f), 44);

            var hint = UIFactory.Panel(v._root, "Controls", new Color(0, 0, 0, 0.5f), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-620, 30), new Vector2(-30, 130));
            UIFactory.Label(hint, "WASD / arrows move   Space sprint\nJ pass   K shoot (hold for power)   L lob\nJ or K without the ball: tackle   Q switch player   Esc pause", 18, UIFactory.TextColor, TextAnchor.MiddleCenter);
            return v;
        }

        private static Button[] TeamGrid(RectTransform panel, System.Action<int> onPick)
        {
            var buttons = new Button[MatchSettings.Teams.Length];
            RectTransform row = null;
            for (int i = 0; i < MatchSettings.Teams.Length; i++)
            {
                if (i % 3 == 0) row = UIFactory.Row(panel, 50f);
                int idx = i;
                var def = MatchSettings.Teams[i];
                buttons[i] = UIFactory.Button(row, def.Name, () => onPick(idx), 18, def.Shirt, 48);
                var t = buttons[i].GetComponentInChildren<Text>();
                if (t != null) t.color = def.Shirt.r + def.Shirt.g + def.Shirt.b > 1.8f ? Color.black : Color.white;
            }
            return buttons;
        }

        public void Show() { _root.gameObject.SetActive(true); Refresh(); }
        public void Hide() => _root.gameObject.SetActive(false);

        private void Refresh()
        {
            for (int i = 0; i < _home.Length; i++)
            {
                var def = MatchSettings.Teams[i];
                _home[i].image.color = i == MatchSettings.HomeTeam ? def.Shirt : Color.Lerp(def.Shirt, Color.black, 0.55f);
                _away[i].image.color = i == MatchSettings.AwayTeam ? def.Shirt : Color.Lerp(def.Shirt, Color.black, 0.55f);
                _away[i].interactable = i != MatchSettings.HomeTeam;
            }
            for (int i = 0; i < _half.Length; i++) _half[i].image.color = Mathf.Approximately(HalfOptions[i], MatchSettings.HalfMinutes) ? UIFactory.ButtonSelected : UIFactory.ButtonBg;
            for (int i = 0; i < _diff.Length; i++) _diff[i].image.color = i == MatchSettings.Difficulty ? UIFactory.ButtonSelected : UIFactory.ButtonBg;
        }
    }
}
