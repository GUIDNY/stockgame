using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Echobound.Core;
using Echobound.Quests;

namespace Echobound.UI
{
    /// <summary>Owns every screen, routes hotkeys per game state and bridges simulation events to the HUD.</summary>
    public class UIManager : MonoBehaviour
    {
        public HUDView HUD { get; private set; }
        public DialogueView Dialogue { get; private set; }
        public MainMenuView MainMenu { get; private set; }
        public LoadingView Loading { get; private set; }
        public IntroView Intro { get; private set; }
        public PauseView Pause { get; private set; }
        public DeathView Death { get; private set; }
        public PlayerMenuView PlayerMenu { get; private set; }
        public DecisionView Decision { get; private set; }
        public DebugPanelView Debug { get; private set; }
        private Image _fade;
        private GameManager _gm;

        public static UIManager Create(GameManager gm)
        {
            var canvas = UIFactory.CreateCanvas("UI");
            canvas.transform.SetParent(gm.transform, false);
            var ui = canvas.gameObject.AddComponent<UIManager>();
            ui._gm = gm;
            var t = canvas.transform;
            ui.HUD = HUDView.Build(t);
            ui.Dialogue = DialogueView.Build(t);
            ui.Decision = DecisionView.Build(t);
            ui.PlayerMenu = PlayerMenuView.Build(t);
            ui.Pause = PauseView.Build(t);
            ui.Death = DeathView.Build(t);
            ui.Intro = IntroView.Build(t);
            ui.Loading = LoadingView.Build(t);
            ui.MainMenu = MainMenuView.Build(t);
            ui.Debug = DebugPanelView.Build(t);
            var fade = UIFactory.Full(t, "Fade", new Color(0, 0, 0, 0));
            ui._fade = fade.GetComponent<Image>(); ui._fade.raycastTarget = false;
            fade.SetAsLastSibling();
            ui.HideAll();
            return ui;
        }

        private void HideAll()
        {
            HUD.Hide(); Dialogue.Close(); Decision.Hide(); PlayerMenu.Hide(); Pause.Hide(); Death.Hide(); Intro.Hide(); Loading.Hide(); MainMenu.Hide();
        }

        public void ShowMainMenu() { HideAll(); MainMenu.Show(); }
        public void ShowLoading(string text) { HideAll(); Loading.Show(text); }
        public void ShowIntro(string text, string subtitle) { HideAll(); Intro.Show(text, subtitle); }
        public void ShowHUD() { HideAll(); HUD.Show(); }
        public void ShowPause() { HUD.Show(); Pause.Show(); }
        public void ShowPlayerMenu(int tab) { HUD.Show(); PlayerMenu.Show(tab); }
        public void ShowDecision(Quest q) { HUD.Show(); Decision.Show(q); }
        public void ShowDeath(string text, Action onContinue) { HideAll(); Death.Show(text, onContinue); }

        /// <summary>Subscribes HUD feedback to the current world's events. Called after every world initialization.</summary>
        public void BindWorld()
        {
            GameEvents.Notification += HUD.Notify;
            GameEvents.ReputationChanged += (f, v, d) => { if (Mathf.Abs(d) >= 5) HUD.Notify($"{World.WorldBible.FactionName(f)}: {(d > 0 ? "+" : "")}{d} reputation"); };
            GameEvents.WorldInfoDiscovered += s => HUD.Notify("Journal updated");
        }

        public void Fade(float duration, Action atBlack) => StartCoroutine(FadeRoutine(duration, atBlack));

        private IEnumerator FadeRoutine(float duration, Action atBlack)
        {
            float t = 0f;
            while (t < duration / 2f) { t += Time.unscaledDeltaTime; SetFade(t / (duration / 2f)); yield return null; }
            SetFade(1f);
            atBlack?.Invoke();
            t = 0f;
            while (t < duration / 2f) { t += Time.unscaledDeltaTime; SetFade(1f - t / (duration / 2f)); yield return null; }
            SetFade(0f);
        }

        private void SetFade(float a) { var c = _fade.color; c.a = Mathf.Clamp01(a); _fade.color = c; }

        private void Update()
        {
            if (_gm == null) return;
            if (Input.GetKeyDown(KeyCode.F1) && _gm.HasWorld) Debug.Toggle();
            switch (_gm.State)
            {
                case GameState.Playing:
                    if (Input.GetKeyDown(KeyCode.Escape)) _gm.Pause();
                    else if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I)) _gm.OpenMenu(0);
                    else if (Input.GetKeyDown(KeyCode.J)) _gm.OpenMenu(1);
                    else if (Input.GetKeyDown(KeyCode.R)) _gm.OpenMenu(2);
                    else if (Input.GetKeyDown(KeyCode.M)) _gm.OpenMenu(3);
                    else if (Input.GetKeyDown(KeyCode.F5)) _gm.SaveGame();
                    else if (Input.GetKeyDown(KeyCode.F9)) _gm.LoadGame();
                    break;
                case GameState.Paused:
                    if (Input.GetKeyDown(KeyCode.Escape)) _gm.Resume();
                    break;
                case GameState.Menu:
                    if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)) _gm.Resume();
                    break;
                case GameState.Dialogue:
                    if (Input.GetKeyDown(KeyCode.Escape)) _gm.EndConversation();
                    break;
                case GameState.Decision:
                    if (Input.GetKeyDown(KeyCode.Escape)) { Decision.Hide(); _gm.DeferDecision(); }
                    break;
                case GameState.Intro:
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) _gm.BeginPlaying();
                    break;
            }
        }
    }
}
