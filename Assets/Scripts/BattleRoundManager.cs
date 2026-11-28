using UnityEngine;

public sealed class BattleRoundManager : MonoBehaviour
{
    static readonly int[] ObjectiveHealth = { 800, 1000 };
    static readonly float[] ObjectivePositions = { -160f / 3f, 160f / 3f };

    BattleStage stage;
    BattleCost cost;
    EnemySpawner spawner;
    GameUI ui;
    BattleTarget objective;
    BattleTarget playerFortress;
    BattleTarget enemyFortress;
    bool defenseWaveFinished;
    bool awaitingAdvance;

    public int Round { get; private set; }
    public string Goal => Round == 1 ? "防守：消灭所有敌兵" : Round == 4 ? "摧毁敌方堡垒" : "摧毁据点 " + (Round - 1) + " / 2";
    public string ObjectiveStatus => Round == 4 && enemyFortress != null
        ? "  敌堡 " + enemyFortress.Health + "/" + enemyFortress.maxHealth
        : objective == null ? "" : "  据点 " + objective.Health + "/" + objective.maxHealth;
    public int PlayerFortressHealth => playerFortress == null ? 0 : playerFortress.Health;

    public void Initialize(BattleStage battleStage, BattleCost battleCost, EnemySpawner enemySpawner, GameUI gameUi)
    {
        stage = battleStage;
        cost = battleCost;
        spawner = enemySpawner;
        ui = gameUi;
        foreach (var fortress in FindObjectsOfType<FortressData>())
        {
            var target = fortress.GetComponent<BattleTarget>();
            if (target == null) target = fortress.gameObject.AddComponent<BattleTarget>();
            if (fortress.side == FortressData.Side.Left) playerFortress = target;
            else enemyFortress = target;
        }
        BeginRound(1);
    }

    void BeginRound(int number)
    {
        Round = number;
        awaitingAdvance = false;
        cost.ResetRound();
        ui.ResetOrder();
        foreach (var soldier in FindObjectsOfType<UnitCombat>())
            if (soldier.IsPlayer && soldier.IsAlive) soldier.SetOrder(UnitCombat.Order.Defense);
        if (objective != null) Destroy(objective.gameObject);
        objective = null;
        if (enemyFortress != null) enemyFortress.isObjective = Round == 4;
        if (Round == 2 || Round == 3) CreateObjective(Round - 2);
        spawner.enabled = true;
        spawner.Initialize(stage, FindObjectOfType<SoldierPool>());
    }

    void CreateObjective(int index)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "据点 " + (index + 1);
        marker.transform.position = new Vector3(ObjectivePositions[index], 6f, 0f);
        marker.transform.localScale = new Vector3(8f, 12f, 8f);
        var renderer = marker.GetComponent<Renderer>();
        renderer.material.color = new Color(.58f, .38f, .23f);
        objective = marker.AddComponent<BattleTarget>();
        objective.ConfigureObjective(ObjectiveHealth[index]);
    }

    void Update()
    {
        if (Round == 0 || ui == null || awaitingAdvance) return;
        if (playerFortress == null || playerFortress.Health <= 0)
        {
            ui.ShowResult(false, "我方堡垒被摧毁");
            enabled = false;
            return;
        }
        bool complete = Round == 1 ? defenseWaveFinished && NoEnemySoldiers()
            : Round == 4 ? enemyFortress != null && enemyFortress.Health <= 0
            : objective == null || objective.Health <= 0;
        if (complete)
        {
            spawner.enabled = false;
            awaitingAdvance = true;
            if (Round == 4) ui.ShowResult(true, "四回合目标已完成");
            else ui.ShowRoundComplete(Round);
            return;
        }
        if (cost.Ledger != null && cost.Ledger.Generated >= cost.Ledger.TotalSupply &&
            cost.Ledger.Current < CheapestUnitCost() && NoPlayerSoldiers())
        {
            ui.ShowResult(false, "费用与兵力耗尽，回合任务未完成");
            enabled = false;
        }
    }

    // The first defense wave will be supplied by the later enemy-wave design.
    public void MarkDefenseWaveFinished()
    {
        defenseWaveFinished = true;
        spawner.enabled = false;
    }

    public void AdvanceRound()
    {
        if (!awaitingAdvance || Round >= 4) return;
        BeginRound(Round + 1);
    }

    bool NoEnemySoldiers()
    {
        foreach (var soldier in FindObjectsOfType<UnitCombat>())
            if (soldier.IsAlive && !soldier.IsPlayer) return false;
        return true;
    }

    bool NoPlayerSoldiers()
    {
        foreach (var soldier in FindObjectsOfType<UnitCombat>())
            if (soldier.IsAlive && soldier.IsPlayer) return false;
        return true;
    }

    int CheapestUnitCost()
    {
        int cheapest = int.MaxValue;
        var pool = FindObjectOfType<SoldierPool>();
        foreach (int id in stage.playerUnitIds)
        {
            var stats = pool.GetUnitStats(id);
            if (stats != null && stats.cost < cheapest) cheapest = stats.cost;
        }
        return cheapest;
    }
}
