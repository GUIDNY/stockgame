using System.Collections.Generic;
using UnityEngine;
using StrikerFive.Audio;
using StrikerFive.Ball;
using StrikerFive.Pitch;
using StrikerFive.Players;
using StrikerFive.UI;

namespace StrikerFive.Core
{
    public enum MatchState { Menu, KickOff, Playing, Restart, Goal, HalfTime, FullTime, Paused }

    /// <summary>Composition root and referee: builds the stadium, runs the clock, possession, rules and restarts.</summary>
    public class MatchManager : MonoBehaviour
    {
        public static MatchManager Instance { get; private set; }

        public MatchState State { get; private set; } = MatchState.Menu;
        public BallController Ball { get; private set; }
        public TeamRuntime Home { get; private set; }
        public TeamRuntime Away { get; private set; }
        public TeamRuntime HumanTeam => Home;
        public PitchBuilder Pitch { get; private set; }
        public BroadcastCamera Cam { get; private set; }
        public UIManager UI { get; private set; }
        public StadiumAudio Audio { get; private set; }
        public HumanController Human { get; private set; }
        public float Clock { get; private set; }
        public int Half { get; private set; } = 1;
        public string Banner { get; private set; } = "";
        public float BannerUntil { get; private set; }
        public TeamRuntime RestartTeam { get; private set; }

        private Transform _world, _playersRoot;
        private float _stateTimer;
        private MatchState _afterTimer;
        private float _possessionHome, _possessionTotal;
        public float PossessionHome01 => _possessionTotal > 0f ? _possessionHome / _possessionTotal : 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 1;
            _world = new GameObject("World").transform; _world.SetParent(transform, false);
            _playersRoot = new GameObject("Players").transform; _playersRoot.SetParent(transform, false);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(_world, false);
            sun.type = LightType.Directional;
            Materials.SetupSky(sun);
            Pitch = PitchBuilder.Build(_world);
            Ball = BallController.Create(_world);
            Ball.PlaceAt(Vector3.zero);
            Cam = BroadcastCamera.Create();
            Cam.Target = Ball.transform;
            Audio = StadiumAudio.Create(transform);
            UI = UIManager.Create(this);
            ShowMenu();
        }

        // ------------------------------------------------------------------ flow
        public void ShowMenu()
        {
            ClearPlayers();
            Ball.PlaceAt(Vector3.zero);
            Cam.MenuMode = true;
            SetState(MatchState.Menu);
            UI.ShowMenu();
        }

        public void StartMatch()
        {
            ClearPlayers();
            Clock = 0f; Half = 1; _possessionHome = 0f; _possessionTotal = 0f;
            int away = MatchSettings.AwayTeam == MatchSettings.HomeTeam ? (MatchSettings.HomeTeam + 1) % MatchSettings.Teams.Length : MatchSettings.AwayTeam;
            Home = BuildTeam(MatchSettings.Teams[MatchSettings.HomeTeam], +1, true, 0);
            Away = BuildTeam(MatchSettings.Teams[away], -1, false, 1);
            Home.AI = new TeamAI(Home, this);
            Away.AI = new TeamAI(Away, this);
            Human = new HumanController(Home, this);
            Cam.MenuMode = false;
            UI.ShowHud();
            KickOff(Away);
            Audio.Whistle(1);
        }

        private TeamRuntime BuildTeam(TeamDef def, int side, bool human, int seed)
        {
            var team = new TeamRuntime { Def = def, Side = side, IsHuman = human };
            var rng = new System.Random(def.Name.GetHashCode() + seed);
            var names = new List<string>(MatchSettings.Surnames);
            int number = 1;
            foreach (var role in Formation.Roles)
            {
                string name = names[rng.Next(names.Count)]; names.Remove(name);
                var (x, z) = Formation.Home(role, side, 0f, 0f, false);
                var p = PlayerFactory.Create(team, role, name, role == Role.Goalkeeper ? 1 : ++number + 3, new Vector3(x, 0f, z), _playersRoot, role == Role.Goalkeeper);
                p.SpeedScale = human ? 1f : MatchSettings.AiSpeed;
                p.Face(new Vector3(side, 0f, 0f));
                team.Players.Add(p);
                if (role == Role.Goalkeeper) team.Goalkeeper = p;
            }
            return team;
        }

        private void ClearPlayers()
        {
            for (int i = _playersRoot.childCount - 1; i >= 0; i--) Destroy(_playersRoot.GetChild(i).gameObject);
            Home = null; Away = null; Human = null;
            Ball.Owner = null; Ball.LastTouch = null;
        }

        private void KickOff(TeamRuntime kicking)
        {
            Ball.PlaceAt(Vector3.zero);
            foreach (var team in new[] { Home, Away })
                foreach (var p in team.Players)
                {
                    var (x, z) = Formation.Home(p.Role, team.Side, 0f, 0f, team == kicking);
                    if (team == kicking && p.Role == Role.Forward) { x = -1.2f * team.Side; z = 0.6f; }
                    if (team != kicking && p.Role == Role.Forward) x = -9f * team.Side;
                    p.transform.position = new Vector3(x, 0f, z);
                    p.Face(new Vector3(team.Side, 0f, 0f));
                    p.MoveDirection(Vector3.zero, false);
                }
            RestartTeam = kicking;
            Freeze(MatchState.KickOff, 1.2f, MatchState.Playing);
            Show(Half == 1 && Clock < 1f ? "KICK OFF" : "", 1.2f);
        }

        private void Freeze(MatchState during, float seconds, MatchState after)
        {
            SetState(during);
            _stateTimer = seconds;
            _afterTimer = after;
        }

        private void SetState(MatchState s)
        {
            State = s;
            Time.timeScale = s == MatchState.Paused ? 0f : 1f;
            AudioListener.pause = s == MatchState.Paused;
            Cursor.visible = s == MatchState.Menu || s == MatchState.Paused || s == MatchState.FullTime;
        }

        public void Pause() { if (State == MatchState.Playing || State == MatchState.Restart || State == MatchState.KickOff) { _pausedFrom = State; SetState(MatchState.Paused); UI.ShowPause(); } }
        private MatchState _pausedFrom = MatchState.Playing;
        public void Resume() { if (State == MatchState.Paused) { SetState(_pausedFrom); UI.ShowHud(); } }
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public TeamRuntime Opponent(TeamRuntime t) => t == Home ? Away : Home;
        public void Show(string banner, float seconds) { Banner = banner; BannerUntil = Time.time + seconds; }

        // ------------------------------------------------------------------ per frame
        private void Update()
        {
            if (Home == null) return;
            float dt = Time.deltaTime;
            switch (State)
            {
                case MatchState.KickOff:
                case MatchState.Restart:
                case MatchState.Goal:
                case MatchState.HalfTime:
                    _stateTimer -= dt;
                    if (State != MatchState.Goal && State != MatchState.HalfTime) { Human.Update(dt); Home.AI.Update(dt); Away.AI.Update(dt); }
                    if (_stateTimer <= 0f)
                    {
                        if (State == MatchState.Goal) KickOff(_concededTeam);
                        else if (State == MatchState.HalfTime) StartSecondHalf();
                        else SetState(_afterTimer);
                    }
                    if (Input.GetKeyDown(KeyCode.Escape)) Pause();
                    break;
                case MatchState.Playing:
                    Clock += dt;
                    Human.Update(dt);
                    Home.AI.Update(dt);
                    Away.AI.Update(dt);
                    if (Ball.Owner != null) { _possessionTotal += dt; if (Ball.Owner.Team == Home) _possessionHome += dt; }
                    Rules();
                    if (Clock >= MatchSettings.HalfMinutes * 60f) EndHalf();
                    if (Input.GetKeyDown(KeyCode.Escape)) Pause();
                    break;
                case MatchState.Paused:
                    if (Input.GetKeyDown(KeyCode.Escape)) Resume();
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (Home == null || State == MatchState.Menu || State == MatchState.Paused || State == MatchState.FullTime) return;
            UpdatePossession();
        }

        private TeamRuntime _concededTeam;

        private void UpdatePossession()
        {
            var ball = Ball;
            Vector3 bp = ball.Position;
            // Tackles win the ball from an opponent.
            foreach (var team in new[] { Home, Away })
                foreach (var p in team.Players)
                {
                    if (!p.IsTackling) continue;
                    Vector3 to = bp - p.Position; to.y = 0f;
                    if (to.magnitude < 1.6f && ball.Owner != null && ball.Owner.Team != team)
                    {
                        ball.Owner.Stumble(0.6f);
                        ball.Owner = p;
                        ball.LastTouch = p;
                        ball.KickCooldownUntil = Time.time + 0.15f;
                        Audio.SetExcitement(0.3f);
                    }
                }
            if (Time.time < ball.KickCooldownUntil) return;
            // Release if the owner lost it.
            if (ball.Owner != null)
            {
                Vector3 to = bp - ball.Owner.Position; to.y = 0f;
                if (to.magnitude > 2.2f || ball.Owner.Stumbling) ball.Owner = null;
            }
            if (ball.Owner != null || bp.y > 1.1f) return;
            PlayerAgent best = null; float bestD = 1.05f;
            foreach (var team in new[] { Home, Away })
                foreach (var p in team.Players)
                {
                    if (p.Stumbling) continue;
                    Vector3 to = bp - p.Position; to.y = 0f;
                    float d = to.magnitude;
                    if (d < bestD) { bestD = d; best = p; }
                }
            if (best != null) { ball.Owner = best; ball.LastTouch = best; }
        }

        private void Rules()
        {
            Vector3 b = Ball.Position;
            foreach (int goalSide in new[] { -1, 1 })
            {
                if (PitchGeometry.IsGoal(b.x, b.y, b.z, goalSide))
                {
                    var scorer = goalSide == Home.Side ? Home : Away;
                    OnGoal(scorer);
                    return;
                }
            }
            var kind = PitchGeometry.Out(b.x, b.z);
            if (kind == OutKind.None) return;
            var lastTeam = Ball.LastTouch != null ? Ball.LastTouch.Team : Away;
            var other = Opponent(lastTeam);
            if (kind == OutKind.Sideline)
            {
                Restart(other, new Vector3(PitchGeometry.ClampX(b.x, 1f), 0f, Mathf.Sign(b.z) * (PitchGeometry.HalfWidth - 0.8f)), "KICK-IN");
                return;
            }
            int goalLineSide = b.x > 0f ? 1 : -1;
            bool lastTeamAttacksThisGoal = lastTeam.Side == goalLineSide;
            if (lastTeamAttacksThisGoal)
                Restart(other, new Vector3(goalLineSide * (PitchGeometry.HalfLength - 6f), 0f, Mathf.Sign(b.z) * 6f), "GOAL KICK");
            else
                Restart(other, new Vector3(goalLineSide * (PitchGeometry.HalfLength - 0.8f), 0f, Mathf.Sign(b.z) * (PitchGeometry.HalfWidth - 0.8f)), "CORNER");
        }

        private void Restart(TeamRuntime team, Vector3 pos, string label)
        {
            Ball.PlaceAt(pos);
            RestartTeam = team;
            var taker = team.ClosestTo(pos, false);
            if (taker != null)
            {
                Vector3 back = (team.OwnGoal - pos); back.y = 0f;
                taker.transform.position = pos + back.normalized * 1.3f;
                taker.Face(-back);
            }
            // Give the taker room.
            foreach (var opp in Opponent(team).Players)
            {
                Vector3 away = opp.Position - pos; away.y = 0f;
                if (away.magnitude < 4f) opp.transform.position = pos + (away.sqrMagnitude > 0.01f ? away.normalized : new Vector3(-team.Side, 0f, 0f)) * 4f;
            }
            Show(label, 1.2f);
            Audio.Whistle(1);
            Freeze(MatchState.Restart, 1.0f, MatchState.Playing);
        }

        private void OnGoal(TeamRuntime scorer)
        {
            scorer.Score++;
            _concededTeam = Opponent(scorer);
            Audio.Goal();
            Audio.Whistle(1);
            Show(scorer == Home ? "GOAL!" : "GOAL " + scorer.Def.Short, 3f);
            Freeze(MatchState.Goal, 3f, MatchState.KickOff);
        }

        private void EndHalf()
        {
            Audio.Whistle(Half == 1 ? 2 : 3);
            if (Half == 1)
            {
                Show("HALF TIME", 3f);
                Freeze(MatchState.HalfTime, 3f, MatchState.KickOff);
            }
            else
            {
                Show("FULL TIME", 3f);
                SetState(MatchState.FullTime);
                UI.ShowFullTime();
            }
        }

        private void StartSecondHalf()
        {
            Half = 2; Clock = 0f;
            Home.Side = -Home.Side; Away.Side = -Away.Side;
            KickOff(Home);
        }

        public string ClockText
        {
            get
            {
                float t = Mathf.Min(Clock, MatchSettings.HalfMinutes * 60f) + (Half == 2 ? MatchSettings.HalfMinutes * 60f : 0f);
                int m = (int)(t / 60f), s = (int)(t % 60f);
                return $"{m:00}:{s:00}";
            }
        }
    }
}
