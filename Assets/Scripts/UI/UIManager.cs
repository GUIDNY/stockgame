using UnityEngine;
using TurboLoop.Core;

namespace TurboLoop.UI
{
    public class UIManager : MonoBehaviour
    {
        public MenuView Menu { get; private set; }
        public HudView Hud { get; private set; }
        public ResultsView Results { get; private set; }
        public PauseView Pause { get; private set; }

        public static UIManager Create(RaceManager rm)
        {
            var canvas = UIFactory.CreateCanvas("UI");
            canvas.transform.SetParent(rm.transform, false);
            var ui = canvas.gameObject.AddComponent<UIManager>();
            var t = canvas.transform;
            ui.Hud = HudView.Build(t);
            ui.Results = ResultsView.Build(t);
            ui.Pause = PauseView.Build(t);
            ui.Menu = MenuView.Build(t);
            ui.HideAll();
            return ui;
        }

        private void HideAll() { Menu.Hide(); Hud.Hide(); Results.Hide(); Pause.Hide(); }
        public void ShowMenu() { HideAll(); Menu.Show(); }
        public void ShowHud() { HideAll(); Hud.Show(); }
        public void ShowPause() { Hud.Show(); Pause.Show(); }
        public void ShowResults() { Hud.Show(); Results.Show(); }
    }
}
