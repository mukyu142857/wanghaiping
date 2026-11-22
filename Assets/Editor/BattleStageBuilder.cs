using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class BattleStageBuilder
{
    const string Scenes = "Assets/Battlefield/Scenes/";
    const string Materials = "Assets/Battlefield/Materials/";

    struct StageSpec
    {
        public string key, title;
        public BattleStage.Terrain terrain;
        public int specialPlayer, specialEnemy;
        public Color ground, sky;

        public StageSpec(string key, string title, BattleStage.Terrain terrain, int player, int enemy, Color ground, Color sky)
        {
            this.key = key; this.title = title; this.terrain = terrain;
            specialPlayer = player; specialEnemy = enemy; this.ground = ground; this.sky = sky;
        }
    }

    static readonly StageSpec[] Stages =
    {
        new StageSpec("Dock", "码头", BattleStage.Terrain.Dock, 4, 10, new Color(.52f,.55f,.50f), new Color(.69f,.80f,.84f)),
        new StageSpec("Plain", "平原", BattleStage.Terrain.Plain, 5, 11, new Color(.43f,.57f,.35f), new Color(.76f,.86f,.77f)),
        new StageSpec("Mountain", "山地", BattleStage.Terrain.Mountain, 6, 12, new Color(.46f,.46f,.47f), new Color(.72f,.76f,.81f)),
        new StageSpec("River", "河流", BattleStage.Terrain.River, 7, 13, new Color(.35f,.53f,.57f), new Color(.69f,.81f,.86f))
    };

    static BattleStageBuilder() => EditorApplication.delayCall += BuildIfMissing;

    static void BuildIfMissing()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Scenes + "Battlefield.unity") == null) return;
        Build();
    }

    [MenuItem("Tools/Battlefield/Create Four Stage Variants")]
    public static void Build()
    {
        var source = Scenes + "Battlefield.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(source) == null)
        {
            Debug.LogError("Create the Battlefield scene first.");
            return;
        }

        var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var stage in Stages)
        {
            var path = Scenes + stage.key + ".unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                if (!AssetDatabase.CopyAsset(source, path))
                {
                    Debug.LogError("Failed to copy Battlefield to " + path);
                    continue;
                }
                AssetDatabase.Refresh();
            }
            Configure(path, stage);
            if (!buildScenes.Exists(item => item.path == path))
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Created four stage variants. Existing stage scenes were preserved.");
    }

    static void Configure(string path, StageSpec spec)
    {
        var scene = SceneManager.GetSceneByPath(path);
        bool openedHere = !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        foreach (var root in scene.GetRootGameObjects())
            if (root.GetComponent<BattleStage>() != null)
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
                return;
            }
        var stageObject = new GameObject("Stage - " + spec.title);
        SceneManager.MoveGameObjectToScene(stageObject, scene);
        var stage = stageObject.AddComponent<BattleStage>();
        stage.terrain = spec.terrain;
        stage.displayName = spec.title;
        stage.playerUnitIds = new[] { 1, 2, 3, spec.specialPlayer };
        stage.enemyUnitIds = new[] { 8, 9, spec.specialEnemy };

        var preview = new GameObject("Unit Lineup Preview");
        preview.transform.SetParent(stageObject.transform, false);
        PlacePreview(stage.playerUnitIds, true, preview.transform, scene);
        PlacePreview(stage.enemyUnitIds, false, preview.transform, scene);

        var background = new GameObject("Background Artwork Slot");
        background.transform.SetParent(stageObject.transform, false);
        stage.backgroundArtwork = background.AddComponent<SpriteRenderer>();

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "Battlefield 320 x 64")
            {
                var renderer = root.GetComponent<Renderer>();
                var materialPath = Materials + spec.key + "Ground.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(renderer.sharedMaterial);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                material.color = spec.ground;
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(material);
            }
            if (root.CompareTag("MainCamera"))
                root.GetComponent<Camera>().backgroundColor = spec.sky;
        }
        EditorSceneManager.SaveScene(scene);
        if (openedHere) EditorSceneManager.CloseScene(scene, true);
    }

    static void PlacePreview(int[] ids, bool player, Transform parent, Scene scene)
    {
        foreach (var id in ids)
        {
            var matches = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Battlefield/Units/Prefabs" });
            GameObject prefab = null;
            foreach (var guid in matches)
            {
                var candidate = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var stats = candidate != null ? candidate.GetComponent<UnitStats>() : null;
                if (stats != null && stats.id == id) { prefab = candidate; break; }
            }
            if (prefab == null) continue;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            SceneManager.MoveGameObjectToScene(instance, scene);
            instance.transform.SetParent(parent, false);
            int index = System.Array.IndexOf(ids, id);
            instance.transform.position = new Vector3(player ? -108f + index * 13f : 108f - index * 13f, 0, (index - 1.5f) * 11f);
        }
    }
}
