using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SoldierPool : MonoBehaviour
{
    [Serializable]
    public sealed class Entry
    {
        public int unitId;
        public GameObject prefab;
        [Min(0)] public int prewarmCount = 4;
    }

    [SerializeField] List<Entry> entries = new List<Entry>();
    readonly Dictionary<int, Stack<GameObject>> available = new Dictionary<int, Stack<GameObject>>();
    readonly Dictionary<int, Entry> byId = new Dictionary<int, Entry>();
    Transform inactiveRoot;
    bool initialized;

    void Awake() => Initialize();

    public void Initialize()
    {
        if (initialized) return;
        var inactiveObject = new GameObject("Inactive Soldiers");
        inactiveObject.transform.SetParent(transform, false);
        inactiveObject.SetActive(false);
        inactiveRoot = inactiveObject.transform;
        foreach (var entry in entries)
        {
            if (entry == null || entry.prefab == null || entry.unitId <= 0)
            {
                Debug.LogError("SoldierPool has an invalid entry.", this);
                continue;
            }
            var stats = entry.prefab.GetComponent<UnitStats>();
            if (stats == null || stats.id != entry.unitId || byId.ContainsKey(entry.unitId))
            {
                Debug.LogError("SoldierPool entry ID does not match a unique UnitStats prefab: " + entry.unitId, this);
                continue;
            }
            byId.Add(entry.unitId, entry);
            var stack = new Stack<GameObject>();
            available.Add(entry.unitId, stack);
            for (int i = 0; i < entry.prewarmCount; i++)
                stack.Push(Create(entry));
        }
        initialized = true;
    }

    public GameObject Spawn(int unitId, Vector3 position, Quaternion rotation)
    {
        Initialize();
        if (!byId.TryGetValue(unitId, out var entry))
        {
            Debug.LogError("No soldier prefab registered for ID " + unitId, this);
            return null;
        }
        var stack = available[unitId];
        var soldier = stack.Count > 0 ? stack.Pop() : Create(entry);
        var member = soldier.GetComponent<PooledSoldier>();
        member.IsRented = true;
        soldier.transform.SetParent(transform, false);
        soldier.transform.SetPositionAndRotation(position, rotation);
        soldier.SetActive(true);
        return soldier;
    }

    public UnitStats GetUnitStats(int unitId)
    {
        if (initialized)
            return byId.TryGetValue(unitId, out var entry) ? entry.prefab.GetComponent<UnitStats>() : null;
        foreach (var entry in entries)
            if (entry != null && entry.unitId == unitId && entry.prefab != null)
                return entry.prefab.GetComponent<UnitStats>();
        return null;
    }

    public bool Despawn(GameObject soldier)
    {
        if (soldier == null) return false;
        Initialize();
        var member = soldier.GetComponent<PooledSoldier>();
        if (member == null || member.Owner != this || !member.IsRented || !available.ContainsKey(member.UnitId))
            return false;
        member.IsRented = false;
        soldier.SetActive(false);
        soldier.transform.SetParent(inactiveRoot, false);
        available[member.UnitId].Push(soldier);
        return true;
    }

    GameObject Create(Entry entry)
    {
        var soldier = Instantiate(entry.prefab, inactiveRoot);
        soldier.SetActive(false);
        var member = soldier.GetComponent<PooledSoldier>();
        if (member == null) member = soldier.AddComponent<PooledSoldier>();
        if (soldier.GetComponent<UnitCombat>() == null) soldier.AddComponent<UnitCombat>();
        member.Owner = this;
        member.UnitId = entry.unitId;
        member.IsRented = false;
        return soldier;
    }
}

public sealed class PooledSoldier : MonoBehaviour
{
    public SoldierPool Owner { get; internal set; }
    public int UnitId { get; internal set; }
    public bool IsRented { get; internal set; }
}
