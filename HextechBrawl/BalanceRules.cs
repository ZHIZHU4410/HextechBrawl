namespace HextechBrawl;
public static class BalanceRules
{
    public static int RewardLevel(int nativeLootLevel) => System.Math.Clamp(nativeLootLevel, 1, 27);
    public static double BindingDamage(int layers) => 1 + .04 * System.Math.Clamp(layers, 0, 25);
    public static double BindingSpeed(int layers) => 1 + .015 * System.Math.Clamp(layers, 0, 30);
    public const double VolleyDamage = .45;
}
