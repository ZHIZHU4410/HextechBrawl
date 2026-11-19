namespace HextechBrawl;
public static class BalanceRules
{
    public static int RewardLevel(int nativeLootLevel) => System.Math.Clamp(nativeLootLevel, 1, 27);
    public static double BindingDamage(int layers) => 1 + .04 * System.Math.Clamp(layers, 0, 25);
    public static double BindingSpeed(int layers) => 1 + .015 * System.Math.Clamp(layers, 0, 30);
    public const double BoundDamageBoost = 2;
    public static double BoundAttackMultiplier(bool bound, int layers) => bound ? BoundDamageBoost * BindingDamage(layers) : 1;
    public static double DamagePenalty(double raw) => System.Math.Clamp(raw, .60, 1);
    public const int SafeDamageCeiling = 100_000_000;
    public static int SafeFinalDamage(double scaledDamage, double additive, double nativeCap)
    {
        double value = scaledDamage + additive;
        if (double.IsNaN(value)) value = 0;
        if (double.IsFinite(nativeCap) && nativeCap >= 0) value = System.Math.Min(value, nativeCap);
        return (int)System.Math.Clamp(value, 0, SafeDamageCeiling);
    }
    public const double VolleyDamage = .45;
}
