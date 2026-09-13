# ECHOBOUND architecture

## One paragraph

Static content (places, NPC slots and roles, items, enemy type, quest actions, mechanics) is the **World Bible**.
Dynamic content (who is the villain, who is missing, who hates whom, what is hidden where) is a **World Seed** generated
per New Game and validated against the Bible. The seed becomes a **World State** that a set of deterministic systems
operate on every game hour (schedules, information spread, quest tracking). The **AI World Director** only calls the
LLM for meaningful narrative moments, always receives structured JSON, validates and auto-corrects it, and applies it
through a single **WorldChangeApplier**. If the LLM is slow, wrong or absent, deterministic fallbacks keep the world
reacting. Unity objects are thin views over this state.

## Layers

```
Assets/Scripts
├── Core/        GameLog, GameEvents (event bus), WorldClock, SeededRandom  [pure]
│                GameManager (composition root), GameBootstrap, GameClock  [Unity]
├── World/       WorldBible, WorldSeed, WorldState, Facts, KnowledgeSystem, FactionSystem,
│                DirectorEvent/WorldChange, WorldChangeApplier, PlayerAction  [pure]
│                GreyboxTownBuilder, LocationZone, Interactables  [Unity]
├── NPC/         NpcState, NPCMemory (short/long term + relationship axes)  [pure]
│                NPCController  [Unity]
├── AI/          IAIProvider, AIRequestManager, AIConfig, AIWorldDirector, Prompts, Payloads,
│                NarrativeGenerator (deterministic worlds), MockDialogue, FallbackEventFactory
│   ├── Providers/  AnthropicProvider, OpenAIProvider, MockAIProvider
│   └── Schemas/    validators for WorldSeed, DirectorEvent, DialogueResponse, Quest, QuestMutation
├── Dialogue/    DialogueManager (intents, mechanics, options), DialogueResponse  [pure]
├── Quests/      Quest model, QuestManager (tracking + mutation)  [pure]
├── Inventory/   Item, InventorySystem  [pure]
├── Combat/      Health, EnemyAI, EnemySpawner  [Unity]
├── Player/      ThirdPersonController, ThirdPersonCamera, PlayerInteractor, PlayerCombat  [Unity]
├── UI/          UIFactory (runtime uGUI), HUD, Dialogue, menus, PlayerMenu, Decision, DebugPanel  [Unity]
└── SaveSystem/  SaveManager (JSON of the whole WorldState)  [pure]
```

"Pure" files have no UnityEngine dependency and are compiled and exercised by `tools/simharness`.

## Data flow

1. **New Game** → `AIWorldDirector.GenerateWorldAsync` → `AIRequestManager` → provider → `WorldSeedValidator`
   → `WorldState.FromSeed` (facts created from hidden truths and relationships, NPC states built, opening quest added).
2. **Player acts** (talk/threaten/search/attack…) → `GameEvents.PlayerActed(PlayerAction)`
   → `KnowledgeSystem.RecordPlayerAction` creates a fact and memories for every witness at that location
   → `QuestManager` checks objectives → `AIWorldDirector` requests a consequence for important actions.
3. **Every game hour** → NPC schedules move NPCs → `KnowledgeSystem.HourlySpread` moves facts through faction
   communication, guard reports, co-location gossip and tavern rumors → deadline checks → tension-scaled ambient event.
4. **NPC dies** → `WorldChangeApplier.KillNpc` → `QuestManager` mutates affected quests (successor takes over, evidence
   search replaces conversation, "who killed X" lead added) → director requests a consequence (retaliation, arrest
   attempt, rumor) → pending offers from that NPC are dropped.
5. **Dialogue** → `DialogueManager` builds a `DialoguePayload` (persona, memory digest, knowable facts, relationship,
   world summary, intent) → LLM or `MockDialogue` → `DialogueResponseValidator` (only fact ids the NPC actually knows
   may be revealed) → mechanics applied (relationship axes, memory, discovered info, hostility, items).
6. **Save** → the whole `WorldState` (seed, NPC memory, facts, quests, reputation, placed items, time, player) as JSON.
   **Load** rebuilds systems and views from it; nothing is regenerated.

## AI cost control

- The LLM is called for: world generation (once), dialogue turns, consequences of important actions, NPC deaths,
  rare ambient events, and text for mutated quests. Nothing per frame.
- Prompts carry: cached stable system prompts, a compact `WorldSummarizer` state summary, a memory digest of at most
  six lines, and the list of facts the NPC can know. Never the full history.
- `AIRequestManager` serializes requests, rate-limits, retries with validation feedback, times out, logs prompts and
  responses, counts tokens and estimates cost. Validation failure → corrected → re-asked once with the errors → fallback.
- `IAIProvider` isolates the backend. `AIConfig` reads a git-ignored local file or environment variables; a proxy URL
  is supported so production builds never hold a key.

## Honest status (what is not done)

- Not opened in the Unity editor in this environment: the Unity-facing scripts were compiled only against a stub of
  the UnityEngine API (`tools/unitystubs`). Expect small fixes on first import (layout tweaks, API version details).
- Placeholder everything: primitives, no animation, no audio, TextMesh labels, uGUI built in code.
- NPC movement is straight-line steering with a raycast, not NavMesh. NPCs can get stuck on props occasionally.
- Combat is one melee swing and one enemy type. Player death is a soft fail (wake up later, lose coins).
- The live Anthropic/OpenAI providers were written against the documented HTTP contracts but were not executed against
  a real key here. The mock path is fully exercised by the harness.
- Free-text dialogue on the mock provider is keyword-classified; nuance requires a live model.
- Endings are recorded and announced but there is no ending cinematic; the world continues as a sandbox.
- Not implemented: quest generation by the AI mid-game beyond QUEST_OFFER events (schema and validator exist, no
  trigger yet), lockpicking/doors, trading UI (prices exist in `FactionSystem.PriceMultiplier` but no shop screen),
  guard arrest sequence beyond hostility/thugs.
