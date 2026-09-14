using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TurboLoop.AI;
using TurboLoop.Audio;
using TurboLoop.Effects;
using TurboLoop.Track;
using TurboLoop.UI;
using TurboLoop.Vehicles;

namespace TurboLoop.Core
{
    public enum RaceState { Menu, Countdown, Racing, Paused, Finished }

    /// <summary>Composition root and race director: builds the world, spawns cars, runs countdown, ranking and results.</summary>
    public class RaceManager : MonoBehaviour
    {
        public static RaceManager Instance { get; private set; }

        public RaceState State { get; private set; } = RaceState.Menu;
        public TrackBuilder Track { get; private set; }
        public readonly List<CarController> Cars = new List<CarController>();
        public CarController Player { get; private set; }
        public ChaseCamera Cam { get; private set; }
        public UIManager UI { get; private set; }
        public Light Sun { get; private set; }
        public float Countdown { get; private set; }
        public float RaceTime { get; private set; }
        public int PlayerFinishRank { get; private set; }
        public string LastEvent { get; private set; } = "";
        public float LastEventTime { get; private set; } = -10f;

        private Transform _world, _carsRoot;
        private int _builtTrackIndex = -1;
        private readonly List<CarController> _standings = new List<CarController>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 1;
            _world = new GameObject("World").transform;
            _world.SetParent(transform, false);
            _carsRoot = new GameObject("Cars").transform;
            _carsRoot.SetParent(transform, false);

            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(_world, false);
            Sun = sunGo.AddComponent<Light>();
            Sun.type = LightType.Directional;

            Cam = ChaseCamera.Create();
            UI = UIManager.Create(this);
            BuildTrack(RaceSettings.TrackIndex);
            ShowMenu();
        }

        // ------------------------------------------------------------------ world
        public void BuildTrack(int index)
        {
            index = Mathf.Clamp(index, 0, TrackLayouts.All.Length - 1);
            if (_builtTrackIndex == index && Track != null) return;
            if (Track != null) Destroy(Track.gameObject);
            var layout = TrackLayouts.All[index];
            Track = TrackBuilder.Build(layout, _world);
            Materials.SetupSky(Sun, layout.Theme);
            _builtTrackIndex = index;
            Cam.OrbitCenter = Track.Center;
        }

        public void PreviewTrack(int index)
        {
            RaceSettings.TrackIndex = index;
            BuildTrack(index);
        }

        private void ClearCars()
        {
            foreach (var c in Cars) if (c != null) Destroy(c.gameObject);
            Cars.Clear();
            Player = null;
        }

        // ------------------------------------------------------------------ flow
        public void ShowMenu()
        {
            SetState(RaceState.Menu);
            ClearCars();
            Cam.MenuMode = true;
            Cam.OrbitCenter = Track.Center;
            UI.ShowMenu();
        }

        public void StartRace()
        {
            BuildTrack(RaceSettings.TrackIndex);
            ClearCars();
            int total = 1 + Mathf.Clamp(RaceSettings.Opponents, 0, 7);
            var rng = new System.Random();
            var colorOrder = Enumerable.Range(0, RaceSettings.Palette.Length).Where(i => i != RaceSettings.PlayerColorIndex).OrderBy(_ => rng.Next()).ToList();
            var names = RaceSettings.AiNames.OrderBy(_ => rng.Next()).ToList();
            // Player starts at the back so overtaking is the game.
            for (int slot = 0; slot < total; slot++)
            {
                bool isPlayer = slot == total - 1;
                Track.GridSlot(slot, out var pos, out var rot);
                var color = isPlayer ? RaceSettings.Palette[RaceSettings.PlayerColorIndex] : RaceSettings.Palette[colorOrder[slot % colorOrder.Count]];
                var car = CarFactory.Create(isPlayer ? "You" : names[slot % names.Count], color, pos, rot, isPlayer, _carsRoot);
                car.Lap = new LapTracker(Track.Spline.Count, RaceSettings.Laps);
                car.TrackIndex = Track.NearestIndex(pos, -1);
                if (isPlayer)
                {
                    car.Input = car.gameObject.AddComponent<PlayerInput>();
                    Player = car;
                }
                else
                {
                    var ai = car.gameObject.AddComponent<AIDriver>();
                    float skill = 0.86f + (float)rng.NextDouble() * 0.13f;
                    ai.Init(Track.Spline, car, skill, (float)(rng.NextDouble() * 5f - 2.5f));
                    car.Input = ai;
                    car.MaxSpeed *= 0.97f;
                }
                EngineAudio.Attach(car, isPlayer);
                TireSmoke.Attach(car);
                Cars.Add(car);
            }
            Cam.SnapTo(Player);
            Countdown = 3.6f;
            RaceTime = 0f;
            PlayerFinishRank = 0;
            SetState(RaceState.Countdown);
            UI.ShowHud();
        }

        public void RestartRace() => StartRace();

        public void Pause()
        {
            if (State != RaceState.Racing && State != RaceState.Countdown) return;
            SetState(RaceState.Paused);
            UI.ShowPause();
        }

        public void Resume()
        {
            if (State != RaceState.Paused) return;
            SetState(Countdown > 0f ? RaceState.Countdown : RaceState.Racing);
            UI.ShowHud();
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetState(RaceState s)
        {
            State = s;
            Time.timeScale = s == RaceState.Paused ? 0f : 1f;
            AudioListener.pause = s == RaceState.Paused;
            bool driving = s == RaceState.Racing || s == RaceState.Finished;
            foreach (var c in Cars) c.ControlsEnabled = driving && (!c.IsPlayer || s == RaceState.Racing);
            Cursor.visible = s != RaceState.Racing && s != RaceState.Countdown;
            Cursor.lockState = CursorLockMode.None;
        }

        // ------------------------------------------------------------------ per frame
        private void Update()
        {
            switch (State)
            {
                case RaceState.Countdown:
                    Countdown -= Time.deltaTime;
                    if (Countdown <= 0f)
                    {
                        foreach (var c in Cars) c.Lap.Start();
                        SetState(RaceState.Racing);
                        Announce("GO!");
                    }
                    if (Input.GetKeyDown(KeyCode.Escape)) Pause();
                    break;
                case RaceState.Racing:
                case RaceState.Finished:
                    RaceTime += Time.deltaTime;
                    Simulate(Time.deltaTime);
                    if (State == RaceState.Racing && Input.GetKeyDown(KeyCode.Escape)) Pause();
                    if (State == RaceState.Racing && Input.GetKeyDown(KeyCode.R)) ResetPlayer();
                    break;
                case RaceState.Paused:
                    if (Input.GetKeyDown(KeyCode.Escape)) Resume();
                    break;
            }
        }

        private void Simulate(float dt)
        {
            foreach (var car in Cars)
            {
                if (car == null) continue;
                car.TrackIndex = Track.NearestIndex(car.transform.position, car.TrackIndex);
                bool lapDone = car.Lap.Update(car.TrackIndex, dt);
                if (lapDone && car.IsPlayer)
                {
                    if (car.Lap.Finished) OnPlayerFinished();
                    else if (car.Lap.Lap == car.Lap.TotalLaps) Announce("FINAL LAP");
                    else Announce($"LAP {car.Lap.Lap}");
                }
                // Fell off the world or flipped: put it back on the road.
                if (car.transform.position.y < -5f) car.ResetTo(Track.P(car.TrackIndex) + Vector3.up * 0.3f, Track.HeadingAt(car.TrackIndex));
            }
            _standings.Clear();
            _standings.AddRange(Cars.Where(c => c != null));
            _standings.Sort((a, b) =>
            {
                if (a.Lap.Finished && b.Lap.Finished) return a.Lap.FinishTime.CompareTo(b.Lap.FinishTime);
                if (a.Lap.Finished != b.Lap.Finished) return a.Lap.Finished ? -1 : 1;
                return b.Lap.RaceProgress.CompareTo(a.Lap.RaceProgress);
            });
            for (int i = 0; i < _standings.Count; i++) _standings[i].Rank = i + 1;
        }

        public IReadOnlyList<CarController> Standings => _standings;

        private void OnPlayerFinished()
        {
            PlayerFinishRank = Cars.Count(c => c.Lap.Finished);
            SetState(RaceState.Finished);
            Player.ControlsEnabled = false;
            Announce("FINISH");
            UI.ShowResults();
        }

        private void ResetPlayer()
        {
            if (Player == null) return;
            int i = Player.TrackIndex;
            Player.ResetTo(Track.P(i) + Vector3.up * 0.3f, Track.HeadingAt(i));
        }

        private void Announce(string text)
        {
            LastEvent = text;
            LastEventTime = Time.time;
        }
    }
}
