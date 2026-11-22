using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CostSystemBuilder
{
    const string ScenePath = "Assets/Battlefield/Scenes/Battlefield.unity";

    static CostSystemBuilder() => EditorApplication.delayCall += InstallIfReady;

    static void InstallIfReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) Install();
    }

    [MenuItem("Tools/Battlefield/Install Cost System")]
    public static void Install()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            Debug.LogError("Battlefield scene is missing.");
            return;
        }
        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponent<BattleCost>() != null)
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
                return;
            }
        var wallet = new GameObject("Battle Cost (finite 50 points)");
        SceneManager.MoveGameObjectToScene(wallet, scene);
        wallet.AddComponent<BattleCost>();
        EditorSceneManager.SaveScene(scene);
        if (openedHere) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("Installed finite cost system into Battlefield scene.");
    }
}
