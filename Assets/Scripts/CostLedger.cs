using System;

// Pure rules for the match's finite supply of points.
public sealed class CostLedger
{
    public int TotalSupply { get; }
    public int Current { get; private set; }
    public int Generated { get; private set; }
    public float SecondsPerPoint { get; }
    float elapsed;

    public event Action Changed = delegate { };

    public CostLedger(int totalSupply = 50, float secondsPerPoint = 1f)
    {
        if (totalSupply < 0) throw new ArgumentOutOfRangeException(nameof(totalSupply));
        if (secondsPerPoint <= 0 || float.IsNaN(secondsPerPoint) || float.IsInfinity(secondsPerPoint))
            throw new ArgumentOutOfRangeException(nameof(secondsPerPoint));
        TotalSupply = totalSupply;
        SecondsPerPoint = secondsPerPoint;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        if (Generated >= TotalSupply) return;
        elapsed += deltaTime;
        while (elapsed >= SecondsPerPoint && Generated < TotalSupply)
        {
            elapsed -= SecondsPerPoint;
            Generated++;
            Current++;
            Changed?.Invoke();
        }
    }

    public bool CanAfford(int amount) => amount >= 0 && Current >= amount;

    public bool TrySpend(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (Current < amount) return false;
        if (amount == 0) return true;
        Current -= amount;
        Changed?.Invoke();
        return true;
    }
}
