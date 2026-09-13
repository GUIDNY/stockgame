using UnityEngine;
using UnityEngine.UI;
using Echobound.Core;
using Echobound.Dialogue;
using Echobound.NPC;

namespace Echobound.UI
{
    /// <summary>NPC name, response, suggested options and an optional free-text line.</summary>
    public class DialogueView : MonoBehaviour
    {
        private RectTransform _root;
        private Text _name, _line, _mood;
        private RectTransform _options;
        private InputField _input;
        private Button _send;
        private DialogueTurn _turn;

        public static DialogueView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<DialogueView>();
            v._root = UIFactory.Panel(canvas, "Dialogue", UIFactory.Bg, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-620, 30), new Vector2(620, 400));
            var r = v._root;
            v._name = UIFactory.Label(r, "", 26, UIFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            ((RectTransform)v._name.transform).offsetMin = new Vector2(24, 0); ((RectTransform)v._name.transform).offsetMax = new Vector2(-24, -14);
            v._mood = UIFactory.Label(r, "", 16, UIFactory.TextDim, TextAnchor.UpperRight);
            ((RectTransform)v._mood.transform).offsetMin = new Vector2(24, 0); ((RectTransform)v._mood.transform).offsetMax = new Vector2(-24, -20);
            var linePanel = UIFactory.Panel(r, "LinePanel", new Color(0, 0, 0, 0), new Vector2(0, 1), new Vector2(0.55f, 1), new Vector2(24, -330), new Vector2(-12, -50));
            v._line = UIFactory.Label(linePanel, "", 22, UIFactory.TextColor, TextAnchor.UpperLeft);
            var right = UIFactory.Panel(r, "Right", new Color(0, 0, 0, 0), new Vector2(0.55f, 0), new Vector2(1, 1), new Vector2(0, 14), new Vector2(-24, -50));
            var scroll = UIFactory.ScrollView(right, "Options", out _);
            ((RectTransform)scroll.parent.parent).offsetMin = new Vector2(0, 56);
            v._options = scroll;
            var inputRow = UIFactory.Panel(right, "InputRow", new Color(0, 0, 0, 0), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 48));
            UIFactory.HLayout(inputRow, 8f);
            v._input = UIFactory.Input(inputRow, "Say anything… (Enter)");
            v._send = UIFactory.Button(inputRow, "Say", () => v.Submit(), 20);
            var sle = v._send.GetComponent<LayoutElement>(); sle.flexibleWidth = 0; sle.minWidth = 90; sle.preferredWidth = 90;
            v._input.onSubmit.AddListener(_ => v.Submit());
            v._root.gameObject.SetActive(false);
            return v;
        }

        public void Open(NpcState npc)
        {
            _root.gameObject.SetActive(true);
            _name.text = npc.DisplayName;
            _mood.text = npc.Role.Replace('_', ' ').ToLowerInvariant() + " · " + npc.Relationship.Label();
            _line.text = "…";
            _input.text = "";
            UIFactory.Clear(_options);
        }

        public void Close() { _root.gameObject.SetActive(false); _turn = null; }

        public void ShowTurn(DialogueTurn turn)
        {
            _turn = turn;
            if (!_root.gameObject.activeSelf) _root.gameObject.SetActive(true);
            _name.text = turn.NpcName;
            _line.text = turn.NpcLine;
            var npc = GameManager.Instance?.World?.GetNpc(turn.NpcId);
            if (npc != null) _mood.text = npc.Role.Replace('_', ' ').ToLowerInvariant() + " · " + npc.Relationship.Label() + " · " + turn.Mood.ToLowerInvariant();
            UIFactory.Clear(_options);
            bool interactive = !turn.Waiting && !turn.Ended;
            _input.interactable = interactive && turn.AllowFreeText;
            _send.interactable = interactive && turn.AllowFreeText;
            if (turn.Ended)
            {
                UIFactory.Button(_options, "[Continue]", () => GameManager.Instance.EndConversation(), 20);
                return;
            }
            if (turn.Waiting) return;
            foreach (var opt in turn.Options)
            {
                var o = opt;
                var b = UIFactory.Button(_options, o.Label, () => GameManager.Instance.Dialogue.Choose(o), 20, o.Intent == "FREE_TEXT" ? new Color(0.14f, 0.2f, 0.18f) : (Color?)null);
                if (o.Offer != null || o.Resolution != null) b.GetComponentInChildren<Text>().color = UIFactory.Accent;
            }
            if (turn.AllowFreeText) { _input.ActivateInputField(); }
        }

        private void Submit()
        {
            if (_turn == null || _turn.Waiting || _turn.Ended || !_turn.AllowFreeText) return;
            string text = _input.text;
            if (string.IsNullOrWhiteSpace(text)) return;
            _input.text = "";
            GameManager.Instance.Dialogue.Say(text);
        }
    }
}
