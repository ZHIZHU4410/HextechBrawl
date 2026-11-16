using dc.tool;
using dc.hl.types;
namespace HextechBrawl;
public sealed partial class Main
{
    private void RegisterVolleys(bool add)
    {
        if (add) dc.tool.weap.Hook_BarrelLauncher.shoot += VolleyBarrelLauncher; else dc.tool.weap.Hook_BarrelLauncher.shoot -= VolleyBarrelLauncher;
        if (add) dc.tool.weap.bow.Hook_AlchemicGun.shoot += VolleyAlchemicGun; else dc.tool.weap.bow.Hook_AlchemicGun.shoot -= VolleyAlchemicGun;
        if (add) dc.tool.weap.bow.Hook_CloseCombatBow.shoot += VolleyCloseCombatBow; else dc.tool.weap.bow.Hook_CloseCombatBow.shoot -= VolleyCloseCombatBow;
        if (add) dc.tool.weap.bow.Hook_CrossBow.shoot += VolleyCrossBow; else dc.tool.weap.bow.Hook_CrossBow.shoot -= VolleyCrossBow;
        if (add) dc.tool.weap.bow.Hook_DualBow.shoot += VolleyDualBow; else dc.tool.weap.bow.Hook_DualBow.shoot -= VolleyDualBow;
        if (add) dc.tool.weap.bow.Hook_ExplosiveCrossBow.shoot += VolleyExplosiveCrossBow; else dc.tool.weap.bow.Hook_ExplosiveCrossBow.shoot -= VolleyExplosiveCrossBow;
        if (add) dc.tool.weap.bow.Hook_FastBow.shoot += VolleyFastBow; else dc.tool.weap.bow.Hook_FastBow.shoot -= VolleyFastBow;
        if (add) dc.tool.weap.bow.Hook_FrostBow.shoot += VolleyFrostBow; else dc.tool.weap.bow.Hook_FrostBow.shoot -= VolleyFrostBow;
        if (add) dc.tool.weap.bow.Hook_FrostCrossBow.shoot += VolleyFrostCrossBow; else dc.tool.weap.bow.Hook_FrostCrossBow.shoot -= VolleyFrostCrossBow;
        if (add) dc.tool.weap.bow.Hook_FrostCrossBowOffHand.shoot += VolleyFrostCrossBowOffHand; else dc.tool.weap.bow.Hook_FrostCrossBowOffHand.shoot -= VolleyFrostCrossBowOffHand;
        if (add) dc.tool.weap.bow.Hook_HeavyBow.shoot += VolleyHeavyBow; else dc.tool.weap.bow.Hook_HeavyBow.shoot -= VolleyHeavyBow;
        if (add) dc.tool.weap.bow.Hook_InfiniteBow.shoot += VolleyInfiniteBow; else dc.tool.weap.bow.Hook_InfiniteBow.shoot -= VolleyInfiniteBow;
        if (add) dc.tool.weap.bow.Hook_LongBow.shoot += VolleyLongBow; else dc.tool.weap.bow.Hook_LongBow.shoot -= VolleyLongBow;
        if (add) dc.tool.weap.bow.Hook_MagicBow.shoot += VolleyMagicBow; else dc.tool.weap.bow.Hook_MagicBow.shoot -= VolleyMagicBow;
        if (add) dc.tool.weap.bow.Hook_MarkBow.shoot += VolleyMarkBow; else dc.tool.weap.bow.Hook_MarkBow.shoot -= VolleyMarkBow;
        if (add) dc.tool.weap.bow.Hook_MultiCrossBow.shoot += VolleyMultiCrossBow; else dc.tool.weap.bow.Hook_MultiCrossBow.shoot -= VolleyMultiCrossBow;
        if (add) dc.tool.weap.bow.Hook_MultiCrossBowOffHand.shoot += VolleyMultiCrossBowOffHand; else dc.tool.weap.bow.Hook_MultiCrossBowOffHand.shoot -= VolleyMultiCrossBowOffHand;
        if (add) dc.tool.weap.bow.Hook_PreciseBow.shoot += VolleyPreciseBow; else dc.tool.weap.bow.Hook_PreciseBow.shoot -= VolleyPreciseBow;
        if (add) dc.tool.weap.bow.Hook_SonicCrossbow.shoot += VolleySonicCrossbow; else dc.tool.weap.bow.Hook_SonicCrossbow.shoot -= VolleySonicCrossbow;
        if (add) dc.tool.weap.bow.Hook_StartBow.shoot += VolleyStartBow; else dc.tool.weap.bow.Hook_StartBow.shoot -= VolleyStartBow;
        if (add) dc.tool.weap.dual.Hook_LanternRanged.shoot += VolleyLanternRanged; else dc.tool.weap.dual.Hook_LanternRanged.shoot -= VolleyLanternRanged;
    }
    private void VolleyBarrelLauncher(dc.tool.weap.Hook_BarrelLauncher.orig_shoot orig, dc.tool.weap.BarrelLauncher weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyAlchemicGun(dc.tool.weap.bow.Hook_AlchemicGun.orig_shoot orig, dc.tool.weap.bow.AlchemicGun weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyCloseCombatBow(dc.tool.weap.bow.Hook_CloseCombatBow.orig_shoot orig, dc.tool.weap.bow.CloseCombatBow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyCrossBow(dc.tool.weap.bow.Hook_CrossBow.orig_shoot orig, dc.tool.weap.bow.CrossBow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyDualBow(dc.tool.weap.bow.Hook_DualBow.orig_shoot orig, dc.tool.weap.bow.DualBow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyExplosiveCrossBow(dc.tool.weap.bow.Hook_ExplosiveCrossBow.orig_shoot orig, dc.tool.weap.bow.ExplosiveCrossBow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyFastBow(dc.tool.weap.bow.Hook_FastBow.orig_shoot orig, dc.tool.weap.bow.FastBow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyFrostBow(dc.tool.weap.bow.Hook_FrostBow.orig_shoot orig, dc.tool.weap.bow.FrostBow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyFrostCrossBow(dc.tool.weap.bow.Hook_FrostCrossBow.orig_shoot orig, dc.tool.weap.bow.FrostCrossBow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyFrostCrossBowOffHand(dc.tool.weap.bow.Hook_FrostCrossBowOffHand.orig_shoot orig, dc.tool.weap.bow.FrostCrossBowOffHand weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyHeavyBow(dc.tool.weap.bow.Hook_HeavyBow.orig_shoot orig, dc.tool.weap.bow.HeavyBow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyInfiniteBow(dc.tool.weap.bow.Hook_InfiniteBow.orig_shoot orig, dc.tool.weap.bow.InfiniteBow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyLongBow(dc.tool.weap.bow.Hook_LongBow.orig_shoot orig, dc.tool.weap.bow.LongBow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyMagicBow(dc.tool.weap.bow.Hook_MagicBow.orig_shoot orig, dc.tool.weap.bow.MagicBow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyMarkBow(dc.tool.weap.bow.Hook_MarkBow.orig_shoot orig, dc.tool.weap.bow.MarkBow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyMultiCrossBow(dc.tool.weap.bow.Hook_MultiCrossBow.orig_shoot orig, dc.tool.weap.bow.MultiCrossBow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyMultiCrossBowOffHand(dc.tool.weap.bow.Hook_MultiCrossBowOffHand.orig_shoot orig, dc.tool.weap.bow.MultiCrossBowOffHand weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyPreciseBow(dc.tool.weap.bow.Hook_PreciseBow.orig_shoot orig, dc.tool.weap.bow.PreciseBow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleySonicCrossbow(dc.tool.weap.bow.Hook_SonicCrossbow.orig_shoot orig, dc.tool.weap.bow.SonicCrossbow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyStartBow(dc.tool.weap.bow.Hook_StartBow.orig_shoot orig, dc.tool.weap.bow.StartBow weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
    private void VolleyLanternRanged(dc.tool.weap.dual.Hook_LanternRanged.orig_shoot orig, dc.tool.weap.dual.LanternRanged weapon, ArrayObj output) => FireVolley(weapon, () => orig(weapon, output));
}