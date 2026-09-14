using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StrikerFive.EditorTools
{
    public static class StrikerMenu
    {
        private const string ScenePath = "Assets/Scenes/Match.unity";

        [MenuItem("Striker Five/Create Match Scene")]
        public static void CreateMatchScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("StrikerFive").AddComponent<Core.MatchManager>();
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("Striker Five: created " + ScenePath + ". Press Play.");
        }
    }
}
