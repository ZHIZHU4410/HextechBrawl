namespace HextechBrawl;

/// <summary>仅存储可序列化的数据；不保存 Hashlink 对象、地址或委托。</summary>
public sealed record BoundPreview(int Id, int Level, int Quality, int Stacks);
public sealed class RunState
{
    public int Version { get; set; } = 4;
    public List<string> RecentOffers { get; set; } = new();
    public List<string> RecentChoices { get; set; } = new();
    public int LastForgeChoice { get; set; } = -100;
    public int SharedForgeTier { get; set; }
    public int ArsenalLevelFloor { get; set; } = 1;
    public int ArsenalQuality { get; set; }
    public void RefreshArsenal(int strongestLevel, int strongestQuality)
    {
        // 旧局曾经付出的锻造投入保留成共享进度，不再只惠及旧实例。
        foreach (var a in Catalogue.Contracts.Where(a => a.WeaponLevels > 0 && Rank(a.Id) > 0))
            SharedForgeTier = Math.Max(SharedForgeTier, a.WeaponLevels);
        SharedForgeTier = Math.Clamp(SharedForgeTier, 0, 3);
        ArsenalLevelFloor = Math.Clamp(Math.Max(ArsenalLevelFloor, strongestLevel), 1, 30);
        ArsenalQuality = Math.Clamp(Math.Max(ArsenalQuality, strongestQuality), 0, 3);
    }
    public int RewardItemLevel(int proposedTier = 0) => Math.Min(30, Math.Max(ArsenalLevelFloor,
        OfferedBaseLevel + Math.Max(SharedForgeTier, proposedTier)));
    public string ChoiceError(Augment a, int gold, int cells, int curse)
        => a.WeaponLevels > 0 && a.WeaponLevels <= SharedForgeTier ? "共享锻造已达到该档位，不能重复升级" :
           Rank(a.Id) >= a.MaxRank ? "该海克斯已满级" : SelectionError(a, gold, cells, curse, true, RewardItemLevel(a.WeaponLevels));
    public string WeaponColor { get; set; } = "";
    public string WeaponCategory { get; set; } = "";
    public string PreferredWeaponKind { get; set; } = "";
    public List<string> CarriedWeaponKinds { get; set; } = new();
    public Dictionary<string, BoundPreview> CarriedBindings { get; set; } = new();
    public int ReplacementQuality { get; set; }
    public double DisplayDamagePenalty { get; set; } = 1;
    public List<string> OfferWeapons { get; set; } = new();
    public Dictionary<int, int> BoundWeapons { get; set; } = new();
    public int BindingStacks(int permanentId) => BoundWeapons.ContainsKey(permanentId) ? Choices : 0;
    public void BindWeapon(int permanentId) => BoundWeapons.TryAdd(permanentId, Choices);
    public string PoolLabel => (WeaponColor == "Brutality" ? "红" : WeaponColor == "Tactic" ? "紫" : WeaponColor == "Survival" ? "绿" : "待选颜色") + "/" +
        (WeaponCategory == "melee" ? "近战+盾" : WeaponCategory == "ranged" ? "远程+盾" : "待选分类");
    public void EnsureWeaponOffers()
    {
        if (OpeningPending || Offers.Count != 3 || OfferWeapons.Count == 3) return;
        var pool = WeaponPool.All.Where(w => WeaponPool.Allows(w, WeaponColor, WeaponCategory)).ToList();
        if (pool.Count == 0) throw new InvalidOperationException("武器池为空");
        OfferWeapons = new() { "", "", "" };
        foreach (int i in Enumerable.Range(0, 3).OrderBy(i => Offers[i] == "multishot" ? 0 : 1))
        {
            var matching = Offers[i] == "multishot" ? pool.Where(w => w.Group == 5 && VolleyCompatibility.Kinds.Contains(w.Id)).ToList() : pool;
            var available = matching.Where(w => !OfferWeapons.Contains(w.Id)).ToList();
            if (available.Count == 0) available = matching;
            OfferWeapons[i] = available[(int)(Next() % (uint)available.Count)].Id;
        }
        // 新生成的候选至少两项不是当前携带种类；不足两种新类型时按实际池回退。
        var heldKinds = CarriedWeaponKinds.Concat(CarriedBindings.Keys).ToHashSet();
        int NovelCount() => OfferWeapons.Count(id => !heldKinds.Contains(id));
        for (int i = 0; i < OfferWeapons.Count && NovelCount() < 2; i++)
        {
            if (!heldKinds.Contains(OfferWeapons[i])) continue;
            var alternatives = pool.Where(w => !heldKinds.Contains(w.Id) && !OfferWeapons.Contains(w.Id) &&
                (Offers[i] != "multishot" || w.Group == 5 && VolleyCompatibility.Kinds.Contains(w.Id))).ToList();
            if (alternatives.Count > 0) OfferWeapons[i] = alternatives[(int)(Next() % (uint)alternatives.Count)].Id;
        }
    }
    public bool OpeningGranted { get; set; }
    public bool OpeningPending { get; set; }
    public string Route { get; set; } = "";
    public List<string> ParkedOffers { get; set; } = new();
    public int ParkedRarity { get; set; }
    public int ParkedRerolls { get; set; }
    public int TargetWeaponId { get; set; } = -1;
    public int TargetWeaponLevel { get; set; }
    public string TargetWeaponLabel { get; set; } = "无武器：请先装备";
    public int OfferedBaseLevel { get; set; } = 1;
    public int Gold { get; set; }
    public int Cells { get; set; }
    public int Curse { get; set; }
    public string Wallet { get; set; } = "";
    public string Notice { get; set; } = "";
    public int RunId { get; set; } = -1;
    public int Seed { get; set; }
    public uint RandomState { get; set; } = 1;
    public Dictionary<string, int> Ranks { get; set; } = new();
    public HashSet<int> ClaimedItems { get; set; } = new();
    public HashSet<string> RebirthMaps { get; set; } = new();
    public List<string> Offers { get; set; } = new();
    public int OfferRarity { get; set; }
    public int OfferRerolls { get; set; }
    public int Pending { get; set; }
    public int Scrolls { get; set; }
    public int Choices { get; set; }
    public int Kills { get; set; }
    public int Bosses { get; set; }
    public int Shards { get; set; }
    public int Rerolls { get; set; } = 3;
    public int CoreUpgrades { get; set; }
    public int Hits { get; set; }
    public long BonusDamage { get; set; }
    public long Healing { get; set; }
    public int CoreLevel => 1 + Choices / 4 + CoreUpgrades;
    public int UpgradeCost => 25 + CoreUpgrades * 20;
    public int Rank(string id) => Ranks.GetValueOrDefault(id);
    public int FamilyCount(Family family) => Ranks.Count(x => x.Value > 0 && Catalogue.ById.TryGetValue(x.Key, out var a) && a.Family == family);
    public bool Synergy(Family family, int count) => FamilyCount(family) >= count;
    public double Growth => 1 + .04 * (CoreLevel - 1);
    public double LifeGrowth => 1 + .03 * (CoreLevel - 1);

    public static string SelectionError(Augment a, int gold, int cells, int curse, bool weapon, int level)
        => gold < a.GoldCost ? $"金币不足，需要 {a.GoldCost}" : cells < a.CellsCost ? $"细胞不足，需要 {a.CellsCost}" :
           a.WeaponLevels > 0 && !weapon ? "请按W选择一把携带武器" :
           a.WeaponLevels > 0 && level > 30 ? "共享武器等级超过上限" :
           a.Id == "purify" && curse <= 0 ? "没有诅咒，不需要赎罪" : "";
    public bool IsRoute(Family family) => Route == "route_" + family.ToString().ToLowerInvariant();
    public void GrantOpening()
    {
        if (OpeningGranted && (OpeningPending || WeaponCategory.Length > 0)) return;
        bool existing = OpeningPending;
        OpeningGranted = OpeningPending = true;
        ParkedOffers = Offers.ToList(); ParkedRarity = OfferRarity; ParkedRerolls = OfferRerolls;
        Offers.Clear(); OfferRarity = OfferRerolls = 0; OfferWeapons.Clear(); if (!existing) Pending++;
    }

    // 独立于游戏随机数：退出读档保留同一组选项，不能靠重进免费重随。
    public uint Next()
    {
        uint x = RandomState == 0 ? 0x9E3779B9u : RandomState;
        x ^= x << 13; x ^= x >> 17; x ^= x << 5;
        return RandomState = x;
    }
    public bool Chance(double chance) => Next() / (double)uint.MaxValue < Math.Clamp(chance, 0, 1);
    public bool Claim(int permanentId, bool scroll = true)
    {
        if (!ClaimedItems.Add(permanentId)) return false;
        Pending++;
        if (scroll) Scrolls++;
        return true;
    }
    public void EnsureOffers()
    {
        if (Pending <= 0) return;
        if (!OpeningPending && Offers.Count == 3) { EnsureWeaponOffers(); return; }
        if (OpeningPending && Offers.Count > 0 && Offers.All(x => x.StartsWith("color_") || x.StartsWith("pool_"))) return;
        if (OpeningPending)
        {
            Offers = WeaponColor.Length == 0 ? new() { "color_red", "color_purple", "color_green" } : new() { "pool_melee", "pool_ranged" };
            OfferRarity = 2; return;
        }
        int next = Choices + 1;
        if (OfferRarity == 0)
        {
            double luck = .05 * Rank("scavenge") + (Synergy(Family.Fortune, 4) ? .15 : 0);
            OfferRarity = next % 5 == 0 ? 3 : Chance(.12 + luck * .5) ? 3 :
                next % 3 == 0 || Chance(.40 + luck) ? 2 : 1;
        }
        FillOffers([]);
        EnsureWeaponOffers();
    }
    private void FillOffers(HashSet<string> avoid)
    {
        var eligible = Catalogue.All.Concat(Catalogue.Abilities.Where(a => a.Id != "multishot" || WeaponCategory == "ranged"))
            .Concat(Catalogue.Contracts.Where(a => a.WeaponLevels == 0)).Where(a => Rank(a.Id) < a.MaxRank).ToList();
        var recent = RecentOffers.Concat(RecentChoices).Concat(avoid).ToHashSet();
        Offers.Clear(); OfferWeapons.Clear();
        Augment Pick(List<Augment> candidates)
        {
            var fresh = candidates.Where(a => !recent.Contains(a.Id)).ToList();
            if (fresh.Count == 0) fresh = candidates.Where(a => !RecentChoices.Contains(a.Id)).ToList();
            if (fresh.Count == 0) fresh = candidates;
            var tier = fresh.Where(a => a.Rarity == OfferRarity).ToList();
            if (tier.Count > 0) fresh = tier;
            var families = Offers.Select(id => Catalogue.ById[id].Family).ToHashSet();
            var different = fresh.Where(a => !families.Contains(a.Family)).ToList();
            if (different.Count > 0) fresh = different;
            if (Chance(.65))
            {
                var unowned = fresh.Where(a => Rank(a.Id) == 0).ToList();
                if (unowned.Count > 0) fresh = unowned;
            }
            if (Chance(.65))
            {
                var aligned = fresh.Where(a => IsRoute(a.Family)).ToList();
                if (aligned.Count > 0) fresh = aligned;
            }
            return fresh[(int)(Next() % (uint)fresh.Count)];
        }
        // 锻造仅25%尝试加入且最多一张，选过锻造后至少两次普通选择不再出现。
        if (Choices - LastForgeChoice >= 2 && Chance(.25))
        {
            var forges = Catalogue.Contracts.Where(a => a.WeaponLevels > SharedForgeTier && Rank(a.Id) < a.MaxRank).ToList();
            if (forges.Count > 0) Offers.Add(Pick(forges).Id);
        }
        while (Offers.Count < 3)
        {
            var candidates = eligible.Where(a => !Offers.Contains(a.Id)).ToList();
            if (candidates.Count == 0) candidates = Catalogue.Endless.Where(a => !Offers.Contains(a.Id)).ToList();
            // 机制池仍有候选时，每手至少一项真实战斗机制，而非三项平铺数值/卷轴。
            if (!Offers.Any(id => Catalogue.MechanicIds.Contains(id)))
            {
                var mechanics = candidates.Where(a => Catalogue.MechanicIds.Contains(a.Id)).ToList();
                if (mechanics.Count > 0) candidates = mechanics;
            }
            Offers.Add(Pick(candidates).Id);
        }
        EnsureSafeOffer();
        // 培养武器与海克斯效果独立；洗牌避免任何列成为固定锻造/升级位。
        for (int i = Offers.Count - 1; i > 0; i--)
        {
            int j = (int)(Next() % (uint)(i + 1));
            (Offers[i], Offers[j]) = (Offers[j], Offers[i]);
        }
        RecentOffers = RecentOffers.Concat(Offers).TakeLast(6).ToList();
    }
    public void EnsureSafeOffer()
    {
        static bool NoEntryCost(Augment a) => a.GoldCost == 0 && a.CellsCost == 0 && a.CurseCost == 0;
        if (Offers.Any(id => NoEntryCost(Catalogue.ById[id]))) return;
        var safe = Catalogue.All.Concat(Catalogue.Endless).Where(a => NoEntryCost(a) && Rank(a.Id) < a.MaxRank && !Offers.Contains(a.Id)).ToList();
        if (safe.Count > 0) Offers[^1] = safe[(int)(Next() % (uint)safe.Count)].Id;
    }
    public bool Reroll()
    {
        if (OpeningPending || Pending <= 0 || Rerolls <= 0 || Offers.Count != 3 || OfferRerolls >= 3) return false;
        Rerolls--; OfferRerolls++;
        FillOffers(Offers.ToHashSet());
        EnsureWeaponOffers();
        return true;
    }
    public bool Choose(int index)
    {
        if (Pending <= 0 || index < 0 || index >= Offers.Count) return false;
        var id = Offers[index];
        if (!Catalogue.ById.TryGetValue(id, out var a) || Rank(id) >= a.MaxRank || a.WeaponLevels > 0 && a.WeaponLevels <= SharedForgeTier) return false;
        if (OpeningPending)
        {
            if (id.StartsWith("color_"))
            {
                WeaponColor = id == "color_red" ? "Brutality" : id == "color_purple" ? "Tactic" : "Survival";
                Offers.Clear(); return true;
            }
            if (!id.StartsWith("pool_") || WeaponColor.Length == 0) return false;
            WeaponCategory = id == "pool_melee" ? "melee" : "ranged";
            Route = WeaponColor == "Brutality" ? "route_assault" : WeaponColor == "Tactic" ? "route_arcane" : "route_fortress";
            OpeningPending = false; Pending--;
            Offers = ParkedOffers; OfferRarity = ParkedRarity; OfferRerolls = ParkedRerolls;
            ParkedOffers = new(); Notice = ""; return true;
        }
        if (a.WeaponLevels > 0) { LastForgeChoice = Choices + 1; SharedForgeTier = a.WeaponLevels; }
        RecentChoices = RecentChoices.Append(id).TakeLast(2).ToList();
        Ranks[id] = Rank(id) + 1;
        Choices++; Pending--; Offers.Clear(); OfferWeapons.Clear(); OfferRarity = OfferRerolls = 0;
        if (Choices % 4 == 0) { Rerolls++; Shards += 10; }
        return true;
    }
    public bool Upgrade()
    {
        if (Shards < UpgradeCost || CoreUpgrades >= 20) return false;
        Shards -= UpgradeCost; CoreUpgrades++; Rerolls++;
        return true;
    }
}
