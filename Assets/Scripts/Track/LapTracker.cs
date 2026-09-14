using System;
using System.Collections.Generic;

namespace TurboLoop.Track
{
    /// <summary>
    /// Tracks one car's progress around the loop using nearest centreline indices: lap counting, lap times,
    /// wrong-way detection and a monotonic race progress value used for ranking. Pure logic, unit tested.
    /// </summary>
    public class LapTracker
    {
        private readonly int _count;
        public int TotalLaps { get; }
        public int Lap { get; private set; } = 1;
        public int Index { get; private set; }
        public bool Finished { get; private set; }
        public float CurrentLapTime { get; private set; }
        public float TotalTime { get; private set; }
        public float BestLap { get; private set; } = float.MaxValue;
        public readonly List<float> LapTimes = new List<float>();
        public bool WrongWay { get; private set; }
        public float FinishTime { get; private set; } = -1f;

        private float _visitedFraction;      // how much of the lap has been covered (0..1)
        private float _wrongWayTimer;
        private bool _started;

        public LapTracker(int waypointCount, int totalLaps)
        {
            _count = Math.Max(4, waypointCount);
            TotalLaps = Math.Max(1, totalLaps);
        }

        /// <summary>Progress within the current lap, 0..1.</summary>
        public float LapProgress => Index / (float)_count;
        /// <summary>Monotonic race progress for ranking: completed laps + fraction of the current lap.</summary>
        public float RaceProgress => Finished ? TotalLaps + 1f : (Lap - 1) + LapProgress;

        public void Start() { _started = true; }

        /// <summary>Call every frame with the car's nearest centreline index. Returns true when a lap was just completed.</summary>
        public bool Update(int nearestIndex, float deltaTime)
        {
            if (!_started || Finished) return false;
            CurrentLapTime += deltaTime;
            TotalTime += deltaTime;
            int prev = Index;
            Index = ((nearestIndex % _count) + _count) % _count;
            int delta = Index - prev;
            if (delta > _count / 2) delta -= _count;         // wrapped backwards over the line
            else if (delta < -_count / 2) delta += _count;   // wrapped forwards over the line

            bool lapCompleted = false;
            if (delta > 0)
            {
                _visitedFraction = Math.Min(1f, _visitedFraction + delta / (float)_count);
                _wrongWayTimer = Math.Max(0f, _wrongWayTimer - deltaTime * 2f);
                if (prev > Index && _visitedFraction >= 0.8f)
                {
                    lapCompleted = true;
                    LapTimes.Add(CurrentLapTime);
                    BestLap = Math.Min(BestLap, CurrentLapTime);
                    CurrentLapTime = 0f;
                    _visitedFraction = 0f;
                    Lap++;
                    if (Lap > TotalLaps) { Finished = true; FinishTime = TotalTime; }
                }
            }
            else if (delta < 0)
            {
                _visitedFraction = Math.Max(0f, _visitedFraction + delta / (float)_count);
                _wrongWayTimer += deltaTime;
            }
            WrongWay = _wrongWayTimer > 1.2f;
            return lapCompleted;
        }

        public static string Format(float seconds)
        {
            if (seconds < 0f || seconds == float.MaxValue) return "--:--.---";
            int m = (int)(seconds / 60f);
            float s = seconds - m * 60f;
            return $"{m:0}:{s:00.000}";
        }
    }
}
