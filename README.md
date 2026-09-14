# ECHOBOUND

A third-person narrative RPG prototype (Unity 2022.3 LTS, C#) where the story is **not pre-written**. Every New Game, an
AI Director generates a new world: a different conflict, villain, victim, alliances, secrets, relationships, opening
quest and possible endings inside the same physical town. The world adapts to what the player actually does: killing a
quest giver mutates the quest instead of failing it, NPCs remember and gossip, factions retaliate, time and opportunity
matter.

This repository contains the **Phase 1 to Phase 6 vertical slice**: a greybox town, a third-person controller, ten
important NPCs with memory and knowledge, two-mode dialogue (suggested options + free text), a quest system with
mutation, a lightweight world simulation with day/night and schedules, an AI Director with strict JSON validation and
deterministic fallbacks, save/load, and an AI Director debug panel.

## Running it

1. Open the project folder in **Unity 2022.3 LTS** (a regular LTS build such as 2022.3.20f1; the 2022.3.7x "Extended LTS" builds need a paid Industry/Enterprise license; the `Newtonsoft Json` package is pulled from the
   package registry automatically).
2. Either use menu **Echobound → Create Town Scene** and press Play, or simply create an empty scene and press Play.
   `GameBootstrap` assembles the whole game at runtime; there are no prefabs or hand-authored scenes.
3. Press **New Game**. The loading screen says "Creating your world…", then the generated introduction appears.

Without any configuration the game runs on the **offline story generator** (`MockAIProvider`): world generation,
dialogue and director events are deterministic/rule-based but still different every New Game.

### Connecting a real model

Never put an API key in the client. For local development:

- copy `Assets/StreamingAssets/ai_config.example.json` to `ai_config.local.json` (git-ignored) and set
  `"provider": "anthropic"` plus `"api_key"` **or** set the environment variables `ECHOBOUND_AI_PROVIDER=anthropic`
  and `ANTHROPIC_API_KEY=...` before starting Unity;
- for production, set `"proxy_url"` to your backend and leave the key empty; the providers then POST the same request
  body to the proxy, which injects credentials.

`provider` may be `mock`, `anthropic` or `openai`. The debug panel (F1) can switch providers at runtime.

## Controls

| Key | Action |
| --- | --- |
| WASD / Shift | Walk / run |
| Mouse | Camera |
| E | Interact (talk, search, sleep, read) |
| Left mouse | Melee attack |
| Tab / I | Inventory |
| J | Journal |
| R | Relationships |
| M | World knowledge |
| Esc | Pause / leave conversation |
| F5 / F9 | Save / load |
| F1 | AI Director debug panel |

## Verifying without Unity

`tools/simharness` compiles every Unity-independent script and runs a headless smoke test of the narrative simulation
(generation → validation → dialogue → memory → quest mutation → director consequence → resolution → save/load):

```bash
cd tools/simharness && ./run.sh          # needs the .NET 8 SDK
```

`tools/unitystubs` compiles the Unity-facing scripts against a minimal stub of the UnityEngine API to catch typos and
missing members. It is not a substitute for opening the project in Unity.

## What is and is not implemented

See `docs/ARCHITECTURE.md` for the system map and `docs/WORLD_BIBLE.md` for the closed vocabulary the AI may use.
The honest status list is at the end of `docs/ARCHITECTURE.md`.
