# TURBO LOOP - Claude Code configuration

Unity 2022.3 LTS, C#, built-in render pipeline, legacy Input Manager, runtime-built uGUI. Arcade racing game.
Everything is generated at runtime from primitives; there are no prefabs, scenes or art assets to maintain.

## Layout
- `Assets/Scripts/{Track,Vehicles,AI,Core,UI,Audio,Effects}` - see README.md for the map.
- `Assets/Scripts/Track/{Vec2,TrackSpline,TrackLayouts,LapTracker}.cs` are pure C# (no UnityEngine): keep them that
  way so `tools/coretest` can validate layouts and lap logic.
- `Assets/Editor/TurboLoopMenu.cs` creates the bootstrap scene.

## Commands
```bash
cd tools/coretest && ./run.sh      # validate track layouts + lap tracker (.NET 8 SDK)
cd tools/unitystubs && ./run.sh    # type-check Unity scripts against a stub API
```

## Rules
- New tracks go in `TrackLayouts.cs`; run coretest, it rejects self-overlapping or undriveable layouts.
- Car handling lives only in `CarController.FixedUpdate`; AI reads the same `CarController` values as the HUD.
- Keep it looking good: bright daylight, saturated colours, no debug primitives left in the scene.
