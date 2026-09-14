using UnityEngine;
using UnityEngine.UI;
using StrikerFive.Core;

namespace StrikerFive.UI
{
    public class HudView : MonoBehaviour
    {
        private RectTransform _root;
        private Text _score, _clock, _banner, _player, _possession;
        private Image _homeBadge, _awayBadge, _power;

        public static HudView Build(Transform canvas)
        {
            var v = canvas.gameObject.AddComponent<HudView>();
            v._root = UIFactory.Full(canvas, "HUD", new Color(0, 0, 0, 0));
            var board = UIFactory.Panel(v._root, "Scoreboard", UIFactory.Bg, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-300, -110), new Vector2(300, -24));
            v._homeBadge = UIFactory.Panel(board, "HomeBadge", Color.red, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(14, 0)).GetComponent<Image>();
            v._awayBadge = UIFactory.Panel(board, "AwayBadge", Color.blue, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-14, 0), new Vector2(0, 0)).GetComponent<Image>();
            v._score = UIFactory.Shadowed(board, "", 40, UIFactory.TextColor, TextAnchor.MiddleCenter);
            ((RectTransform)v._score.transform).offsetMin = new Vector2(20, 26); ((RectTransform)v._score.transform).offsetMax = new Vector2(-20, -4);
            v._clock = UIFactory.Label(board, "", 22, UIFactory.Gold, TextAnchor.LowerCenter, FontStyle.Bold);
            ((RectTransform)v._clock.transform).offsetMin = new Vector2(20, 6); ((RectTransform)v._clock.transform).offsetMax = new Vector2(-20, -56);

            var center = UIFactory.Panel(v._root, "Banner", new Color(0, 0, 0, 0), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-600, 60), new Vector2(600, 240));
            v._banner = UIFactory.Shadowed(center, "", 110, UIFactory.Gold, TextAnchor.MiddleCenter);

            var bottom = UIFactory.Panel(v._root, "Player", UIFactory.Bg, new Vector2(0, 0), new Vector2(0, 0), new Vector2(30, 30), new Vector2(420, 110));
            v._player = UIFactory.Label(bottom, "", 22, UIFactory.TextColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            ((RectTransform)v._player.transform).offsetMin = new Vector2(20, 30); ((RectTransform)v._player.transform).offsetMax = new Vector2(-20, -8);
            var powerBg = UIFactory.Panel(bottom, "PowerBg", new Color(0.2f, 0.2f, 0.25f), new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 12), new Vector2(-20, 24));
            v._power = UIFactory.Panel(powerBg, "Power", UIFactory.Gold, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero).GetComponent<Image>();

            var poss = UIFactory.Panel(v._root, "Possession", UIFactory.Bg, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-300, 30), new Vector2(-30, 80));
            v._possession = UIFactory.Label(poss, "", 18, UIFactory.TextDim, TextAnchor.MiddleCenter);
            return v;
        }

        public void Show() => _root.gameObject.SetActive(true);
        public void Hide() => _root.gameObject.SetActive(false);

        private void Update()
        {
            var mm = MatchManager.Instance;
            if (mm == null || mm.Home == null) return;
            _homeBadge.color = mm.Home.Def.Shirt;
            _awayBadge.color = mm.Away.Def.Shirt;
            _score.text = $"{mm.Home.Def.Short}  <color=#ffd133>{mm.Home.Score} - {mm.Away.Score}</color>  {mm.Away.Def.Short}";
            _clock.text = (mm.Half == 1 ? "1ST HALF  " : "2ND HALF  ") + mm.ClockText;
            bool showBanner = Time.time < mm.BannerUntil && mm.Banner.Length > 0;
            _banner.text = showBanner ? mm.Banner : "";
            _banner.fontSize = mm.Banner.Length > 6 ? 80 : 120;
            var c = mm.Home.Controlled;
            _player.text = c != null ? $"{c.PlayerName}  <color=#aab><size=16>#{c.Number} · {c.Role.ToString().ToLowerInvariant()}</size></color>" + (c.HasBall ? "   <color=#59f284>● BALL</color>" : "") : "";
            float charge = mm.Human != null ? mm.Human.ShootCharge01 : 0f;
            ((RectTransform)_power.transform).anchorMax = new Vector2(charge, 1f);
            int hp = Mathf.RoundToInt(mm.PossessionHome01 * 100f);
            _possession.text = $"Possession  {mm.Home.Def.Short} {hp}%  ·  {mm.Away.Def.Short} {100 - hp}%";
        }
    }
}
