using UnityEngine;
using UnityEngine.UI;
using StrikerFive.Core;

namespace StrikerFive.UI
{
    public class FullTimeView : MonoBehaviour
    {
        private RectTransform _root;
        private Text _title, _score, _sub;

        public static FullTimeView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<FullTimeView>();
            var mm = MatchManager.Instance;
            v._root = UIFactory.Full(canvas, "FullTime", new Color(0, 0, 0, 0.45f));
            var panel = UIFactory.Panel(v._root, "Panel", UIFactory.Bg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-360, -240), new Vector2(360, 240));
            UIFactory.VLayout(panel, 12f, 30);
            v._title = UIFactory.Paragraph(panel, "FULL TIME", 48, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, 70);
            v._score = UIFactory.Paragraph(panel, "", 56, UIFactory.TextColor, TextAnchor.MiddleCenter, FontStyle.Bold, 80);
            v._sub = UIFactory.Paragraph(panel, "", 22, UIFactory.TextDim, TextAnchor.MiddleCenter, FontStyle.Normal, 60);
            var buttons = UIFactory.Row(panel, 60f);
            UIFactory.Button(buttons, "REMATCH", () => mm.StartMatch(), 24, new Color(0.1f, 0.6f, 0.3f), 56);
            UIFactory.Button(buttons, "MENU", () => mm.ShowMenu(), 24, null, 56);
            UIFactory.Button(buttons, "QUIT", () => mm.Quit(), 24, new Color(0.3f, 0.12f, 0.12f), 56);
            return v;
        }

        public void Show()
        {
            var mm = MatchManager.Instance;
            _root.gameObject.SetActive(true);
            _score.text = $"{mm.Home.Def.Short} {mm.Home.Score} - {mm.Away.Score} {mm.Away.Def.Short}";
            _title.text = mm.Home.Score > mm.Away.Score ? "YOU WIN!" : mm.Home.Score < mm.Away.Score ? "DEFEAT" : "DRAW";
            _title.color = mm.Home.Score > mm.Away.Score ? UIFactory.Gold : UIFactory.TextColor;
            int hp = Mathf.RoundToInt(mm.PossessionHome01 * 100f);
            _sub.text = $"{mm.Home.Def.Name} vs {mm.Away.Def.Name}\nPossession {hp}% - {100 - hp}%";
        }

        public void Hide() => _root.gameObject.SetActive(false);
    }
}
