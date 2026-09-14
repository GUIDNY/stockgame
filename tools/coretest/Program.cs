using System;
using System.Linq;
using TurboLoop.Track;

public static class Program
{
    private static int _fail;
    private static void Check(bool ok, string what) { if (!ok) { _fail++; Console.WriteLine("  FAIL: " + what); } }

    public static int Main()
    {
        foreach (var layout in TrackLayouts.All)
        {
            var s = new TrackSpline(layout.ControlPoints, 3f);
            Console.WriteLine($"{layout.Name}: {s.Count} samples, {s.Length:0} m, max curvature {s.Curvature.Max(Math.Abs):0.000} rad/m");
            Check(s.Count > 100, "enough samples");
            Check(!s.SelfIntersects(layout.RoadWidth + 6f), $"{layout.Name} does not overlap itself");
            float minRadius = 1f / Math.Max(1e-4f, s.Curvature.Max(Math.Abs));
            Check(minRadius > 12f, $"{layout.Name} tightest corner radius {minRadius:0} m is driveable");
            // Spacing is uniform.
            float minStep = float.MaxValue, maxStep = 0;
            for (int i = 0; i < s.Count; i++) { float d = Vec2.Distance(s.Points[i], s.Points[s.Wrap(i + 1)]); minStep = Math.Min(minStep, d); maxStep = Math.Max(maxStep, d); }
            Check(maxStep - minStep < 1.0f, $"uniform spacing ({minStep:0.00}..{maxStep:0.00})");
            // Nearest index with hint agrees with brute force along the loop.
            for (int i = 0; i < s.Count; i += 7)
            {
                var p = s.Points[i] + s.LeftNormal(i) * 3f;
                Check(s.NearestIndex(p) == i, "brute nearest");
                Check(s.NearestIndex(p, s.Wrap(i - 5)) == i, "hinted nearest");
            }
        }

        // Lap tracker: drive two laps, one reverse stint, and finish.
        var track = new TrackSpline(TrackLayouts.All[0].ControlPoints, 3f);
        var lap = new LapTracker(track.Count, 2);
        lap.Start();
        int idx = 3; int laps = 0;
        for (int step = 0; step < track.Count * 2 + 10; step++)
        {
            idx = track.Wrap(idx + 1);
            if (lap.Update(idx, 0.1f)) laps++;
        }
        Check(laps == 2, $"two laps counted ({laps})");
        Check(lap.Finished && lap.FinishTime > 0, "race finished");
        Check(lap.LapTimes.Count == 2 && Math.Abs(lap.LapTimes[1] - track.Count * 0.1f) < 0.2f, "second lap time equals one full loop");
        Check(!lap.WrongWay, "no false wrong-way");
        var lap2 = new LapTracker(track.Count, 3);
        lap2.Start();
        idx = 10;
        for (int step = 0; step < 30; step++) { idx = track.Wrap(idx - 1); lap2.Update(idx, 0.1f); }
        Check(lap2.WrongWay, "wrong way detected");
        Check(lap2.Lap == 1, "reversing over the line does not count a lap");
        for (int step = 0; step < 40; step++) { idx = track.Wrap(idx + 1); lap2.Update(idx, 0.1f); }
        Check(!lap2.WrongWay, "wrong way clears when driving forward again");
        Check(lap2.Lap == 1, "no lap credited without covering the loop");
        Check(LapTracker.Format(83.456f) == "1:23.456", "time format");
        Console.WriteLine(_fail == 0 ? "ALL CHECKS PASSED" : _fail + " FAILURE(S)");
        return _fail == 0 ? 0 : 1;
    }
}
