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
        Check(s.BindingStacks(n) == s.Choices, "new weapon inherits full run growth");
        if (n > 1) Check(s.BindingStacks(1) == oldStacks + 1, "old bound weapon grows after every choice");
        s.BindWeapon(n); Check(s.BindingStacks(n) == s.Choices, "duplicate bind cannot alter shared growth");
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
foreach (string color in new[] { "Brutality", "Tactic", "Survival" })
foreach (string category in new[] { "melee", "ranged" })
{
    var carried = WeaponPool.All.Where(w => WeaponPool.Allows(w, color, category)).Take(3).ToArray();
    var build = new RunState { WeaponColor = color, WeaponCategory = category, PreferredWeaponKind = carried[0].Id, Pending = 1, OpeningGranted = true };
    foreach (var w in carried) build.CarriedBindings[w.Id] = new BoundPreview(1, 12, 3, 15);
    build.EnsureOffers();
    Check(build.OfferWeapons.Count(id => !build.CarriedBindings.ContainsKey(id)) >= 2, "draft provides at least two switch opportunities");
    Check(build.OfferWeapons.Distinct().Count() == 3, "switch candidates stay distinct");
    var original = build.OfferWeapons.ToArray(); build.PreferredWeaponKind = ""; build.EnsureWeaponOffers();
    Check(build.OfferWeapons.SequenceEqual(original), "changing selected slot cannot reroll saved candidate");
}
var costly = new RunState { Offers = ["forge_gold", "forge_cells", "gold_loan"] };
costly.EnsureSafeOffer();
Check(costly.Offers.Any(id => { var a = Catalogue.ById[id]; return a.GoldCost == 0 && a.CellsCost == 0 && a.CurseCost == 0; }), "all-cost hand repaired");
var instance = new RunState { Choices = 3 }; instance.BindWeapon(17); instance.Choices = 8; instance.BindWeapon(17);
Check(instance.BindingStacks(17) == 8, "reforging same instance preserves birth and accumulated layers");
var restoredBuild = JsonSerializer.Deserialize<RunState>(JsonSerializer.Serialize(instance));
Check(restoredBuild.BindingStacks(17) == 8, "reforged instance persistence");
Console.WriteLine("PASS optimized roguelike checks; total " + checks);


var progress = new RunState { Choices = 18, OfferedBaseLevel = 10 };
progress.BindWeapon(10);
int oldLayers = progress.BindingStacks(10);
progress.BindWeapon(20);
Check(progress.BindingStacks(20) == oldLayers && BalanceRules.BoundAttackMultiplier(true, progress.BindingStacks(20)) == BalanceRules.BoundAttackMultiplier(true, oldLayers), "switching carries exactly equal mastery multiplier");
Check(progress.BindingStacks(99) == 0, "unbound world weapon does not inherit core bonus");
progress.RefreshArsenal(14, 3);
Check(progress.RewardItemLevel() == 14 && progress.ArsenalQuality == 3, "earned level and normal quality are shared with future rewards");
progress.Pending = 1; progress.Offers = new() { "forge_gold", "might", "focus" };
Check(progress.Choose(0) && progress.SharedForgeTier == 1, "first forge raises shared tier");
int stable = progress.RewardItemLevel();
for (int i = 0; i < 20; i++)
{
    progress.Pending = 1; progress.Offers = new() { "forge_gold", "might", "focus" };
    Check(!progress.Choose(0) && progress.RewardItemLevel() == stable, "repeated same forge cannot compound level or consume choice");
}
progress.Pending = 1; progress.Offers = new() { "forge_curse", "might", "focus" };
Check(progress.Choose(0) && progress.SharedForgeTier == 3 && progress.RewardItemLevel() == 14, "forge tier is absolute, not added to old weapon level");
progress.Pending = 1; progress.Offers = new() { "forge_cells", "might", "focus" };
Check(!progress.Choose(0), "lower forge tier cannot be farmed after skipping upward");
var legacy = new RunState { Choices = 20, OfferedBaseLevel = 5, Ranks = new() { ["forge_gold"] = 8 } };
legacy.RefreshArsenal(27, 3);
Check(legacy.SharedForgeTier == 1 && legacy.RewardItemLevel() == 27, "legacy high level becomes arsenal floor without nerf or exclusive advantage");
var loaded = JsonSerializer.Deserialize<RunState>(JsonSerializer.Serialize(progress));
Check(loaded.SharedForgeTier == 3 && loaded.RewardItemLevel() == progress.RewardItemLevel() && loaded.ArsenalQuality == 3, "shared arsenal persists");
var fixedMap = new RunState { OfferedBaseLevel = 10, SharedForgeTier = 3 };
for (int turn = 0; turn < 50; turn++)
{
    fixedMap.Choices++;
    fixedMap.RefreshArsenal(fixedMap.RewardItemLevel(), 2);
    Check(fixedMap.RewardItemLevel() == 13, "repeated choices at fixed depth cannot repeatedly add forge bonus");
}
int hands = 0, switches = 0;
for (uint seed = 1; seed <= 100; seed++)
{
    var draft = new RunState { RandomState = seed, WeaponColor = "Brutality", WeaponCategory = "melee", Route = "route_assault", OpeningGranted = true };
    var seen = new HashSet<string>();
    for (int turn = 0; turn < 20; turn++)
    {
        draft.Pending++; draft.EnsureOffers(); hands++;
        Check(draft.OfferWeapons.Count(id => !draft.CarriedBindings.ContainsKey(id)) >= 2, "two actual switch choices remain available");
        Check(draft.Offers.Count(id => Catalogue.ById[id].WeaponLevels > 0) <= 1, "at most one forge");
        if (draft.SharedForgeTier == 3) Check(!draft.Offers.Any(id => Catalogue.ById[id].WeaponLevels > 0), "max shared forge disappears from pool");
        int slot = draft.OfferWeapons.FindIndex(id => !draft.CarriedBindings.ContainsKey(id));
        string kind = draft.OfferWeapons[slot];
        if (seen.Add(kind)) switches++;
        draft.Choose(slot); draft.BindWeapon(turn + 1);
        draft.CarriedBindings.Clear(); draft.CarriedBindings[kind] = new BoundPreview(turn + 1, draft.RewardItemLevel(), 2, draft.Choices);
    }
}
Console.WriteLine($"PASS shared progression and switch regression: {hands} hands, {switches} distinct weapon acquisitions across runs; total {checks}");

Check(BalanceRules.SafeFinalDamage((double)int.MaxValue * 10, 0, -1) == BalanceRules.SafeDamageCeiling, "large hero damage saturates instead of wrapping to non-positive int");
Check(BalanceRules.SafeFinalDamage(double.PositiveInfinity, 0, -1) == BalanceRules.SafeDamageCeiling, "infinite scaled damage is bounded");
Check(BalanceRules.SafeFinalDamage(double.NaN, 0, -1) == 0, "NaN cannot corrupt health math");
Check(BalanceRules.SafeFinalDamage(1e30, 0, 5000) == 5000 && BalanceRules.SafeFinalDamage(1e30, 0, 0) == 0, "boss damage cap and zero protection preserved");
Check(BalanceRules.SafeFinalDamage(12345, 12, -1) == 12357, "normal finite damage math preserved");
Check(BalanceRules.SafeFinalDamage(100, -10000, -1) == 0, "negative damage never heals enemy through int overflow");
Console.WriteLine("PASS damage conversion regression; total " + checks);
