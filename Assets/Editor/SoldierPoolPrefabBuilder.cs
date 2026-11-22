using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SoldierPoolPrefabBuilder
{
    const string UnitsRoot = "Assets/Battlefield/Units";

    [MenuItem("Tools/Battlefield/Rebuild Soldier Pool Prefab")]
    public static void Build()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { UnitsRoot + "/Prefabs" });
        var prefabs = new SortedDictionary<int, GameObject>();
        foreach (var guid in guids)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            var stats = prefab != null ? prefab.GetComponent<UnitStats>() : null;
            if (stats != null) prefabs[stats.id] = prefab;
        }
        if (prefabs.Count != 13)
        {
            Debug.LogError("Expected 13 soldier prefabs before building the pool; found " + prefabs.Count + ". Run Rebuild Unit Prefabs first.");
            return;
        }
        var root = new GameObject("SoldierPool");
        var pool = root.AddComponent<SoldierPool>();
        var serialized = new SerializedObject(pool);
        var entries = serialized.FindProperty("entries");
        entries.arraySize = prefabs.Count;
        int index = 0;
        foreach (var item in prefabs)
        {
            var entry = entries.GetArrayElementAtIndex(index++);
            entry.FindPropertyRelative("unitId").intValue = item.Key;
            entry.FindPropertyRelative("prefab").objectReferenceValue = item.Value;
            entry.FindPropertyRelative("prewarmCount").intValue = 4;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, UnitsRoot + "/SoldierPool.prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("Built SoldierPool prefab with 13 soldier types.");
    }
}
