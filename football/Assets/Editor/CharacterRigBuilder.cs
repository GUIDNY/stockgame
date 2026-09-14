using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace StrikerFive.EditorTools
{
    /// <summary>
    /// Builds the animated player from whatever rigged FBX files sit in Assets/Characters:
    /// the bundled Quaternius library (one FBX, many takes) or Mixamo files (one clip per FBX).
    /// Output: Assets/Characters/PlayerAnimator.controller and Assets/Resources/PlayerModel.prefab.
    /// </summary>
    public static class CharacterRigBuilder
    {
        private const string CharactersDir = "Assets/Characters";
        private const string ControllerPath = "Assets/Characters/PlayerAnimator.controller";
        private const string PrefabPath = "Assets/Resources/PlayerModel.prefab";

        [MenuItem("Striker Five/Build Character Rig")]
        public static void Build()
        {
            if (!Directory.Exists(CharactersDir)) { Debug.LogError("Striker Five: " + CharactersDir + " is missing."); return; }
            var models = AssetDatabase.FindAssets("t:Model", new[] { CharactersDir }).Select(AssetDatabase.GUIDToAssetPath).Distinct().ToList();
            if (models.Count == 0) { Debug.LogError("Striker Five: no FBX files found in " + CharactersDir); return; }

            // 1. Import as Humanoid, loop the loops, keep animation in place.
            foreach (var path in models)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;
                importer.animationType = ModelImporterAnimationType.Human;
                importer.importAnimation = true;
                var clips = importer.defaultClipAnimations;
                foreach (var c in clips)
                {
                    string n = c.name.ToLowerInvariant();
                    c.loopTime = n.Contains("loop") || n.Contains("idle") || n.Contains("run") || n.Contains("walk") || n.Contains("jog") || n.Contains("sprint") || n.Contains("dance");
                    c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
                    c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
                }
                if (clips.Length > 0) importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }

            // 2. Collect every clip across all files, look them up by keyword (first keyword that matches wins).
            var allClips = models.SelectMany(p => AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>()).Where(c => !c.name.StartsWith("__preview")).ToList();
            AnimationClip Find(params string[] keys)
            {
                foreach (var k in keys)
                {
                    var hit = allClips.FirstOrDefault(c => c.name.ToLowerInvariant().Contains(k));
                    if (hit != null) return hit;
                }
                return null;
            }
            var idle = Find("idle_loop", "idle");
            var run = Find("jog_fwd", "run", "jog");
            var sprint = Find("sprint") ?? run;
            var kick = Find("kick", "shoot");
            var tackle = Find("tackle", "slide", "roll");
            var celebrate = Find("dance", "celebrat", "victory");
            var stumble = Find("hit_chest", "hit", "stumble");
            if (idle == null || run == null) { Debug.LogError("Striker Five: need at least an idle and a run/jog clip. Clips found: " + string.Join(", ", allClips.Select(c => c.name))); return; }

            // 3. Animator controller.
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null) AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("HasBall", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Celebrate", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Kick", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Tackle", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Stumble", AnimatorControllerParameterType.Trigger);
            var layers = controller.layers; layers[0].iKPass = true; controller.layers = layers;   // foot IK for procedural kicks
            var sm = controller.layers[0].stateMachine;
            var locomotion = controller.CreateBlendTreeInController("Locomotion", out var tree);
            tree.blendParameter = "Speed";
            tree.AddChild(idle, 0f);
            tree.AddChild(run, 0.55f);
            tree.AddChild(sprint, 1f);
            sm.defaultState = locomotion;
            void OneShot(string name, AnimationClip clip, string trigger, float exit)
            {
                if (clip == null) return;
                var st = sm.AddState(name); st.motion = clip;
                var t = sm.AddAnyStateTransition(st); t.AddCondition(AnimatorConditionMode.If, 0, trigger); t.duration = 0.06f; t.canTransitionToSelf = false;
                var back = st.AddTransition(locomotion); back.hasExitTime = true; back.exitTime = exit; back.duration = 0.15f;
            }
            OneShot("Kick", kick, "Kick", 0.6f);
            OneShot("Tackle", tackle, "Tackle", 0.85f);
            OneShot("Stumble", stumble, "Stumble", 0.8f);
            if (celebrate != null)
            {
                var st = sm.AddState("Celebrate"); st.motion = celebrate;
                var to = locomotion.AddTransition(st); to.AddCondition(AnimatorConditionMode.If, 0, "Celebrate"); to.duration = 0.2f;
                var back = st.AddTransition(locomotion); back.AddCondition(AnimatorConditionMode.IfNot, 0, "Celebrate"); back.duration = 0.2f;
            }

            // 4. Prefab from the FBX that has a skinned mesh, normalised to 1.8 m tall.
            string modelPath = models.FirstOrDefault(p => AssetDatabase.LoadAssetAtPath<GameObject>(p)?.GetComponentInChildren<SkinnedMeshRenderer>() != null);
            if (modelPath == null) { Debug.LogError("Striker Five: no FBX with a skinned mesh found in " + CharactersDir); return; }
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "PlayerModel";
            var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var smr = instance.GetComponentInChildren<SkinnedMeshRenderer>();
            float height = Mathf.Max(0.01f, smr.bounds.size.y);
            instance.transform.localScale = Vector3.one * (1.8f / height);
            instance.transform.localPosition = Vector3.zero;
            instance.AddComponent<Players.PlayerAnimationHooks>();
            Directory.CreateDirectory("Assets/Resources");
            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Striker Five: rig built from {Path.GetFileName(modelPath)}. idle={idle.name} run={run.name} sprint={sprint.name} kick={(kick != null ? kick.name : "procedural IK")} tackle={(tackle != null ? tackle.name : "none")} celebrate={(celebrate != null ? celebrate.name : "none")} stumble={(stumble != null ? stumble.name : "none")}. Press Play.");
        }
    }
}
