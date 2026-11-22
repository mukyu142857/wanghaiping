using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class BattlefieldBuilder
{
    const string Root = "Assets/Battlefield";
    const float PixelsPerUnit = 100f;
    const float MapLength = 320f;
    const float MapDepth = 64f;
    const float FortressSpacing = 280f;

    static BattlefieldBuilder()
    {
        EditorApplication.delayCall += BuildOnFirstImport;
    }

    static void BuildOnFirstImport()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Root + "/Scenes/Battlefield.unity") == null)
            Build();
    }

    [MenuItem("Tools/Battlefield/Rebuild Prototype Scene")]
    public static void Build()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Prefabs");
        Directory.CreateDirectory(Root + "/Scenes");
        AssetDatabase.Refresh();

        var groundMaterial = Material("Ground", new Color(0.39f, 0.46f, 0.39f));
        var lineMaterial = Material("CenterLine", new Color(0.79f, 0.75f, 0.62f));
        var leftMaterial = Material("LeftFortress", new Color(0.25f, 0.45f, 0.75f));
        var rightMaterial = Material("RightFortress", new Color(0.76f, 0.35f, 0.31f));
        var trimMaterial = Material("FortressTrim", new Color(0.18f, 0.22f, 0.26f));

        var leftPrefab = FortressPrefab("LeftFortress", FortressData.Side.Left, leftMaterial, trimMaterial);
        var rightPrefab = FortressPrefab("RightFortress", FortressData.Side.Right, rightMaterial, trimMaterial);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        scene.name = "Battlefield";

        Cube("Battlefield 320 x 64", null, new Vector3(0, -0.12f, 0), new Vector3(MapLength, 0.2f, MapDepth), groundMaterial);
        Cube("Center guide", null, new Vector3(0, 0.012f, 0), new Vector3(MapLength, 0.025f, 0.12f), lineMaterial);

        Place(leftPrefab, new Vector3(-FortressSpacing / 2f, 0, 0));
        Place(rightPrefab, new Vector3(FortressSpacing / 2f, 0, 0));

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 102f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.79f, 0.84f, 0.84f);
        cameraObject.transform.position = new Vector3(0, 112, -145);
        cameraObject.transform.LookAt(new Vector3(0, 8, 0));

        var lightObject = new GameObject("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;

        EditorSceneManager.SaveScene(scene, Root + "/Scenes/Battlefield.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Root + "/Scenes/Battlefield.unity", true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Battlefield prototype generated. Scale: " + PixelsPerUnit + " px per Unity unit.");
    }

    static Material Material(string name, Color color)
    {
        var path = Root + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    static GameObject FortressPrefab(string name, FortressData.Side side, Material face, Material trim)
    {
        var root = new GameObject(name);
        var data = root.AddComponent<FortressData>();
        data.side = side;
        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, 12f, 0);
        collider.size = new Vector3(16f, 24f, 4f);

        // The front surface is a replaceable placeholder for future 2D artwork.
        Cube("Artwork placeholder 2560 x 3200 px", root.transform, new Vector3(0, 16f, 0), new Vector3(25.6f, 32f, 0.35f), face, false);
        Cube("Base", root.transform, new Vector3(0, 1.1f, -0.3f), new Vector3(27f, 2.2f, 4f), trim, false);
        Cube("Battlement", root.transform, new Vector3(0, 31.2f, -0.3f), new Vector3(27f, 1.7f, 2.4f), trim, false);
        Cube("Left tower", root.transform, new Vector3(-11f, 17f, -0.35f), new Vector3(4.2f, 29f, 1.3f), trim, false);
        Cube("Right tower", root.transform, new Vector3(11f, 17f, -0.35f), new Vector3(4.2f, 29f, 1.3f), trim, false);

        var path = Root + "/Prefabs/" + name + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static void Place(GameObject prefab, Vector3 position)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
    }

    static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider = true)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        if (parent != null) cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider) Object.DestroyImmediate(cube.GetComponent<BoxCollider>());
        return cube;
    }
}
