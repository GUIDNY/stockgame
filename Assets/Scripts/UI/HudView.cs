using UnityEngine;
using UnityEngine.UI;
using TurboLoop.Core;
using TurboLoop.Track;

namespace TurboLoop.UI
{
    /// <summary>Speed, lap, position, times, countdown, announcements and wrong-way warning.</summary>
    public class HudView : MonoBehaviour
    {
        private RectTransform _root;
        private Text _speed, _speedUnit, _lap, _pos, _times, _center, _wrongWay, _drift;
        private Image _speedFill;
        private int _lastCountdownNumber = -1;

        public static HudView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<HudView>();
            v._root = UIFactory.Full(canvas, "HUD", new Color(0, 0, 0, 0));
            var r = v._root;

            var speedPanel = UIFactory.Panel(r, "Speed", UIFactory.Bg, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-340, 30), new Vector2(-30, 170));
            v._speed = UIFactory.Shadowed(speedPanel, "0", 78, UIFactory.TextColor, TextAnchor.MiddleLeft);
            ((RectTransform)v._speed.transform).offsetMin = new Vector2(24, 24); ((RectTransform)v._speed.transform).offsetMax = new Vector2(-110, -6);
            v._speedUnit = UIFactory.Label(speedPanel, "km/h", 22, UIFactory.TextDim, TextAnchor.LowerRight);
            ((RectTransform)v._speedUnit.transform).offsetMin = new Vector2(0, 40); ((RectTransform)v._speedUnit.transform).offsetMax = new Vector2(-24, 0);
            var bar = UIFactory.Panel(speedPanel, "BarArea", new Color(0, 0, 0, 0), new Vector2(0, 0), new Vector2(1, 0), new Vector2(24, 12), new Vector2(-24, 24));
            v._speedFill = UIFactory.FillBar(bar, UIFactory.Accent, new Color(0.2f, 0.2f, 0.25f));

            var lapPanel = UIFactory.Panel(r, "Lap", UIFactory.Bg, new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 30), new Vector2(330, 170));
            v._pos = UIFactory.Shadowed(lapPanel, "P1", 64, UIFactory.Accent, TextAnchor.MiddleLeft);
            ((RectTransform)v._pos.transform).offsetMin = new Vector2(24, 50); ((RectTransform)v._pos.transform).offsetMax = new Vector2(-24, -10);
            v._lap = UIFactory.Label(lapPanel, "LAP 1 / 3", 26, UIFactory.TextColor, TextAnchor.LowerLeft, FontStyle.Bold);
            ((RectTransform)v._lap.transform).offsetMin = new Vector2(24, 16); ((RectTransform)v._lap.transform).offsetMax = new Vector2(-24, -90);

            var timePanel = UIFactory.Panel(r, "Times", UIFactory.Bg, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -140), new Vector2(360, -30));
            v._times = UIFactory.Label(timePanel, "", 22, UIFactory.TextColor, TextAnchor.MiddleLeft);
            ((RectTransform)v._times.transform).offsetMin = new Vector2(20, 8); ((RectTransform)v._times.transform).offsetMax = new Vector2(-20, -8);

            var center = UIFactory.Panel(r, "Center", new Color(0, 0, 0, 0), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-500, 40), new Vector2(500, 260));
            v._center = UIFactory.Shadowed(center, "", 140, UIFactory.Accent, TextAnchor.MiddleCenter);
            var ww = UIFactory.Panel(r, "WrongWay", new Color(0, 0, 0, 0), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-400, -60), new Vector2(400, 20));
            v._wrongWay = UIFactory.Shadowed(ww, "WRONG WAY", 48, UIFactory.AccentRed, TextAnchor.MiddleCenter);
            var drift = UIFactory.Panel(r, "Drift", new Color(0, 0, 0, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-200, 40), new Vector2(200, 90));
            v._drift = UIFactory.Shadowed(drift, "DRIFT", 34, UIFactory.Accent, TextAnchor.MiddleCenter);
            return v;
        }

        public void Show() => _root.gameObject.SetActive(true);
        public void Hide() => _root.gameObject.SetActive(false);

        private void Update()
        {
            var rm = RaceManager.Instance;
            if (rm == null || rm.Player == null) return;
            var p = rm.Player;
            _speed.text = Mathf.RoundToInt(p.SpeedKmh).ToString();
            UIFactory.SetFill(_speedFill, Mathf.Clamp01(p.SpeedKmh / 205f));
            _speedFill.color = Color.Lerp(UIFactory.Accent, UIFactory.AccentRed, Mathf.Clamp01((p.SpeedKmh - 150f) / 55f));
            _pos.text = $"P{p.Rank}<size=30><color=#aab>/{rm.Cars.Count}</color></size>";
            _lap.text = p.Lap.Finished ? "FINISHED" : $"LAP {Mathf.Min(p.Lap.Lap, p.Lap.TotalLaps)} / {p.Lap.TotalLaps}";
            _times.text = $"LAP   {LapTracker.Format(p.Lap.Finished ? p.Lap.LapTimes[p.Lap.LapTimes.Count - 1] : p.Lap.CurrentLapTime)}\nBEST  {LapTracker.Format(p.Lap.BestLap)}\nTOTAL {LapTracker.Format(rm.State == RaceState.Countdown ? 0f : p.Lap.TotalTime)}";

            // Countdown and announcements share the centre text.
            if (rm.State == RaceState.Countdown)
            {
                int n = Mathf.CeilToInt(rm.Countdown);
                _center.text = n > 3 ? "" : n.ToString();
                _center.color = new Color(1f, 0.78f, 0.15f, 0.6f + 0.4f * (rm.Countdown - Mathf.Floor(rm.Countdown)));
                _center.fontSize = 140 + (int)(60f * (rm.Countdown - Mathf.Floor(rm.Countdown)));
                _lastCountdownNumber = n;
            }
            else
            {
                float age = Time.time - rm.LastEventTime;
                bool show = age < 1.6f;
                _center.text = show ? rm.LastEvent : "";
                _center.fontSize = rm.LastEvent.Length > 4 ? 90 : 140;
                _center.color = new Color(1f, 0.78f, 0.15f, show ? Mathf.Clamp01(1.6f - age) : 0f);
            }
            _wrongWay.enabled = rm.State == RaceState.Racing && p.Lap.WrongWay && Mathf.Repeat(Time.time, 0.6f) < 0.4f;
            _drift.enabled = p.IsDrifting;
        }
    }
}
