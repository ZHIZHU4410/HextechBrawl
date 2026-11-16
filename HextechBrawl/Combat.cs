using dc;
using dc.en;
using dc.en.mob;
using dc.tool.atk;
using HaxeProxy.Runtime;
using Math = System.Math;

namespace HextechBrawl;

public sealed partial class Main
{
    private sealed class Dot(Mob target, double damage)
    {
        public Mob Target = target;
        public double Damage = damage, Timer = 1;
        public int Ticks = 3;
    }
    private readonly Dictionary<(nint, string), Dot> dots = new();
    private int procDepth;
    private double frenzyTime, sinceHurt = 99, sparkLock, predatorLock, novaTime = 5, shieldTime = 8, healAccumulator;
    private bool shieldReady;
    private void ResetCombat()
    {
        dots.Clear(); frenzyTime = sparkLock = predatorLock = healAccumulator = 0;
        sinceHurt = 99; novaTime = 5; shieldTime = 8; shieldReady = false;
    }
    private IEnumerable<Mob> Enemies(Hero hero, bool screen = false, Entity center = null, double radius = 0)
    {
        var result = new List<Mob>();
        var entities = hero._level.entities;
        for (int i = 0; i < entities.length; i++)
        {
            // ArrayObj 的原始元素是 HashlinkObject，必须通过动态转换得到具体代理。
            Entity entity = (Entity)(dynamic)entities.array[i];
            if (entity is not Mob mob || mob.destroyed || mob.onDieDone || mob.life <= 0 ||
                mob._level?.HashlinkPointer != hero._level.HashlinkPointer || !hero.isOpponent(mob) || screen && !mob.isOnScreen) continue;
            if (center != null && radius > 0 && DistanceSq(center, mob) > radius * radius) continue;
            result.Add(mob);
        }
        return result; // 攻击会改变关卡实体列表，必须先完成快照。
    }
    private static double DistanceSq(Entity a, Entity b) => Math.Pow(a.cx - b.cx, 2) + Math.Pow(a.cy - b.cy, 2);

    private void OnHit(Hook__AttackUtils.orig_hit orig, AttackData attack, Entity target)
    {
        var hero = dc.pr.Game.Class.ME?.hero;
        if (procDepth > 0 || hero == null || attack == null || target == null || !EnsureRun(hero) || State.Choices == 0 && State.Route.Length == 0)
        { orig(attack, target); return; }
        bool outgoing = target is Mob m && m.life > 0 && hero.isOpponent(m) && attack.source != null &&
            attack.source._team == hero._team;
        bool incoming = target.HashlinkPointer == hero.HashlinkPointer && attack.source is Mob source && hero.isOpponent(source);
        if (!outgoing && !incoming) { orig(attack, target); return; }
        double oldMultiplier = attack.dmgMultiplier, oldCrit = attack.critMul;
        bool oldTag = attack.hasTag(2);
        int before = target.life;
        if (outgoing)
        {
            var mob = (Mob)target;
            var sourceItem = attack.sourceItem ?? attack.sourceWeapon?.item;
            double binding = BalanceRules.BoundAttackMultiplier(Bound(sourceItem), State.BindingStacks(sourceItem?.permanentId ?? -1));
            double bonus = 1 + .30 * State.Rank("might") + .25 * State.Rank("end_power") + State.Rank("pact") + .40 * State.Rank("curse_power");
            if (State.Synergy(Family.Assault, 2)) bonus += .25;
            if (hero.life >= hero.maxLife * .90) bonus += .45 * State.Rank("focus");
            if (hero.life < hero.maxLife * .50) bonus += .75 * State.Rank("berserk");
            if (hero.life < hero.maxLife * .35) bonus += 2 * State.Rank("laststand");
            if (mob is Boss || mob.elite) bonus += .60 * State.Rank("slayer");
            if (mob.life >= mob.maxLife * .90) bonus += .70 * State.Rank("hunter");
            if (frenzyTime > 0) bonus += .60 * State.Rank("frenzy");
            bonus += (State.Kills / 10) * .05 * State.Rank("momentum");
            bonus += Math.Min(2.4, (hero._level.game.data.money / 1000) * .12) * State.Rank("investment");
            if (State.Rank("execute") > 0 && mob is Boss && mob.life < mob.maxLife * .30) bonus += 1;
            attack.dmgMultiplier *= Math.Min(100_000, State.Growth * bonus * binding * DamageTradeoff * (attack.sourceWeapon != null ?
                State.IsRoute(Family.Assault) ? 1.15 : State.IsRoute(Family.Arcane) ? .90 : 1 : 1));
            if (Bound(sourceItem))
            {
                if (State.Rank("phase_wall") > 0) attack.dmgMultiplier *= .90;
                if (State.Rank("multishot") > 0 && sourceItem._itemData.group == 5 && VolleyCompatibility.Kinds.Contains(sourceItem._itemData.id.ToString()))
                    attack.dmgMultiplier *= BalanceRules.VolleyDamage;
            }
            bool rhythm = State.Rank("rhythm") > 0 && (State.Hits + 1) % 4 == 0;
            bool lucky = State.Rank("lucky") > 0 && State.Chance(.20 * State.Rank("lucky"));
            if (rhythm || lucky) attack.addTag(2);
            if (attack.hasTag(2)) attack.critMul += .35 * State.Rank("rhythm") + .20 * State.Rank("lucky");
        }
        else
        {
            double reduction = Math.Min(.75, .15 * State.Rank("armor") + (State.Synergy(Family.Fortress, 2) ? .10 : 0));
            attack.dmgMultiplier *= (1 - reduction) * (State.IsRoute(Family.Assault) ? 1.10 : 1);
            if (shieldReady) attack.dmgMultiplier *= .10;
        }
        try { orig(attack, target); }
        finally
        {
            // 一份 AttackData 可能攻击多个敌人，必须恢复共享实例，不能在多目标时指数叠加。
            attack.dmgMultiplier = oldMultiplier; attack.critMul = oldCrit; attack.setTag(2, oldTag);
        }
        int dealt = Math.Max(0, before - Math.Max(0, target.life));
        bool critical = attack.hitResult?.Index == HitResult.Indexes.Critical;
        if (dealt <= 0) return; // 无敌、格挡或未命中的攻击不能触发效果。
        if (incoming)
        {
            sinceHurt = 0;
            if (shieldReady) { shieldReady = false; shieldTime = 8; }
            if (hero.life > 0 && attack.source is Mob attacker && State.Rank("thorns") > 0)
                Proc(hero, attacker, dealt * 2d * State.Rank("thorns") * DamageTradeoff, 0xC6A8EA);
            return;
        }
        State.Hits++;
        var victim = (Mob)target;
        Heal(hero, dealt * ((State.IsRoute(Family.Blood) ? .02 : 0) + .06 * State.Rank("leech") + .15 * State.Rank("vampire") + (State.Synergy(Family.Blood, 2) ? .03 : 0)));
        if (State.Rank("double") > 0 && State.Hits % 3 == 0) Proc(hero, victim, dealt * 1.5, 0xF8CC68);
        if (critical && State.Synergy(Family.Assault, 4)) Proc(hero, victim, dealt * .40, 0xF8CC68);
        if (State.Rank("execute") > 0 && victim is not Boss && victim.life > 0 && victim.life < victim.maxLife * .20)
            Proc(hero, victim, victim.life * 20d, 0xFF6677);
        if (!victim.destroyed && victim.life > 0)
        {
            if (State.Rank("frost") > 0 && State.Chance(.25 * State.Rank("frost")))
                victim.setAffectS(8, .6, Ref<double>.Null, false);
            AddDot(victim, "fire", dealt * .45 * State.Rank("inferno") / 3);
            AddDot(victim, "blood", dealt * .60 * State.Rank("hemorrhage") / 3);
        }
        if (sparkLock <= 0 && State.Rank("spark") > 0 && State.Chance(.25 * State.Rank("spark")))
        {
            sparkLock = .3;
            foreach (var enemy in Enemies(hero, center: victim, radius: 10).Where(e => e.HashlinkPointer != victim.HashlinkPointer)
                .OrderBy(e => DistanceSq(e, victim)).Take(3)) Proc(hero, enemy, dealt * .60, 0x77DAFF);
        }
        if (State.Rank("overload") > 0 && State.Hits % 8 == 0)
            foreach (var enemy in Enemies(hero, true)) Proc(hero, enemy, dealt * 2d, 0xBBA0FF);
        if (State.Rank("predator") > 0 && predatorLock <= 0)
        {
            predatorLock = 2;
            foreach (var enemy in Enemies(hero, true).Where(e => e.HashlinkPointer != victim.HashlinkPointer))
                Proc(hero, enemy, dealt * .50, 0xFFB26C);
        }
    }
    private void AddDot(Mob target, string kind, double damage)
    {
        if (damage <= 0) return;
        var key = (target.HashlinkPointer, kind);
        if (dots.TryGetValue(key, out var dot)) { dot.Damage = Math.Max(dot.Damage, damage); dot.Ticks = 3; }
        else dots[key] = new Dot(target, damage);
    }
    private void Proc(Hero hero, Mob target, double damage, int color)
    {
        if (target.destroyed || target.life <= 0 || !hero.isOpponent(target) || hero.life <= 0 ||
            target._level?.HashlinkPointer != hero._level.HashlinkPointer) return;
        damage *= State.Synergy(Family.Arcane, 2) ? 1.3 : 1;
        damage *= (State.IsRoute(Family.Arcane) ? 1.25 : 1);
        damage = Math.Clamp(damage, 0, 100_000_000);
        if (damage < 1) return;
        var attack = new AttackData().init(hero, (object)damage);
        attack.useHeroScaling = false;
        // false 选择原版怪物等级公式，而非“禁用缩放”；固定1级，避免对实际伤害再次按玩家属性缩放。
        attack.sourceTier = 1;
        attack.dmgMultiplier = 1;
        int before = target.life;
        procDepth++;
        try
        {
            target.colorBlink(color, .7, .12);
            AttackUtils.Class.hit.Invoke(attack, target);
            State.BonusDamage += Math.Max(0, before - Math.Max(0, target.life));
        }
        finally { procDepth--; }
    }
    private void OnKill(Hook_Hero.orig_onMobDeath orig, Hero hero, Mob mob)
    {
        // orig 保留原版掉落、击杀计数、诅咒解锁等。只计算属于玩家的敌对击杀。
        bool valid = mob != null && hero.isOpponent(mob) && EnsureRun(hero);
        orig(hero, mob);
        if (!valid || State.Choices == 0 && State.Route.Length == 0) return;
        State.Kills++;
        int extraShards = State.Rank("bounty") + State.Rank("scavenge") + (State.Synergy(Family.Fortune, 2) ? 1 : 0);
        State.Shards += 1 + extraShards + (mob.elite ? 4 : 0) + (mob is Boss ? 24 : 0);
        if (mob is Boss) { State.Bosses++; State.Rerolls++; }
        int money = (State.IsRoute(Family.Fortune) ? 5 : 0) + 20 * State.Rank("bounty") + (State.Synergy(Family.Fortune, 4) && State.Kills % 5 == 0 ? 50 : 0);
        if (money > 0) hero.addMoney(money, Ref<bool>.In(false));
        Heal(hero, hero.maxLife * (.03 * State.Rank("feast") + .05 * State.Rank("vampire")));
        double cooldown = .7 * State.Rank("recycle") + (State.Synergy(Family.Hunt, 4) ? 1 : 0);
        if (cooldown > 0) hero.activeSkillsManager.reduceAllActiveCooldowns(cooldown);
        if (State.Rank("frenzy") > 0) frenzyTime = 4;
        if (State.Rank("jackpot") > 0 && (mob.elite || mob is Boss)) { State.Pending++; deferred = false; openDelay = .4; }
        // 击杀爆炸仅由普通攻击触发；附加伤害可获击杀收益，但不会再次触发攻击效果。
        if (procDepth == 0 && State.Synergy(Family.Blood, 4))
            foreach (var enemy in Enemies(hero, center: mob, radius: 5)) Proc(hero, enemy, 80 * State.Growth * DamageTradeoff, 0xFF627E);
    }
    private void OnDie(Hook_Hero.orig_onDie orig, Hero hero)
    {
        if (EnsureRun(hero) && State.Rank("rebirth") > 0 && hero.curseCounter <= 0 &&
            State.RebirthMaps.Add(hero._level.map.id.ToString()))
        {
            hero.life = Math.Max(1, (int)(hero.maxLife * .35));
            hero.setAffectS(5, 2, Ref<double>.Null, true);
            sinceHurt = 0;
            Logger.Information("[HextechBrawl] Rebirth used in {Level}", hero._level.map.id);
            return;
        }
        Close();
        orig(hero);
    }
    private void Heal(Hero hero, double amount)
    {
        if (hero.destroyed || hero.life <= 0 || amount <= 0) return;
        int value = (int)Math.Clamp(amount, 0, Math.Max(0, hero.maxLife - hero.life));
        if (value <= 0) return;
        int before = hero.life;
        hero.heal(value);
        State.Healing += Math.Max(0, hero.life - before);
    }
    private void TickCombat(Hero hero, double dt)
    {
        if (State.Choices == 0 && State.Route.Length == 0) return;
        frenzyTime = Math.Max(0, frenzyTime - dt); sinceHurt += dt;
        sparkLock -= dt; predatorLock -= dt; novaTime -= dt;
        if (State.Rank("echo") > 0) hero.activeSkillsManager.reduceAllActiveCooldowns(dt);
        if (State.Rank("bulwark") > 0 && !shieldReady)
        {
            shieldTime -= dt;
            if (shieldTime <= 0) { shieldReady = true; hero.colorBlink(0x76DFFF, .7, .3); }
        }
        double regen = (sinceHurt >= 3 ? .015 * State.Rank("regen") : 0) + (State.Synergy(Family.Fortress, 4) ? .01 : 0);
        healAccumulator += hero.maxLife * regen * dt;
        if (healAccumulator >= 1) { Heal(hero, healAccumulator); healAccumulator -= Math.Floor(healAccumulator); }
        if (novaTime <= 0)
        {
            novaTime = State.Synergy(Family.Arcane, 4) ? 2.5 : 5;
            if (State.Rank("nova") > 0)
            {
                hero._level.fx.electricArea(hero, 6 * 24, 0x76CEFF, 0xA8A0FF);
                foreach (var enemy in Enemies(hero, center: hero, radius: 6))
                    Proc(hero, enemy, 60 * State.CoreLevel * State.Rank("nova") * State.Growth * DamageTradeoff, 0x76CEFF);
            }
        }
        foreach (var key in dots.Keys.ToArray())
        {
            if (!dots.TryGetValue(key, out var dot)) continue;
            if (dot.Target.destroyed || dot.Target.life <= 0 || dot.Target._level?.HashlinkPointer != hero._level.HashlinkPointer)
            { dots.Remove(key); continue; }
            dot.Timer -= dt;
            if (dot.Timer > 0) continue;
            dot.Timer += 1;
            Proc(hero, dot.Target, dot.Damage, key.Item2 == "fire" ? 0xFFAF55 : 0xFF5577);
            if (--dot.Ticks <= 0) dots.Remove(key);
        }
    }
}
