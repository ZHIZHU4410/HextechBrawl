using HextechBrawl;
using System.Text.Json;
int checks = 0;
void Check(bool valid, string name) { checks++; if (!valid) throw new Exception(name); }
Check(WeaponPool.All.Length == 114 && WeaponPool.All.Select(w => w.Id).Distinct().Count() == 114, "native weapon catalog");
var torch = WeaponPool.Get("ThrowingTorch");
Check(WeaponPool.Allows(torch, "Brutality", "ranged") && WeaponPool.Allows(torch, "Tactic", "ranged"), "red-purple throwing torch admitted by either tag");
Check(!WeaponPool.Allows(torch, "Brutality", "melee"), "ranged excluded from melee");
foreach (var w in WeaponPool.All.Where(w => w.Color1 == "Tactic" && w.Color2 == "")) Check(!WeaponPool.Allows(w, "Brutality", "ranged") && !WeaponPool.Allows(w, "Brutality", "melee"), "pure purple excluded by red");
foreach (var shield in WeaponPool.All.Where(w => w.Group == 6)) Check(WeaponPool.Allows(shield, shield.Color1, "melee") && WeaponPool.Allows(shield, shield.Color1, "ranged"), "shields merge into both pools");
for (uint seed = 1; seed <= 100; seed++)
{
    var s = new RunState { RandomState = seed };
    s.GrantOpening(); s.GrantOpening(); s.EnsureOffers();
    Check(s.Pending == 1 && s.Offers.Count == 3 && !s.Reroll(), "one opening scroll, no reroll");
    s.Choose((int)(seed % 3)); s.EnsureOffers();
    Check(s.Pending == 1 && s.Offers.SequenceEqual(new[] { "pool_melee", "pool_ranged" }) && s.Choices == 0, "color then exactly two category pools");
    s.Choose((int)(seed % 2));
    Check(s.Pending == 0 && !s.OpeningPending && s.Choices == 0, "category locks without normal reward consumption");
    var color = s.WeaponColor; var category = s.WeaponCategory;
    for (int n = 1; n <= 100; n++)
    {
        Check(s.Claim(n) && !s.Claim(n), "scroll not duplicated"); s.EnsureOffers();
        if (category == "melee") Check(!s.Offers.Contains("multishot"), "melee excludes ranged-only ability");
        for (int c = 0; c < s.Offers.Count; c++)
            if (s.Offers[c] == "multishot") Check(VolleyCompatibility.Kinds.Contains(s.OfferWeapons[c]) && WeaponPool.Get(s.OfferWeapons[c]).Group == 5, "multishot paired only with compatible ranged weapon");
        Check(s.Offers.Any(id => { var a = Catalogue.ById[id]; return a.GoldCost == 0 && a.CellsCost == 0 && a.CurseCost == 0; }), "draft never forces currency or curse cost");
        Check(s.Offers.Count == 3 && s.OfferWeapons.Count == 3, "every hex card has weapon");
        Check(s.OfferWeapons.All(id => WeaponPool.Allows(WeaponPool.Get(id), color, category)), "strict color and exclusive category");
        Check(s.OfferWeapons.Distinct().Count() == 3, "distinct weapons per hand");
        var saved = JsonSerializer.Deserialize<RunState>(JsonSerializer.Serialize(s));
        Check(saved.OfferWeapons.SequenceEqual(s.OfferWeapons) && saved.WeaponCategory == category && saved.Next() == s.Next(), "save retains locked pool, weapon assignments and rng");
        if (n % 3 == 0 && s.Rerolls > 0) Check(s.Reroll() && s.OfferWeapons.All(id => WeaponPool.Allows(WeaponPool.Get(id), color, category)), "reroll stays in pool");
        int oldStacks = n > 1 ? s.BindingStacks(1) : 0;
        s.Choose((int)(s.Next() % 3)); s.BindWeapon(n);
        Check(s.BindingStacks(n) == 0, "new weapon begins at zero subsequent choices");
        if (n > 1) Check(s.BindingStacks(1) == oldStacks + 1, "old bound weapon grows after every choice");
        s.BindWeapon(n); Check(s.BindingStacks(n) == 0, "duplicate bind cannot reset growth");
        Check(s.WeaponColor == color && s.WeaponCategory == category, "pool remains locked");
    }
}
var old = new RunState { OpeningGranted = true, Pending = 1, Choices = 4, Offers = ["might", "focus", "armor"], OfferRarity = 1, OfferRerolls = 2 };
old.GrantOpening(); old.EnsureOffers(); old.Choose(0); old.EnsureOffers(); old.Choose(1); old.EnsureOffers();
Check(old.Pending == 1 && old.Choices == 4 && old.Offers.SequenceEqual(new[] { "might", "focus", "armor" }) && old.OfferRerolls == 2 && old.OfferWeapons.Count == 3, "v2 migration keeps pending hexes and adds weapon assignments");
Console.WriteLine($"PASS {checks:N0} checks: 100 seeds x 100 drafts; colors, category exclusivity, shields, bindings, migration and save stability.");

foreach (int native in new[] { 1, 3, 6, 10, 15, 25 }) Check(BalanceRules.RewardLevel(native) == native, "reward follows native map difficulty level");
Check(BalanceRules.RewardLevel(0) == 1 && BalanceRules.RewardLevel(99) == 27, "reward bounds");
Check(BalanceRules.BindingDamage(1000) == 2 && BalanceRules.BindingSpeed(1000) == 1.45, "binding damage and speed caps");
Check(System.Math.Abs(3 * BalanceRules.VolleyDamage - 1.35) < .00001, "shotgun has 45 percent per shot, 135 percent full hit");
var growth = new RunState { Choices = 100 }; Check(growth.Growth < 3 && growth.LifeGrowth < 2, "no exponential automatic scaling alongside native scroll stats");
Console.WriteLine("PASS additional balance and capability checks; total " + checks);

for (int layers = 0; layers <= 100; layers++)
{
    Check(BalanceRules.BoundAttackMultiplier(true, layers) == 2 * BalanceRules.BindingDamage(layers), "bound baseline exactly doubles at all layer counts");
    Check(BalanceRules.BoundAttackMultiplier(false, layers) == 1, "unbound attacks do not receive weapon boost");
}
Check(BalanceRules.DamagePenalty(.12) == .60 && BalanceRules.DamagePenalty(.85) == .85 && BalanceRules.DamagePenalty(1) == 1, "passive penalty floor preserves normal tradeoffs");
Check(BalanceRules.ReforgeLevel(3, 12, 1) == 13 && BalanceRules.ReforgeLevel(12, 3, 2) == 14 && BalanceRules.ReforgeLevel(27, 29, 3) == 30, "reforge never downgrades old level and respects cap");
foreach (string color in new[] { "Brutality", "Tactic", "Survival" })
foreach (string category in new[] { "melee", "ranged" })
{
    var preferred = WeaponPool.All.First(w => WeaponPool.Allows(w, color, category)).Id;
    var build = new RunState { WeaponColor = color, WeaponCategory = category, PreferredWeaponKind = preferred, Pending = 1, OpeningGranted = true };
    build.EnsureOffers();
    Check(build.OfferWeapons.Contains(preferred) && build.OfferWeapons.Distinct().Count() == 3, "new draft offers cultivation path without duplicate weapons");
    var original = build.OfferWeapons.ToArray(); build.PreferredWeaponKind = ""; build.EnsureWeaponOffers();
    Check(build.OfferWeapons.SequenceEqual(original), "changing selected slot cannot reroll saved candidate");
}
var costly = new RunState { Offers = ["forge_gold", "forge_cells", "gold_loan"] };
costly.EnsureSafeOffer();
Check(costly.Offers.Any(id => { var a = Catalogue.ById[id]; return a.GoldCost == 0 && a.CellsCost == 0 && a.CurseCost == 0; }), "all-cost hand repaired");
var instance = new RunState { Choices = 3 }; instance.BindWeapon(17); instance.Choices = 8; instance.BindWeapon(17);
Check(instance.BindingStacks(17) == 5, "reforging same instance preserves birth and accumulated layers");
var restoredBuild = JsonSerializer.Deserialize<RunState>(JsonSerializer.Serialize(instance));
Check(restoredBuild.BindingStacks(17) == 5, "reforged instance persistence");
Console.WriteLine("PASS optimized roguelike checks; total " + checks);
