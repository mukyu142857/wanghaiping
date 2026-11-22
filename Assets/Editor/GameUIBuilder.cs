using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class GameUIBuilder
{
    const string Scenes = "Assets/Battlefield/Scenes/";
    static readonly string[] Stages = { "Mountain", "Plain", "Dock", "River" };

    static GameUIBuilder() => EditorApplication.delayCall += BuildIfMissing;

    static void BuildIfMissing()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Scenes + "Battlefield.unity") != null)
            Build();
    }

    [MenuItem("Tools/Battlefield/Build Document UI")]
    public static void Build()
    {
        BattleStageBuilder.Build();
        var menuPath = Scenes + "MainMenu.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(menuPath) == null)
        {
            if (!AssetDatabase.CopyAsset(Scenes + "Battlefield.unity", menuPath))
            {
                Debug.LogError("Could not copy Battlefield scene for MainMenu UI.");
                return;
            }
            AssetDatabase.Refresh();
        }
        var menuScene = SceneManager.GetSceneByPath(menuPath);
        bool openedMenuHere = !menuScene.isLoaded;
        if (openedMenuHere) menuScene = EditorSceneManager.OpenScene(menuPath, OpenSceneMode.Additive);
        bool hasMenuUi = false;
        foreach (var menuRoot in menuScene.GetRootGameObjects())
            if (menuRoot.GetComponent<GameUI>() != null) hasMenuUi = true;
        if (!hasMenuUi)
        {
            foreach (var staleMenuRoot in menuScene.GetRootGameObjects()) Object.DestroyImmediate(staleMenuRoot);
            var menuUiRoot = new GameObject("Game UI");
            SceneManager.MoveGameObjectToScene(menuUiRoot, menuScene);
            menuUiRoot.AddComponent<GameUI>();
        }
        foreach (var previewMenuRoot in menuScene.GetRootGameObjects())
        {
            var ui = previewMenuRoot.GetComponent<GameUI>();
            if (ui != null) { ui.BuildEditorPreview(); break; }
        }
        EditorSceneManager.SaveScene(menuScene);
        if (openedMenuHere) EditorSceneManager.CloseScene(menuScene, true);

        var poolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Battlefield/Units/SoldierPool.prefab");
        foreach (var key in Stages)
        {
            var path = Scenes + key + ".unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            var scene = SceneManager.GetSceneByPath(path);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            bool changed = false, hasPool = false, hasUi = false;
            foreach (var existingRoot in scene.GetRootGameObjects())
            {
                if (existingRoot.GetComponent<SoldierPool>() != null) hasPool = true;
                if (existingRoot.GetComponent<GameUI>() != null) hasUi = true;
            }
            if (!hasPool && poolPrefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(poolPrefab);
                SceneManager.MoveGameObjectToScene(instance, scene);
                changed = true;
            }
            if (!hasUi)
            {
                var battleUiRoot = new GameObject("Game UI");
                SceneManager.MoveGameObjectToScene(battleUiRoot, scene);
                var ui = battleUiRoot.AddComponent<GameUI>();
                var serialized = new SerializedObject(ui);
                serialized.FindProperty("battleMode").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
            foreach (var sceneRoot in scene.GetRootGameObjects())
            {
                var ui = sceneRoot.GetComponent<GameUI>();
                if (ui != null) { ui.BuildEditorPreview(); changed = true; break; }
            }
            if (changed) EditorSceneManager.SaveScene(scene);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }
        var buildScenes = new List<EditorBuildSettingsScene>();
        buildScenes.Add(new EditorBuildSettingsScene(menuPath, true));
        foreach (var item in EditorBuildSettings.scenes)
            if (item.path != menuPath) buildScenes.Add(item);
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Built document UI scenes and battle overlays.");
    }
}
