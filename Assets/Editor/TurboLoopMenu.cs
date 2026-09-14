using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TurboLoop.EditorTools
{
    public static class TurboLoopMenu
    {
        private const string ScenePath = "Assets/Scenes/Race.unity";

        [MenuItem("Turbo Loop/Create Race Scene")]
        public static void CreateRaceScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("TurboLoop").AddComponent<Core.RaceManager>();
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("Turbo Loop: created " + ScenePath + ". Press Play.");
        }
    }
}
