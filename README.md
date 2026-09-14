# TURBO LOOP

Arcade circuit racing for PC, built in Unity 2022.3 LTS with C#. Everything you see is generated at runtime from
primitives and procedural textures: three circuits, cars, grandstand, scenery, skybox, even the engine sound.
No art assets, no prefabs, no hand-authored scenes.

## Play

1. Open the folder in Unity 2022.3 LTS (a regular LTS build such as 2022.3.20f1).
2. Menu **Turbo Loop → Create Race Scene**, then press Play. (Any empty scene also works: the game bootstraps itself.)
3. Pick a track, laps, opponents and a colour. **START RACE**.

| Key | Action |
| --- | --- |
| W / ↑ | Accelerate |
| S / ↓ | Brake, reverse |
| A D / ← → | Steer |
| Space | Handbrake (drift) |
| R | Reset onto the track |
| Esc | Pause |

## Code map

```
Assets/Scripts
├── Track/     Vec2, TrackSpline (closed Catmull-Rom, arc-length resampled), TrackLayouts, LapTracker  [pure C#]
│              TrackBuilder (road/kerb/wall/start meshes), Scenery                                     [Unity]
├── Vehicles/  CarController (arcade Rigidbody physics), CarFactory, PlayerInput, ICarInput
├── AI/        AIDriver (lookahead steering, curvature braking, avoidance, unstuck)
├── Core/      RaceManager (flow, ranking, results), ChaseCamera, Materials (procedural textures, sky), RaceSettings
├── UI/        UIFactory, MenuView, HudView, ResultsView, PauseView
├── Audio/     EngineAudio (synthesised engine + tyre squeal)
└── Effects/   TireSmoke
```

`tools/coretest` compiles the pure track core and validates every layout (no self-overlap, driveable radii) and the
lap tracker. `tools/unitystubs` type-checks the Unity scripts against a stub API. Neither replaces opening Unity.

## Also in this repository

`football/` is a second, independent Unity project: **STRIKER FIVE**, five-a-side arcade football. See `football/README.md`.
