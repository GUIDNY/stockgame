using System;
using UnityEngine;
using UnityEngine.UI;
using Echobound.Core;

namespace Echobound.UI
{
    public class MainMenuView : MonoBehaviour
    {
        private RectTransform _root;
        private Button _continue;
        private Text _status;

        public static MainMenuView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<MainMenuView>();
            v._root = UIFactory.Full(canvas, "MainMenu", new Color(0.03f, 0.04f, 0.06f, 1f));
            UIFactory.Label(v._root, "ECHOBOUND", 96, UIFactory.Accent, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.offsetMin = new Vector2(0, 220);
            var sub = UIFactory.Label(v._root, "A living town. A story that is written by what you do.", 24, UIFactory.TextDim, TextAnchor.MiddleCenter);
            sub.rectTransform.offsetMin = new Vector2(0, 120); sub.rectTransform.offsetMax = new Vector2(0, -120);
            var col = UIFactory.Panel(v._root, "Buttons", new Color(0, 0, 0, 0), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180, -260), new Vector2(180, 40));
            UIFactory.VLayout(col, 12f, 0);
            UIFactory.Button(col, "New Game", () => GameManager.Instance.NewGame(), 26, null, 56);
            v._continue = UIFactory.Button(col, "Continue", () => GameManager.Instance.LoadGame(), 26, null, 56);
            UIFactory.Button(col, "Quit", () => GameManager.Instance.QuitGame(), 26, null, 56);
            v._status = UIFactory.Label(v._root, "", 16, UIFactory.TextDim, TextAnchor.LowerCenter);
            v._status.rectTransform.offsetMin = new Vector2(0, 20); v._status.rectTransform.offsetMax = new Vector2(0, 60);
            return v;
        }

        public void Show()
        {
            _root.gameObject.SetActive(true);
            var gm = GameManager.Instance;
            _continue.interactable = gm != null && gm.HasSave;
            if (gm != null)
                _status.text = $"AI provider: {gm.AI.Provider.Name}" + (gm.AI.Provider.Name == "mock" ? "  (offline story generator; add StreamingAssets/ai_config.local.json or an API key env var for a live model)" : $"  ·  model {gm.Config.Model}") +
                               $"\nSave file: {(gm.HasSave ? gm.SavePath : "none")}";
        }

        public void Hide() => _root.gameObject.SetActive(false);
    }

    public class LoadingView : MonoBehaviour
    {
        private RectTransform _root;
        private Text _text;
        private string _base = "";
        private float _t;

        public static LoadingView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<LoadingView>();
            v._root = UIFactory.Full(canvas, "Loading", new Color(0.03f, 0.04f, 0.06f, 1f));
            v._text = UIFactory.Label(v._root, "", 40, UIFactory.TextColor, TextAnchor.MiddleCenter);
            var hint = UIFactory.Label(v._root, "The AI Director is choosing a conflict, a villain, a victim, alliances, secrets and an opening.", 18, UIFactory.TextDim, TextAnchor.MiddleCenter);
            hint.rectTransform.offsetMax = new Vector2(0, -120);
            return v;
        }

        public void Show(string text) { _base = text; _root.gameObject.SetActive(true); }
        public void Hide() => _root.gameObject.SetActive(false);

        private void Update()
        {
            if (!_root.gameObject.activeSelf) return;
            _t += Time.unscaledDeltaTime;
            int dots = (int)(_t * 2f) % 4;
            _text.text = _base.TrimEnd('.') + new string('.', dots);
        }
    }

    public class IntroView : MonoBehaviour
    {
        private RectTransform _root;
        private Text _text, _sub;
        private string _full = "";
        private float _t;

        public static IntroView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<IntroView>();
            v._root = UIFactory.Full(canvas, "Intro", new Color(0.02f, 0.02f, 0.03f, 1f));
            v._text = UIFactory.Label(v._root, "", 30, UIFactory.TextColor, TextAnchor.MiddleCenter);
            v._text.rectTransform.offsetMin = new Vector2(200, 120); v._text.rectTransform.offsetMax = new Vector2(-200, -80);
            v._text.lineSpacing = 1.4f;
            v._sub = UIFactory.Label(v._root, "", 16, UIFactory.TextDim, TextAnchor.UpperCenter);
            v._sub.rectTransform.offsetMax = new Vector2(0, -30);
            var col = UIFactory.Panel(v._root, "Buttons", new Color(0, 0, 0, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-140, 50), new Vector2(140, 110));
            UIFactory.VLayout(col, 0, 0);
            UIFactory.Button(col, "BEGIN", () => GameManager.Instance.BeginPlaying(), 28, new Color(0.15f, 0.35f, 0.25f), 56);
            return v;
        }

        public void Show(string text, string subtitle) { _full = text; _t = 0f; _sub.text = subtitle; _root.gameObject.SetActive(true); }
        public void Hide() => _root.gameObject.SetActive(false);

        private void Update()
        {
            if (!_root.gameObject.activeSelf) return;
            _t += Time.unscaledDeltaTime * 40f;
            int n = Mathf.Min(_full.Length, (int)_t);
            _text.text = _full.Substring(0, n);
        }
    }

    public class PauseView : MonoBehaviour
    {
        private RectTransform _root;

        public static PauseView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<PauseView>();
            v._root = UIFactory.Full(canvas, "Pause", new Color(0, 0, 0, 0.7f));
            UIFactory.Label(v._root, "PAUSED", 48, UIFactory.TextColor, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.offsetMin = new Vector2(0, 260);
            var col = UIFactory.Panel(v._root, "Buttons", new Color(0, 0, 0, 0), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-160, -200), new Vector2(160, 100));
            UIFactory.VLayout(col, 10f, 0);
            UIFactory.Button(col, "Resume", () => GameManager.Instance.Resume(), 24, null, 50);
            UIFactory.Button(col, "Save (F5)", () => GameManager.Instance.SaveGame(), 24, null, 50);
            UIFactory.Button(col, "Load (F9)", () => GameManager.Instance.LoadGame(), 24, null, 50);
            UIFactory.Button(col, "Main Menu", () => GameManager.Instance.ReturnToMainMenu(), 24, null, 50);
            UIFactory.Button(col, "Quit", () => GameManager.Instance.QuitGame(), 24, null, 50);
            return v;
        }

        public void Show() => _root.gameObject.SetActive(true);
        public void Hide() => _root.gameObject.SetActive(false);
    }

    public class DeathView : MonoBehaviour
    {
        private RectTransform _root;
        private Text _text;
        private Action _onContinue;

        public static DeathView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<DeathView>();
            v._root = UIFactory.Full(canvas, "Death", new Color(0.1f, 0.02f, 0.02f, 0.96f));
            UIFactory.Label(v._root, "BEATEN DOWN", 56, UIFactory.AccentRed, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.offsetMin = new Vector2(0, 200);
            v._text = UIFactory.Label(v._root, "", 24, UIFactory.TextColor, TextAnchor.MiddleCenter);
            v._text.rectTransform.offsetMin = new Vector2(200, 0); v._text.rectTransform.offsetMax = new Vector2(-200, 0);
            var col = UIFactory.Panel(v._root, "Buttons", new Color(0, 0, 0, 0), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-140, -200), new Vector2(140, -140));
            UIFactory.VLayout(col, 0, 0);
            UIFactory.Button(col, "Get up", () => { v._root.gameObject.SetActive(false); v._onContinue?.Invoke(); }, 24, null, 56);
            return v;
        }

        public void Show(string text, Action onContinue) { _text.text = text; _onContinue = onContinue; _root.gameObject.SetActive(true); }
        public void Hide() => _root.gameObject.SetActive(false);
    }
}
