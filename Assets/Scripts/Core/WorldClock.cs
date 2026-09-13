using System;

namespace Echobound.Core
{
    /// <summary>
    /// Pure simulation clock. Tracks elapsed game minutes and exposes day/hour.
    /// The Unity GameClock component advances it from real time; important actions advance it directly.
    /// </summary>
    public class WorldClock
    {
        public const int MinutesPerDay = 24 * 60;
        public const int StartMinuteOfDay = 18 * 60; // the player arrives in the evening

        public int ElapsedMinutes { get; private set; }
        public int TotalMinutes => StartMinuteOfDay + ElapsedMinutes;
        public int Day => TotalMinutes / MinutesPerDay + 1;
        public int Hour => (TotalMinutes % MinutesPerDay) / 60;
        public int Minute => TotalMinutes % 60;
        public bool IsNight => Hour >= 21 || Hour < 6;
        public float DayFraction => (TotalMinutes % MinutesPerDay) / (float)MinutesPerDay;

        public void Reset(int elapsedMinutes = 0)
        {
            ElapsedMinutes = Math.Max(0, elapsedMinutes);
        }

        /// <summary>Advance by a number of minutes, raising HourStarted for every hour boundary crossed.</summary>
        public void Advance(int minutes)
        {
            if (minutes <= 0) return;
            int beforeHour = TotalMinutes / 60;
            ElapsedMinutes += minutes;
            int afterHour = TotalMinutes / 60;
            GameEvents.RaiseTimeAdvanced(ElapsedMinutes);
            for (int h = beforeHour + 1; h <= afterHour; h++)
            {
                GameEvents.RaiseHourStarted(h % 24);
            }
        }

        public string Format() => $"Day {Day}  {Hour:00}:{Minute:00}";
    }
}
