using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace StrikerFive.EditorTools
{
    /// <summary>
    /// Turns a Mixamo character plus animation FBX files into the rigged player the game uses automatically.
    /// Put the files in Assets/Characters (see ASSETS.md), run Striker Five → Build Character Rig, press Play.
    /// </summary>
    public static class CharacterRigBuilder
    {
        private const string CharactersDir = "Assets/Characters";
        private const string ControllerPath = "Assets/Characters/PlayerAnimator.controller";
        private const string PrefabPath = "Assets/Resources/PlayerModel.prefab";

        [MenuItem("Striker Five/Build Character Rig")]
        public static void Build()
        {
            if (!Directory.Exists(CharactersDir)) { Debug.LogError("Striker Five: create " + CharactersDir + " and add the Mixamo files first (see ASSETS.md)."); return; }
            var fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { CharactersDir });
            var models = fbxGuids.Select(AssetDatabase.GUIDToAssetPath).ToList();
            if (models.Count == 0) { Debug.LogError("Striker Five: no FBX files found in " + CharactersDir); return; }

            // 1. Import every FBX as Humanoid so clips retarget onto any character.
            foreach (var path in models)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;
                bool changed = false;
                if (importer.animationType != ModelImporterAnimationType.Human) { importer.animationType = ModelImporterAnimationType.Human; changed = true; }
                var clips = importer.defaultClipAnimations;
                if (clips.Length > 0)
                {
                    string lower = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                    bool loop = lower.Contains("idle") || lower.Contains("run") || lower.Contains("walk") || lower.Contains("sprint") || lower.Contains("jog");
                    foreach (var c in clips) { c.loopTime = loop; c.lockRootRotation = true; c.lockRootHeightY = true; c.keepOriginalPositionXZ = true; c.lockRootPositionXZ = true; }
                    importer.clipAnimations = clips;
                    changed = true;
                }
                if (changed) importer.SaveAndReimport();
            }

            // 2. Find the clips by file name.
            AnimationClip Find(params string[] keys)
            {
                foreach (var path in models)
                {
                    string lower = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                    if (!keys.Any(k => lower.Contains(k))) continue;
                    var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
                    if (clip != null) return clip;
                }
                return null;
            }
            var idle = Find("idle"); var run = Find("run", "jog"); var sprint = Find("sprint") ?? run; var kick = Find("kick", "shoot"); var tackle = Find("tackle", "slide");
            if (idle == null || run == null) { Debug.LogError("Striker Five: need at least an Idle and a Run animation FBX in " + CharactersDir); return; }

            // 3. Animator controller: Speed blend tree, Kick and Tackle triggers.
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("HasBall", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Kick", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Tackle", AnimatorControllerParameterType.Trigger);
            var sm = controller.layers[0].stateMachine;
            var locomotion = controller.CreateBlendTreeInController("Locomotion", out var tree);
            tree.blendParameter = "Speed";
            tree.AddChild(idle, 0f);
            tree.AddChild(run, 0.55f);
            tree.AddChild(sprint, 1f);
            sm.defaultState = locomotion;
            if (kick != null)
            {
                var kickState = sm.AddState("Kick"); kickState.motion = kick;
                var t = sm.AddAnyStateTransition(kickState); t.AddCondition(AnimatorConditionMode.If, 0, "Kick"); t.duration = 0.05f; t.canTransitionToSelf = false;
                var back = kickState.AddTransition(locomotion); back.hasExitTime = true; back.exitTime = 0.6f; back.duration = 0.15f;
            }
            if (tackle != null)
            {
                var tackleState = sm.AddState("Tackle"); tackleState.motion = tackle;
                var t = sm.AddAnyStateTransition(tackleState); t.AddCondition(AnimatorConditionMode.If, 0, "Tackle"); t.duration = 0.05f; t.canTransitionToSelf = false;
                var back = tackleState.AddTransition(locomotion); back.hasExitTime = true; back.exitTime = 0.8f; back.duration = 0.15f;
            }

            // 4. Prefab from the character model (the FBX with a mesh).
            string modelPath = models.FirstOrDefault(p => AssetDatabase.LoadAssetAtPath<GameObject>(p)?.GetComponentInChildren<SkinnedMeshRenderer>() != null);
            if (modelPath == null) { Debug.LogError("Striker Five: no FBX with a skinned mesh (the character) found in " + CharactersDir); return; }
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = "PlayerModel";
            var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            // Mixamo characters are 100x too big when imported at default scale; normalise to ~1.8 m tall.
            var bounds = instance.GetComponentInChildren<SkinnedMeshRenderer>().bounds;
            float height = Mathf.Max(0.01f, bounds.size.y);
            instance.transform.localScale = Vector3.one * (1.8f / height);
            Directory.CreateDirectory("Assets/Resources");
            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Striker Five: rig built. Clips: idle={idle.name} run={run.name} sprint={sprint.name} kick={(kick != null ? kick.name : "none")} tackle={(tackle != null ? tackle.name : "none")}. Press Play.");
        }
    }
}
