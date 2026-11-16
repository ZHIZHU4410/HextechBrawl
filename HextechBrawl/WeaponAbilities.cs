using dc;
using dc.en;
using dc.tool;
using dc.tool.weap;
using HaxeProxy.Runtime;
using Math = System.Math;
namespace HextechBrawl;
public sealed partial class Main
{
    private int wallChecks, volleyDepth, volleySide, shootDepth;
    public static readonly HashSet<string> VolleyKinds = new() { "BarrelLauncher", "AlchemicGun", "CloseCombatBow", "CrossBow", "DualBow", "ExplosiveCrossBow", "FastBow", "FrostBow", "FrostCrossBow", "FrostCrossBowOffHand", "HeavyBow", "InfiniteBow", "LongBow", "MagicBow", "MarkBow", "MultiCrossBow", "MultiCrossBowOffHand", "PreciseBow", "SonicCrossbow", "StartBow", "LanternRanged", "ThrowingTorch" };
    private bool Bound(InventItem item) => item != null && State.BoundWeapons.ContainsKey(item.permanentId);
    private bool WallWeapon(Weapon w) => w != null && EnsureRun(w.owner) && Bound(w.item) && State.Rank("phase_wall") > 0;
    private bool VolleyWeapon(Weapon w) => w != null && EnsureRun(w.owner) && Bound(w.item) && State.Rank("multishot") > 0 &&
        w.item._itemData.group == 5 && VolleyKinds.Contains(w.item._itemData.id.ToString());
    private void RegisterAbilities(bool add)
    {
        if (add)
        {
            Hook_Weapon.canHit += PhaseHit; Hook_PureNail.canHit += PhaseNail;
            Hook_Entity.sightCheckCase += PhaseSight; Hook_Bullet.shootFromWeapon += BulletLaunched;
            Hook_Bullet.fixedUpdate += PhaseBullet; Hook_ThrowingTorch.onExecute += TorchVolley;
        }
        else
        {
            Hook_Weapon.canHit -= PhaseHit; Hook_PureNail.canHit -= PhaseNail;
            Hook_Entity.sightCheckCase -= PhaseSight; Hook_Bullet.shootFromWeapon -= BulletLaunched;
            Hook_Bullet.fixedUpdate -= PhaseBullet; Hook_ThrowingTorch.onExecute -= TorchVolley;
        }
        RegisterVolleys(add);
    }
    private bool PhaseHit(Hook_Weapon.orig_canHit orig, Weapon w, Entity target, Area area)
    {
        if (!WallWeapon(w)) return orig(w, target, area);
        wallChecks++;
        try { return orig(w, target, area); } finally { wallChecks--; }
    }
    private bool PhaseNail(Hook_PureNail.orig_canHit orig, PureNail w, Entity target, Area area)
    {
        if (!WallWeapon(w)) return orig(w, target, area);
        wallChecks++;
        try { return orig(w, target, area); } finally { wallChecks--; }
    }
    private bool PhaseSight(Hook_Entity.orig_sightCheckCase orig, Entity source, int x, int y, Ref<bool> oneWay, int? spot)
        => wallChecks > 0 && source is Hero ? true : orig(source, x, y, oneWay, spot);
    private void PhaseBullet(Hook_Bullet.orig_fixedUpdate orig, Bullet bullet)
    {
        if (bullet.atk?.source is Hero hero && EnsureRun(hero) && Bound(bullet.atk.sourceItem) && State.Rank("phase_wall") > 0)
            bullet.ignoreWalls = true;
        orig(bullet);
    }
    private void BulletLaunched(Hook_Bullet.orig_shootFromWeapon orig, Bullet bullet, Weapon w, Ref<bool> ammo, Ref<double> x, Ref<double> y)
    {
        if (WallWeapon(w)) bullet.ignoreWalls = true;
        orig(bullet, w, ammo, x, y);
        if (volleyDepth <= 0) return;
        double angle = volleySide * .13, dx = bullet.dx, dy = bullet.dy;
        bullet.dx = dx * Math.Cos(angle) - dy * Math.Sin(angle);
        bullet.dy = dx * Math.Sin(angle) + dy * Math.Cos(angle);
        // 额外弹丸不能回收成额外弹药，防止多发复制弹药。
        bullet.itemAmmoRef = null; bullet.ammoDropped = true;
    }
    private void FireVolley(Weapon w, Action fire)
    {
        if (shootDepth > 0) { fire(); return; }
        shootDepth++;
        try
        {
            fire();
            if (!VolleyWeapon(w)) return;
            int ammo = w.item.ammo;
            volleyDepth++;
            try
            {
                foreach (int side in new[] { -1, 1 })
                {
                    volleySide = side;
                    try { fire(); } finally { w.item.ammo = ammo; }
                }
            }
            finally { volleyDepth--; volleySide = 0; }
        }
        finally { shootDepth--; }
    }
    private bool TorchVolley(Hook_ThrowingTorch.orig_onExecute orig, ThrowingTorch w)
    {
        bool result = false;
        FireVolley(w, () => result = orig(w));
        return result;
    }
}
