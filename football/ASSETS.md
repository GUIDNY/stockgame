# Upgrading STRIKER FIVE with real assets

The game runs with primitive players out of the box. Drop in a rigged character and it switches automatically.

## 1. Animated character (Mixamo, free)

1. Go to https://www.mixamo.com and sign in (free Adobe account).
2. Pick any character (for example "Y Bot" or one of the human characters).
3. Download the character: **Format FBX for Unity, Pose T-pose**. Save as `Character.fbx`.
4. Download these animations for the same character, each as **FBX for Unity, Skin: Without Skin, 30 fps**.
   Name the files exactly like this (the file name is how the game recognises them):

   | Search on Mixamo | Save as |
   | --- | --- |
   | Idle | `Idle.fbx` |
   | Running (or Jog Forward) | `Run.fbx` |
   | Sprint (optional) | `Sprint.fbx` |
   | Soccer Pass / Kick | `Kick.fbx` |
   | Soccer Tackle (or Sliding) | `Tackle.fbx` |

5. In the Unity project (the `football` folder) create the folder `Assets/Characters` and copy all the FBX files there.
6. Wait for the import, then menu **Striker Five → Build Character Rig**. It imports everything as Humanoid,
   builds the animator (idle/run/sprint blend, kick and tackle triggers) and saves `Assets/Resources/PlayerModel.prefab`.
7. Press Play. Every player now uses the animated model, tinted in the team colours.

To go back to primitives, delete `Assets/Resources/PlayerModel.prefab`.

## 2. Stadium (optional)

Any free stadium from the Unity Asset Store works as decoration: import it, drop it in the scene around the pitch
(the pitch is centred at the origin, 64 x 42 m for 5v5, 40 x 26 m for 1v1) and disable the `Stands` object under
`StrikerFive/World/Pitch` at runtime if it clashes.

## 3. Post-processing (optional)

Window → Package Manager → Unity Registry → **Post Processing** → Install. Add a `Post-process Layer` to the
Main Camera and a `Post-process Volume` (global) with Bloom, Ambient Occlusion and Color Grading. This is manual on
purpose: the package is not required for the game to run.
