using UnityEngine;
using StrikerFive.Core;

namespace StrikerFive.UI
{
    public class UIManager : MonoBehaviour
    {
        public MenuView Menu { get; private set; }
        public HudView Hud { get; private set; }
        public FullTimeView FullTime { get; private set; }
        public PauseView Pause { get; private set; }

        public static UIManager Create(MatchManager mm)
        {
            var canvas = UIFactory.CreateCanvas("UI");
            canvas.transform.SetParent(mm.transform, false);
            var ui = canvas.gameObject.AddComponent<UIManager>();
            var t = canvas.transform;
            ui.Hud = HudView.Build(t);
            ui.FullTime = FullTimeView.Build(t);
            ui.Pause = PauseView.Build(t);
            ui.Menu = MenuView.Build(t);
            ui.HideAll();
            return ui;
        }

        private void HideAll() { Menu.Hide(); Hud.Hide(); FullTime.Hide(); Pause.Hide(); }
        public void ShowMenu() { HideAll(); Menu.Show(); }
        public void ShowHud() { HideAll(); Hud.Show(); }
        public void ShowPause() { Hud.Show(); Pause.Show(); }
        public void ShowFullTime() { Hud.Show(); FullTime.Show(); }
    }
}
