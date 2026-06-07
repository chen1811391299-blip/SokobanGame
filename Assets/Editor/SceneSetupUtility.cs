using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public static class SceneSetupUtility
{
    [MenuItem("SokobanGame/3. Create All Scenes")]
    public static void CreateAllScenes()
    {
        string[] scenes = { "MainMenu", "LevelSelect", "Gameplay", "LevelEditor" };

        // Ensure Scenes folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        foreach (var name in scenes)
        {
            string path = $"Assets/Scenes/{name}.unity";
            if (File.Exists(Application.dataPath + $"/Scenes/{name}.unity"))
            {
                Debug.Log($"[SceneSetup] {name} already exists, skipping.");
                continue;
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true);
            Debug.Log($"[SceneSetup] Created {path}");
        }

        // Add all to Build Settings
        var buildScenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity",   true),
            new EditorBuildSettingsScene("Assets/Scenes/LevelSelect.unity",true),
            new EditorBuildSettingsScene("Assets/Scenes/Gameplay.unity",   true),
            new EditorBuildSettingsScene("Assets/Scenes/LevelEditor.unity",true),
        };
        EditorBuildSettings.scenes = buildScenes;

        AssetDatabase.Refresh();
        Debug.Log("[SceneSetup] Done — 4 scenes created and added to Build Settings.");
    }
}
