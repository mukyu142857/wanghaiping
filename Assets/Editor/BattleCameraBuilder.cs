using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class BattleCameraBuilder
{
    const string Scenes = "Assets/Battlefield/Scenes/";
    static readonly string[] Stages = { "Dock", "Plain", "Mountain", "River" };

    static BattleCameraBuilder() => EditorApplication.delayCall += Build;

    [MenuItem("Tools/Battlefield/Configure Battle Cameras")]
    public static void Build()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (var key in Stages)
        {
            var path = Scenes + key + ".unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            var scene = SceneManager.GetSceneByPath(path);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            foreach (var root in scene.GetRootGameObjects())
            {
                var camera = root.GetComponent<Camera>();
                if (camera == null || !root.CompareTag("MainCamera")) continue;
                camera.orthographic = true;
                camera.orthographicSize = 30f; // 320 / 3 / 2 / (16 / 9)
                camera.rect = new Rect(0f, 330f / 1080f, 1f, 750f / 1080f);
                root.transform.position = new Vector3(0, 90, -140);
                root.transform.LookAt(new Vector3(0, 12, 0));
                if (root.GetComponent<BattleCameraController>() == null)
                    root.AddComponent<BattleCameraController>();
                EditorSceneManager.SaveScene(scene);
                break;
            }
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
