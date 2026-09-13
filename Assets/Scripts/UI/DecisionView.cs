using UnityEngine;
using UnityEngine.UI;
using Echobound.Core;
using Echobound.Quests;

namespace Echobound.UI
{
    /// <summary>Presents a quest's possible resolutions. No good/evil colouring: every option is a plain choice.</summary>
    public class DecisionView : MonoBehaviour
    {
        private RectTransform _root;
        private Text _title, _reason;
        private RectTransform _options;

        public static DecisionView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<DecisionView>();
            v._root = UIFactory.Panel(canvas, "Decision", UIFactory.Bg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420, -300), new Vector2(420, 300));
            v._title = UIFactory.Label(v._root, "", 30, UIFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            v._title.rectTransform.offsetMin = new Vector2(30, 0); v._title.rectTransform.offsetMax = new Vector2(-30, -20);
            v._reason = UIFactory.Label(v._root, "", 20, UIFactory.TextColor, TextAnchor.UpperLeft);
            v._reason.rectTransform.offsetMin = new Vector2(30, 0); v._reason.rectTransform.offsetMax = new Vector2(-30, -70);
            var area = UIFactory.Panel(v._root, "Area", new Color(0, 0, 0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(30, 24), new Vector2(-30, -190));
            v._options = UIFactory.ScrollView(area, "Options", out _);
            v._root.gameObject.SetActive(false);
            return v;
        }

        public void Show(Quest quest)
        {
            var gm = GameManager.Instance;
            _root.gameObject.SetActive(true);
            _title.text = quest.Title;
            _reason.text = quest.NarrativeReason + "\n\nHow do you settle this?";
            UIFactory.Clear(_options);
            foreach (var res in gm.Quests.AvailableResolutions(quest))
            {
                var r = res;
                UIFactory.Button(_options, r.Summary, () => { Hide(); gm.ResolveDecision(quest, r); }, 22, null, 50);
            }
            UIFactory.Button(_options, "Decide later (talk to the people involved first)", () => { Hide(); gm.DeferDecision(); }, 18, new Color(0.12f, 0.12f, 0.14f), 44);
        }

        public void Hide() => _root.gameObject.SetActive(false);
    }
}
