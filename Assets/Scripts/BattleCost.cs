using UnityEngine;

public sealed class BattleCost : MonoBehaviour
{
    [SerializeField, Min(0)] int totalSupply = 50;
    [SerializeField, Min(0.01f)] float secondsPerPoint = 1f;
    [SerializeField] bool showHud = true;

    public CostLedger Ledger { get; private set; }

    void Awake() => Ledger = new CostLedger(totalSupply, secondsPerPoint);
    void Update() => Ledger.Tick(Time.deltaTime);

    public bool CanAfford(int amount) => Ledger.CanAfford(amount);
    public bool TrySpend(int amount) => Ledger.TrySpend(amount);

    void OnGUI()
    {
        if (!showHud || Ledger == null) return;
        GUI.Box(new Rect(16, 16, 230, 54), "费用");
        GUI.Label(new Rect(28, 39, 205, 24), Ledger.Current + " 可用 / " + Ledger.Generated + " 已回复 / " + Ledger.TotalSupply + " 总量");
    }
}
