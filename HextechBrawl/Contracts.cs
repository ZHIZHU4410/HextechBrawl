using dc.en;
using dc.tool;
using HaxeProxy.Runtime;
using ModCore.Utilities;
using Math = System.Math;

namespace HextechBrawl;

public sealed partial class Main
{
    private List<InventItem> CarriedWeapons(Hero hero)
    {
        var items = new[] { hero.inventory.getEquippedWeaponOn(0), hero.inventory.getEquippedWeaponOn(1), hero.inventory.getBackpackWeapon() };
        return items.Where(i => i != null && i.kind is InventItemKind.Weapon).DistinctBy(i => i.permanentId).ToList();
    }
    private void RefreshWallet(Hero hero)
    {
        var weapons = CarriedWeapons(hero);
        var target = weapons.FirstOrDefault(i => i.permanentId == State.TargetWeaponId) ?? weapons.FirstOrDefault();
        State.TargetWeaponId = target?.permanentId ?? -1;
        State.TargetWeaponLevel = target?.getRawItemLevel() ?? 0;
        State.TargetWeaponLabel = target == null ? "无武器：请先装备" :
            (target._itemData?.name == null ? "武器" : dc.Lang.Class.t.get(target._itemData.name, null).ToString()) + " Lv." + target.getRawItemLevel();
        int nativeLevel = hero._level.game.isBossRush() ? hero._level.game.bossRush.getItemLevel() : hero._level.map.lootLevel;
        State.OfferedBaseLevel = BalanceRules.RewardLevel(nativeLevel);
        State.CarriedWeaponKinds = weapons.Select(w => w._itemData.id.ToString()).Distinct().ToList();
        State.CarriedBindings = weapons.Where(w => Bound(w)).GroupBy(w => w._itemData.id.ToString()).ToDictionary(g => g.Key,
            g => { var w = g.OrderByDescending(i => i.permanentId == State.TargetWeaponId).ThenByDescending(i => State.BindingStacks(i.permanentId)).First();
                return new BoundPreview(w.permanentId, w.getRawItemLevel(), w.getUpgradeLevel(), State.BindingStacks(w.permanentId)); });
        State.PreferredWeaponKind = target != null && Bound(target) ? target._itemData.id.ToString() : State.CarriedBindings.Keys.FirstOrDefault() ?? "";
        State.RefreshArsenal(State.CarriedBindings.Values.Select(v => v.Level).DefaultIfEmpty(1).Max(),
            State.CarriedBindings.Values.Select(v => v.Quality).DefaultIfEmpty(0).Max());
        State.ReplacementQuality = Math.Max(State.ArsenalQuality, Math.Clamp(target?.getUpgradeLevel() ?? 0, 0, 3));
        State.DisplayDamagePenalty = DamageTradeoff;
        State.Gold = hero._level.game.data.money; State.Cells = hero.cells; State.Curse = hero.curseCounter;
        State.Wallet = $"金币 {hero._level.game.data.money}   细胞 {hero.cells}   诅咒 {hero.curseCounter}";
    }
    private void CycleWeapon()
    {
        if (view == null || view.Selecting || State.OpeningPending) return;
        var weapons = CarriedWeapons(pausedGame.hero);
        if (weapons.Count == 0) return;
        int index = weapons.FindIndex(i => i.permanentId == State.TargetWeaponId);
        State.TargetWeaponId = weapons[(index + 1) % weapons.Count].permanentId;
        State.Notice = ""; RefreshWallet(pausedGame.hero); Redraw();
    }
    private bool ApplySelection(int index, Hero hero)
    {
        if (index < 0 || index >= State.Offers.Count) return false;
        var a = Catalogue.ById[State.Offers[index]];
        var target = CarriedWeapons(hero).FirstOrDefault(i => i.permanentId == State.TargetWeaponId);
        bool reward = !State.OpeningPending;
        State.EnsureWeaponOffers();
        string weaponId = reward && index < State.OfferWeapons.Count ? State.OfferWeapons[index] : "";
        var entry = WeaponPool.Get(weaponId);
        int baseLevel = State.RewardItemLevel(a.WeaponLevels);
        if (reward && (entry == null || !WeaponPool.Allows(entry, State.WeaponColor, State.WeaponCategory)))
        { State.Notice = "目标武器不符合本局池，请重随。"; view.Reject(); Redraw(); return false; }
        string error = State.ChoiceError(a, hero._level.game.data.money, hero.cells, hero.curseCounter);
        if (error.Length > 0)
        {
            State.Notice = error + "；未扣费，可选其他卡或重随。";
            view.Reject(); Redraw(); return false;
        }
        // 先准备属性物品、确认目标/余额，再结算。直接入库存，避免再次进入卷轴奖励Hook。
        var stats = new List<InventItem>();
        if (reward)
        {
            string mainStat = State.WeaponColor == "Brutality" ? "BrutalityUp" : State.WeaponColor == "Tactic" ? "TacticUp" : "SurvivalUp";
            stats.Add(InventItem.Class.fromItem.Invoke(mainStat.AsHaxeString(), null));
        }
        for (int n = 0; n < a.AllStats; n++)
            foreach (string id in new[] { "BrutalityUp", "TacticUp", "SurvivalUp" })
                stats.Add(InventItem.Class.fromItem.Invoke(id.AsHaxeString(), null));
        var acquired = reward ? InventItem.Class.fromItem.Invoke(weaponId.AsHaxeString(), baseLevel) : null;
        if (reward)
            for (int q = acquired.getUpgradeLevel(); q < State.ReplacementQuality; q++)
                acquired.addAffix("QualityUp".AsHaxeString(), Ref<bool>.In(true));
        if (!State.Choose(index)) return false;
        if (reward)
        {
            // 每次确认都生成卡上武器，不替换、不合并玩家当前装备。
            var rewardDrop = new dc.en.inter.ItemDrop(hero._level, hero.cx, hero.cy, acquired, true, Ref<bool>.In(false));
            rewardDrop.init(); rewardDrop.onDropAsLoot();
            State.BindWeapon(acquired.permanentId);
        }
        if (a.GoldCost > 0) hero.substractMoney(a.GoldCost, Ref<bool>.In(false));
        if (a.CellsCost > 0) hero.substractCells(a.CellsCost, Ref<bool>.In(false));
        if (a.CurseCost > 0) hero.curse(a.CurseCost, "海克斯契约".AsHaxeString(), Ref<bool>.In(true), Ref<bool>.In(false));
        if (reward)
        {
            // 固定共享目标等级，不以旧等级为底反复加数；换新与留旧承接相同进度。
            int sharedLevel = State.RewardItemLevel();
            foreach (var w in CarriedWeapons(hero).Where(w => Bound(w)))
            {
                w.setItemLevel(Math.Max(w.getRawItemLevel(), sharedLevel));
                for (int q = w.getUpgradeLevel(); q < State.ReplacementQuality; q++)
                    w.addAffix("QualityUp".AsHaxeString(), Ref<bool>.In(true));
            }
            State.ArsenalLevelFloor = Math.Max(State.ArsenalLevelFloor, sharedLevel);
            State.ArsenalQuality = Math.Max(State.ArsenalQuality, State.ReplacementQuality);
        }
        foreach (var stat in stats) hero.inventory.add(stat);
        if (a.Id == "gold_loan") hero.addMoney(1000, Ref<bool>.In(false));
        if (a.Id == "soul_debt") hero.addCells(30, Ref<bool>.In(false));
        if (a.Id == "purify") hero.reduceCurse(10);
        if (stats.Count > 0 || reward)
            hero.onEquipedItemsChange(Ref<bool>.In(true), Ref<bool>.In(false), Ref<bool>.In(false));
        State.Notice = "";
        return true;
    }
    private void OnHealing(Hook_Hero.orig_heal orig, Hero hero, int amount)
    {
        if (amount > 0 && EnsureRun(hero)) amount = (int)(amount * Math.Pow(.75, State.Rank("soul_debt")));
        orig(hero, amount);
    }
    private double DamageTradeoff => BalanceRules.DamagePenalty(Math.Pow(.85, State.Rank("haste")) * Math.Pow(.90, State.Rank("vitality")) *
        Math.Pow(.92, State.Rank("leech")) * Math.Pow(.95, State.Rank("purify")) *
        (State.IsRoute(Family.Hunt) || State.IsRoute(Family.Fortune) ? .90 : 1));
}
