using System;

public static class GameEvents
{
    public static event Action OnHit;
    public static event Action OnMiss;

    public static void RaiseOnHit()
    {
        OnHit?.Invoke();
    }

    public static void RaiseOnMiss()
    {
        OnMiss?.Invoke();
    }
}