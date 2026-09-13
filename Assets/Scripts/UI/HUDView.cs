using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Echobound.Core;

namespace Echobound.UI
{
    /// <summary>Health, coins, time/location, current objective, interaction prompt and a small notification feed.</summary>
    public class HUDView : MonoBehaviour
    {
        private RectTransform _root;
        private Image _healthFill;
        private Text _healthText, _coinsText, _timeText, _locationText, _objectiveText, _promptText, _hintText;
        private RectTransform _notifications;
        private Image _flash;
        private readonly List<(Text text, float until)> _notes = new List<(Text, float)>();
        private float _flashTimer;

        public static HUDView Build(Transform canvas)
        {
            var hud = canvas.gameObject.AddComponent<HUDView>();
            hud._root = UIFactory.Full(canvas, "HUD", new Color(0, 0, 0, 0));
            var r = hud._root;

            var flash = UIFactory.Full(r, "Flash", new Color(0.8f, 0.1f, 0.1f, 0f));
            hud._flash = flash.GetComponent<Image>(); hud._flash.raycastTarget = false;

            var topLeft = UIFactory.Panel(r, "TopLeft", UIFactory.Bg, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -120), new Vector2(360, -20));
            var bar = UIFactory.Panel(topLeft, "HealthBar", new Color(0, 0, 0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -42), new Vector2(-14, -14));
            hud._healthFill = UIFactory.FillBar(bar, UIFactory.Accent, new Color(0.2f, 0.2f, 0.22f));
            hud._healthText = UIFactory.Label(bar, "100 / 100", 18, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold);
            hud._coinsText = UIFactory.Label(topLeft, "Coins: 0", 20, UIFactory.TextColor, TextAnchor.MiddleLeft);
            ((RectTransform)hud._coinsText.transform).offsetMin = new Vector2(14, 30); ((RectTransform)hud._coinsText.transform).offsetMax = new Vector2(-14, -48);
            hud._timeText = UIFactory.Label(topLeft, "Day 1 18:00", 18, UIFactory.TextDim, TextAnchor.LowerLeft);
            ((RectTransform)hud._timeText.transform).offsetMin = new Vector2(14, 8); ((RectTransform)hud._timeText.transform).offsetMax = new Vector2(-14, -72);
            hud._locationText = UIFactory.Label(topLeft, "", 18, UIFactory.AccentBlue, TextAnchor.LowerRight);
            ((RectTransform)hud._locationText.transform).offsetMin = new Vector2(14, 8); ((RectTransform)hud._locationText.transform).offsetMax = new Vector2(-14, -72);

            var topRight = UIFactory.Panel(r, "Objective", UIFactory.Bg, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-520, -110), new Vector2(-20, -20));
            UIFactory.Label(topRight, "OBJECTIVE", 14, UIFactory.TextDim, TextAnchor.UpperLeft, FontStyle.Bold);
            ((RectTransform)topRight.GetChild(0)).offsetMin = new Vector2(14, 0); ((RectTransform)topRight.GetChild(0)).offsetMax = new Vector2(-14, -10);
            hud._objectiveText = UIFactory.Label(topRight, "", 20, UIFactory.TextColor, TextAnchor.UpperLeft);
            ((RectTransform)hud._objectiveText.transform).offsetMin = new Vector2(14, 8); ((RectTransform)hud._objectiveText.transform).offsetMax = new Vector2(-14, -30);

            var prompt = UIFactory.Panel(r, "Prompt", new Color(0, 0, 0, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-300, 120), new Vector2(300, 160));
            hud._promptText = UIFactory.Label(prompt, "", 24, UIFactory.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);

            var hint = UIFactory.Panel(r, "Hint", new Color(0, 0, 0, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-500, 16), new Vector2(500, 44));
            hud._hintText = UIFactory.Label(hint, "WASD move · Shift run · E interact · LMB attack · Tab inventory · J journal · Esc pause · F1 director debug", 16, UIFactory.TextDim, TextAnchor.MiddleCenter);

            hud._notifications = UIFactory.Panel(r, "Notifications", new Color(0, 0, 0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 60), new Vector2(620, 420));
            var v = UIFactory.VLayout(hud._notifications, 4f, 0); v.childAlignment = TextAnchor.LowerLeft;
            return hud;
        }

        public void Show() => _root.gameObject.SetActive(true);
        public void Hide() => _root.gameObject.SetActive(false);
        public void SetPrompt(string text) { if (_promptText != null) _promptText.text = text; }
        public void SetLocation(string text) { if (_locationText != null) _locationText.text = text; }
        public void Flash() { _flashTimer = 0.35f; }

        public void Notify(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var t = UIFactory.Paragraph(_notifications, text, 18, UIFactory.TextColor);
            var bg = t.gameObject.AddComponent<Outline>(); bg.effectColor = new Color(0, 0, 0, 0.8f); bg.effectDistance = new Vector2(1, -1);
            _notes.Add((t, Time.unscaledTime + 6f));
            while (_notes.Count > 7) { Destroy(_notes[0].text.gameObject); _notes.RemoveAt(0); }
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm?.World != null)
            {
                var p = gm.World.Player;
                UIFactory.SetFill(_healthFill, p.Health / (float)p.MaxHealth);
                _healthText.text = $"{p.Health} / {p.MaxHealth}";
                _coinsText.text = $"Coins: {p.Coins}";
                _timeText.text = gm.Clock.Format() + (gm.Clock.IsNight ? "  night" : "");
                _objectiveText.text = gm.Quests?.CurrentObjectiveText() ?? "";
            }
            for (int i = _notes.Count - 1; i >= 0; i--)
            {
                float remaining = _notes[i].until - Time.unscaledTime;
                if (remaining <= 0f) { Destroy(_notes[i].text.gameObject); _notes.RemoveAt(i); continue; }
                var c = _notes[i].text.color; c.a = Mathf.Clamp01(remaining); _notes[i].text.color = c;
            }
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                var c = _flash.color; c.a = Mathf.Clamp01(_flashTimer / 0.35f) * 0.45f; _flash.color = c;
            }
        }
    }
}
