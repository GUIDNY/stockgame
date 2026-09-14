using UnityEngine;
using UnityEngine.UI;
using TurboLoop.Core;
using TurboLoop.Track;

namespace TurboLoop.UI
{
    /// <summary>Main menu: track, laps, opponents, car colour, start. The live track orbits behind it.</summary>
    public class MenuView : MonoBehaviour
    {
        private RectTransform _root;
        private Button[] _trackButtons, _lapButtons, _oppButtons, _colorButtons;
        private Text _trackInfo;
        private static readonly int[] LapOptions = { 1, 3, 5 };
        private static readonly int[] OppOptions = { 3, 5, 7 };

        public static MenuView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<MenuView>();
            var rm = RaceManager.Instance;
            v._root = UIFactory.Full(canvas, "Menu", new Color(0, 0, 0, 0.15f));
            var panel = UIFactory.Panel(v._root, "Panel", UIFactory.Bg, new Vector2(0, 0), new Vector2(0, 1), new Vector2(40, 40), new Vector2(640, -40));
            UIFactory.VLayout(panel, 10f, 26);

            UIFactory.Paragraph(panel, "TURBO LOOP", 64, UIFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold, 80);
            UIFactory.Paragraph(panel, "Arcade circuit racing. Hold Space to drift.", 20, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Normal, 30);

            UIFactory.Paragraph(panel, "TRACK", 18, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold, 30);
            v._trackButtons = new Button[TrackLayouts.All.Length];
            for (int i = 0; i < TrackLayouts.All.Length; i++)
            {
                int idx = i;
                v._trackButtons[i] = UIFactory.Button(panel, TrackLayouts.All[i].Name, () => { rm.PreviewTrack(idx); v.Refresh(); }, 22);
            }
            v._trackInfo = UIFactory.Paragraph(panel, "", 17, UIFactory.TextDim, TextAnchor.UpperLeft, FontStyle.Italic, 44);

            UIFactory.Paragraph(panel, "LAPS", 18, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold, 30);
            var lapRow = Row(panel); v._lapButtons = new Button[LapOptions.Length];
            for (int i = 0; i < LapOptions.Length; i++) { int n = LapOptions[i]; v._lapButtons[i] = UIFactory.Button(lapRow, n.ToString(), () => { RaceSettings.Laps = n; v.Refresh(); }, 22); }

            UIFactory.Paragraph(panel, "OPPONENTS", 18, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold, 30);
            var oppRow = Row(panel); v._oppButtons = new Button[OppOptions.Length];
            for (int i = 0; i < OppOptions.Length; i++) { int n = OppOptions[i]; v._oppButtons[i] = UIFactory.Button(oppRow, n.ToString(), () => { RaceSettings.Opponents = n; v.Refresh(); }, 22); }

            UIFactory.Paragraph(panel, "YOUR CAR", 18, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold, 30);
            var colorRow = Row(panel); v._colorButtons = new Button[RaceSettings.Palette.Length];
            for (int i = 0; i < RaceSettings.Palette.Length; i++)
            {
                int idx = i;
                var b = UIFactory.Button(colorRow, "", () => { RaceSettings.PlayerColorIndex = idx; v.Refresh(); }, 14, RaceSettings.Palette[i], 44);
                v._colorButtons[i] = b;
            }

            UIFactory.Paragraph(panel, "", 10, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Normal, 12);
            UIFactory.Button(panel, "START RACE", () => rm.StartRace(), 30, new Color(0.1f, 0.6f, 0.3f), 66);
            UIFactory.Button(panel, "Quit", () => rm.Quit(), 20, new Color(0.3f, 0.12f, 0.12f), 44);

            var hint = UIFactory.Panel(v._root, "Controls", new Color(0, 0, 0, 0.5f), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-560, 30), new Vector2(-30, 120));
            UIFactory.Label(hint, "W / ↑ accelerate   S / ↓ brake, reverse   A D / ← → steer\nSpace handbrake (drift)   R reset to track   Esc pause", 18, UIFactory.TextColor, TextAnchor.MiddleCenter);
            return v;
        }

        private static RectTransform Row(RectTransform parent)
        {
            var row = UIFactory.Panel(parent, "Row", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UIFactory.HLayout(row, 8f);
            var le = row.gameObject.AddComponent<LayoutElement>(); le.minHeight = 48; le.preferredHeight = 48;
            return row;
        }

        public void Show() { _root.gameObject.SetActive(true); Refresh(); }
        public void Hide() => _root.gameObject.SetActive(false);

        private void Refresh()
        {
            for (int i = 0; i < _trackButtons.Length; i++) _trackButtons[i].image.color = i == RaceSettings.TrackIndex ? UIFactory.ButtonSelected : UIFactory.ButtonBg;
            for (int i = 0; i < _lapButtons.Length; i++) _lapButtons[i].image.color = LapOptions[i] == RaceSettings.Laps ? UIFactory.ButtonSelected : UIFactory.ButtonBg;
            for (int i = 0; i < _oppButtons.Length; i++) _oppButtons[i].image.color = OppOptions[i] == RaceSettings.Opponents ? UIFactory.ButtonSelected : UIFactory.ButtonBg;
            for (int i = 0; i < _colorButtons.Length; i++)
            {
                var t = _colorButtons[i].GetComponentInChildren<Text>();
                if (t != null) t.text = i == RaceSettings.PlayerColorIndex ? "●" : "";
                if (t != null) t.color = RaceSettings.PlayerColorIndex == 6 ? Color.black : Color.white;
            }
            var layout = TrackLayouts.All[RaceSettings.TrackIndex];
            _trackInfo.text = layout.Description;
        }
    }
}
