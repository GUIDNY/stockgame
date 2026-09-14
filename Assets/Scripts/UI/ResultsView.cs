using UnityEngine;
using UnityEngine.UI;
using TurboLoop.Core;
using TurboLoop.Track;

namespace TurboLoop.UI
{
    public class ResultsView : MonoBehaviour
    {
        private RectTransform _root, _rows;
        private Text _title;

        public static ResultsView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<ResultsView>();
            var rm = RaceManager.Instance;
            v._root = UIFactory.Full(canvas, "Results", new Color(0, 0, 0, 0.35f));
            var panel = UIFactory.Panel(v._root, "Panel", UIFactory.Bg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420, -330), new Vector2(420, 330));
            UIFactory.VLayout(panel, 8f, 28);
            v._title = UIFactory.Paragraph(panel, "RACE COMPLETE", 48, UIFactory.Accent, TextAnchor.MiddleCenter, FontStyle.Bold, 70);
            v._rows = UIFactory.Panel(panel, "Rows", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UIFactory.VLayout(v._rows, 4f, 0);
            var le = v._rows.gameObject.AddComponent<LayoutElement>(); le.flexibleHeight = 1; le.minHeight = 300;
            var buttons = UIFactory.Panel(panel, "Buttons", new Color(0, 0, 0, 0), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UIFactory.HLayout(buttons, 10f);
            var ble = buttons.gameObject.AddComponent<LayoutElement>(); ble.minHeight = 60; ble.preferredHeight = 60;
            UIFactory.Button(buttons, "RACE AGAIN", () => rm.RestartRace(), 24, new Color(0.1f, 0.6f, 0.3f), 56);
            UIFactory.Button(buttons, "MENU", () => rm.ShowMenu(), 24, null, 56);
            UIFactory.Button(buttons, "QUIT", () => rm.Quit(), 24, new Color(0.3f, 0.12f, 0.12f), 56);
            return v;
        }

        public void Show()
        {
            var rm = RaceManager.Instance;
            _root.gameObject.SetActive(true);
            int rank = rm.PlayerFinishRank;
            _title.text = rank == 1 ? "YOU WIN!" : $"YOU FINISHED P{rank}";
            _title.color = rank == 1 ? UIFactory.Accent : UIFactory.TextColor;
            UIFactory.Clear(_rows);
            var header = UIFactory.Paragraph(_rows, "<b>POS   DRIVER            BEST LAP      TOTAL</b>", 20, UIFactory.TextDim, TextAnchor.MiddleLeft, FontStyle.Normal, 30);
            foreach (var car in rm.Standings)
            {
                string total = car.Lap.Finished ? LapTracker.Format(car.Lap.FinishTime) : $"racing (lap {Mathf.Min(car.Lap.Lap, car.Lap.TotalLaps)})";
                string line = $"{car.Rank,-5} {car.DriverName,-17} {LapTracker.Format(car.Lap.BestLap),-13} {total}";
                var t = UIFactory.Paragraph(_rows, line, 22, car.IsPlayer ? UIFactory.Accent : UIFactory.TextColor, TextAnchor.MiddleLeft, car.IsPlayer ? FontStyle.Bold : FontStyle.Normal, 32);
                t.font = UIFactory.DefaultFont;
            }
        }

        public void Hide() => _root.gameObject.SetActive(false);
    }
}
