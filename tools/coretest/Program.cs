using System;
using System.Linq;
using TurboLoop.Track;
using StrikerFive.Core;

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
        // Football rules geometry.
        Check(PitchGeometry.IsGoal(32.5f, 1f, 0f, 1), "ball over the +x line inside posts is a goal");
        Check(!PitchGeometry.IsGoal(32.5f, 1f, 4f, 1), "wide of the post is not a goal");
        Check(!PitchGeometry.IsGoal(32.5f, 2.6f, 0f, 1), "over the bar is not a goal");
        Check(!PitchGeometry.IsGoal(31.9f, 1f, 0f, 1), "on the line is not a goal");
        Check(PitchGeometry.IsGoal(-32.5f, 0.5f, -1f, -1), "goal at -x");
        Check(PitchGeometry.Out(0f, 21.5f) == OutKind.Sideline, "sideline out");
        Check(PitchGeometry.Out(32.6f, 5f) == OutKind.GoalLine, "goal line out");
        Check(PitchGeometry.Out(10f, 10f) == OutKind.None, "in play");
        Check(PitchGeometry.InBox(28f, 3f, 1) && !PitchGeometry.InBox(15f, 3f, 1) && !PitchGeometry.InBox(28f, 15f, 1), "penalty box");
        // Formation slides with the ball and mirrors by side.
        var (gx, gz) = Formation.Home(Role.Goalkeeper, 1, 0f, 0f, false);
        Check(gx < -25f && Math.Abs(gz) < 0.1f, "keeper starts near own goal");
        var (fx, _) = Formation.Home(Role.Forward, 1, 20f, 0f, true);
        var (fx2, _) = Formation.Home(Role.Forward, 1, -20f, 0f, false);
        Check(fx > fx2 + 10f, "forward pushes up with the ball");
        var (mx, mz) = Formation.Home(Role.Midfielder, -1, 10f, 5f, false);
        var (mxp, mzp) = Formation.Home(Role.Midfielder, 1, -10f, 5f, false);
        Check(Math.Abs(mx + mxp) < 0.01f && Math.Abs(mz - mzp) < 0.01f, "formation mirrors across sides");
        foreach (var role in Formation.Roles)
            for (float bx = -32; bx <= 32; bx += 8)
            {
                var (hx, hz) = Formation.Home(role, 1, bx, 15f, true);
                Check(Math.Abs(hx) <= 31f && Math.Abs(hz) <= 19f, "home position stays on the pitch");
            }
        // Small formats: geometry shrinks, formations stay inside, no keeper role.
        PitchGeometry.Configure(1);
        Check(PitchGeometry.HalfLength < 25f && PitchGeometry.GoalHalfWidth < 3f, "1v1 uses a small pitch and goals");
        Check(Formation.RolesFor(1).Length == 1 && Formation.RolesFor(2).Length == 2 && Formation.RolesFor(5).Length == 5, "roles per format");
        Check(Array.IndexOf(Formation.RolesFor(1), Role.Goalkeeper) < 0, "1v1 has no keeper");
        foreach (var role in Formation.RolesFor(2))
            for (float bx = -20; bx <= 20; bx += 5)
            {
                var (hx, hz) = Formation.Home(role, -1, bx, -8f, false);
                Check(Math.Abs(hx) < PitchGeometry.HalfLength && Math.Abs(hz) < PitchGeometry.HalfWidth, "small-pitch home position on the pitch");
            }
        Check(PitchGeometry.IsGoal(20.6f, 1f, 0f, 1) && !PitchGeometry.IsGoal(20.6f, 1f, 3f, 1), "1v1 goal detection uses the small goal");
        PitchGeometry.Configure(5);
        Check(Math.Abs(PitchGeometry.HalfLength - 32f) < 0.01f, "configure back to 5v5");
        Console.WriteLine(_fail == 0 ? "ALL CHECKS PASSED" : _fail + " FAILURE(S)");
        return _fail == 0 ? 0 : 1;
    }
}
