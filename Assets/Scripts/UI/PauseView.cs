using UnityEngine;
using TurboLoop.Core;

namespace TurboLoop.UI
{
    public class PauseView : MonoBehaviour
    {
        private RectTransform _root;

        public static PauseView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<PauseView>();
            var rm = RaceManager.Instance;
            v._root = UIFactory.Full(canvas, "Pause", new Color(0, 0, 0, 0.6f));
            var panel = UIFactory.Panel(v._root, "Panel", UIFactory.Bg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200, -190), new Vector2(200, 190));
            UIFactory.VLayout(panel, 10f, 24);
            UIFactory.Paragraph(panel, "PAUSED", 40, UIFactory.Accent, TextAnchor.MiddleCenter, FontStyle.Bold, 60);
            UIFactory.Button(panel, "Resume", () => rm.Resume(), 24);
            UIFactory.Button(panel, "Restart Race", () => rm.RestartRace(), 24);
            UIFactory.Button(panel, "Main Menu", () => rm.ShowMenu(), 24);
            UIFactory.Button(panel, "Quit", () => rm.Quit(), 24, new Color(0.3f, 0.12f, 0.12f));
            return v;
        }

        public void Show() => _root.gameObject.SetActive(true);
        public void Hide() => _root.gameObject.SetActive(false);
    }
}
