# STRIKER FIVE

Five-a-side arcade football (Unity 2022.3 LTS, C#). A separate Unity project inside this repository: open the
`football` folder in Unity Hub (Add project from disk), then **Striker Five → Create Match Scene** and press Play.
Everything is generated at runtime: pitch, goals with nets, stands with a crowd, floodlights, players, ball, crowd
noise, whistles.

## Controls

| Key | With the ball | Without the ball |
| --- | --- | --- |
| WASD / arrows | Move | Move |
| Space | Sprint | Sprint |
| J | Pass to the teammate you are facing | Tackle |
| K (hold) | Shoot, power grows while held | Tackle |
| L | Lob / long ball | |
| Q | | Switch to another player |
| Esc | Pause | Pause |

The selected player has a glowing ring and a name label. Selection follows the ball automatically.

## Rules

Goals, kick-ins, corners and goal kicks, two halves with a side swap, no fouls or offside. Original fictional teams.

## Code map

```
Assets/Scripts
├── Core/     PitchGeometry + Formation (pure C#, tested), MatchSettings, MatchManager (referee/flow),
│             BroadcastCamera, Materials, Bootstrap
├── Pitch/    PitchBuilder (pitch, markings, goals, boards, stands, floodlights)
├── Ball/     BallController (physics, dribble steering, kicks)
├── Players/  PlayerAgent, PlayerFactory, Team, TeamAI (formation, chasing, decisions, keeper), HumanController
├── UI/       UIFactory, MenuView, HudView, FullTimeView, PauseView
└── Audio/    StadiumAudio (synthesised crowd, kicks, whistles, roar)
```
