using dc;
using dc.en;
using dc.tool.atk;
namespace HextechBrawl;
public sealed partial class Main
{
    private bool damageClampLogged;
    private int SafeDamage(Hook__AttackUtils.orig_computeFinalDmg orig, AttackData attack, double damage, double cap)
    {
        if (attack.source is not Hero) return orig(attack, damage, cap);
        double scaled = attack.useHeroScaling
            ? Const.Class.scaleHeroValueToTier.Invoke((object)damage, attack.sourceTier)
            : Const.Class.scaleMobValueToTier.Invoke((object)damage, attack.sourceTier);
        double value = scaled + attack.dmgScaledAdd;
        // 正常伤害继续交给原版与其他模组，只接管有溢出风险的转换。
        if (double.IsFinite(value) && value >= 0 && value <= BalanceRules.SafeDamageCeiling)
            return orig(attack, damage, cap);
        if (!damageClampLogged)
        {
            damageClampLogged = true;
            Logger.Information("[HextechBrawl] Guarded unsafe final damage conversion; native caps and boss phase checks retained");
        }
        return BalanceRules.SafeFinalDamage(scaled, attack.dmgScaledAdd, cap);
    }
}
