# ECHOBOUND - Claude Code configuration

## Project
Unity 2022.3 LTS (C#, built-in render pipeline, legacy Input Manager, uGUI built at runtime, Newtonsoft Json package).
A third-person narrative RPG whose story is generated per New Game by an AI Director and adapts to the player.

## Layout
- `Assets/Scripts/{Core,Player,AI,NPC,Dialogue,Quests,World,Combat,Inventory,UI,SaveSystem}` - see `docs/ARCHITECTURE.md`
- Pure simulation code has no `using UnityEngine`; keep it that way so `tools/simharness` can compile and test it.
- `Assets/Editor/EchoboundMenu.cs` - editor menu (create scene, local AI config).
- `docs/WORLD_BIBLE.md` - the closed vocabulary the AI may use. Add identifiers in `WorldBible.cs` first.

## Commands
```bash
cd tools/simharness && ./run.sh        # compile pure core + headless narrative smoke test (.NET 8 SDK)
cd tools/unitystubs && ./run.sh        # compile Unity-facing scripts against a UnityEngine stub
```

## Rules
- The AI never executes game code: it returns JSON, a validator in `AI/Schemas` checks it, `WorldChangeApplier` applies it.
- Every AI call goes through `AIRequestManager` and must provide a deterministic fallback.
- Never hardcode API keys. Config comes from `Assets/StreamingAssets/ai_config.local.json` (git-ignored) or env vars.
- Quests mutate instead of failing when the world changes underneath them (`QuestManager.MutateForDeath`).
- NPCs only know facts they witnessed or that spread through `KnowledgeSystem` channels.
- Prefer a small working system over an impressive fake one. State explicitly what is not implemented.
