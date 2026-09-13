using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echobound.EditorTools
{
    /// <summary>Editor conveniences: create the bootstrap scene, open the save folder, create the local AI config.</summary>
    public static class EchoboundMenu
    {
        private const string ScenePath = "Assets/Scenes/Town.unity";

        [MenuItem("Echobound/Create Town Scene")]
        public static void CreateTownScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Echobound");
            root.AddComponent<Core.GameManager>();
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("Echobound: created " + ScenePath + ". Press Play. Everything else is built at runtime.");
        }

        [MenuItem("Echobound/Open Save Folder")]
        public static void OpenSaveFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath);

        [MenuItem("Echobound/Delete Save File")]
        public static void DeleteSave()
        {
            var path = Path.Combine(Application.persistentDataPath, "echobound_save.json");
            if (File.Exists(path)) { File.Delete(path); Debug.Log("Deleted " + path); }
        }

        [MenuItem("Echobound/Create Local AI Config")]
        public static void CreateLocalConfig()
        {
            var example = Path.Combine(Application.streamingAssetsPath, "ai_config.example.json");
            var local = Path.Combine(Application.streamingAssetsPath, AI.AIConfig.LocalFileName);
            if (!File.Exists(local) && File.Exists(example)) File.Copy(example, local);
            Debug.Log("Edit " + local + " (git-ignored): set provider to anthropic/openai and add api_key or proxy_url.");
            AssetDatabase.Refresh();
        }
    }
}
