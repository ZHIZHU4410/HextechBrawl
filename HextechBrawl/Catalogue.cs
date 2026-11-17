namespace HextechBrawl;

public enum Family { Assault, Fortress, Arcane, Blood, Hunt, Fortune }
public sealed record Augment(string Id, string Name, int Rarity, Family Family, string Description, int MaxRank = 3, int GoldCost = 0, int CellsCost = 0, int CurseCost = 0, int WeaponLevels = 0, int AllStats = 0);

/// <summary>说明中的数值均为每级收益。羁绊按不同强化种类计数，升级同一强化不重复计数。</summary>
public static class Catalogue
{
    public static readonly HashSet<string> MechanicIds = new()
    {
        "double", "execute", "thorns", "bulwark", "rebirth", "spark", "frost", "nova", "inferno",
        "overload", "echo", "leech", "hemorrhage", "vampire", "rhythm", "predator", "phase_wall", "multishot"
    };
    public static readonly string[] Families = ["征服", "壁垒", "奥术", "血契", "猎杀", "财富"];
    public static readonly string[] Rarities = ["", "白银", "黄金", "棱彩"];
    public static readonly string[] Synergies = [
        "2：伤害+25% / 4：暴击追加40%伤害",
        "2：减伤10% / 4：生命+50%、每秒恢复1%",
        "2：附加攻击伤害+30% / 4：奥术脉冲间隔减半",
        "2：吸血+3% / 4：击杀产生80伤害血爆",
        "2：攻速+20% / 4：击杀减少技能冷却1秒",
        "2：击杀碎片+1 / 4：高品质概率+15%、每5杀+50金"
    ];
    public static readonly Augment[] All = [
        new("might", "蛮力生长", 1, Family.Assault, "所有伤害 +30%，攻击速度 -10%。"),
        new("focus", "完美状态", 1, Family.Assault, "生命高于90%时，伤害 +45%。"),
        new("berserk", "狂战之心", 2, Family.Assault, "生命低于50%时，伤害 +75%。"),
        new("slayer", "巨人杀手", 2, Family.Assault, "对精英与首领伤害 +60%。"),
        new("double", "双重打击", 3, Family.Assault, "每第3次有效命中，追加150%伤害。", 1),
        new("execute", "终结协议", 3, Family.Assault, "斩杀低于20%生命的普通敌人；\n对低于30%生命的首领伤害翻倍。", 1),
        new("vitality", "巨像血统", 1, Family.Fortress, "最大生命 +35%，所有伤害 -10%。"),
        new("armor", "合金装甲", 1, Family.Fortress, "受到攻击伤害 -15%，攻速 -8%。总减伤上限75%。"),
        new("regen", "修复模块", 2, Family.Fortress, "3秒未受伤后，每秒恢复最大生命的1.5%。"),
        new("thorns", "荆棘反应炉", 2, Family.Fortress, "受敌人伤害后，反击其实际伤害的200%。"),
        new("bulwark", "绝对防线", 3, Family.Fortress, "每8秒获得一次护盾，将下一次\n敌人的攻击伤害降低90%。", 1),
        new("rebirth", "不灭核心", 3, Family.Fortress, "每张地图一次：致命伤害后恢复35%生命。\n诅咒死亡无效。", 1),
        new("spark", "连锁闪电", 1, Family.Arcane, "命中有25%概率向附近3个敌人传递\n60%伤害的闪电。触发间隔0.3秒。"),
        new("frost", "冰霜回路", 1, Family.Arcane, "有效命中有25%概率眩晕敌人0.6秒。\n服从原版抗性。"),
        new("nova", "奥术脉冲", 2, Family.Arcane, "每5秒对周围6格敌人造成\n60×核心等级的基础伤害。"),
        new("inferno", "熔火引擎", 2, Family.Arcane, "命中追加45%伤害，在3秒内结算。\n同种持续伤害取最高值并刷新。"),
        new("overload", "过载风暴", 3, Family.Arcane, "每第8次有效命中，以该次伤害的200%\n轰击屏幕内所有敌人。", 1),
        new("echo", "时间裂隙", 3, Family.Arcane, "所有技能冷却流速提高100%，最大生命 -20%。", 1),
        new("leech", "生命汲取", 1, Family.Blood, "有效命中吸血6%，所有伤害 -8%。"),
        new("feast", "鲜血盛宴", 1, Family.Blood, "击杀回复最大生命的3%。"),
        new("hemorrhage", "血流不止", 2, Family.Blood, "命中追加60%伤害，在3秒内结算。\n同种持续伤害取最高值并刷新。"),
        new("pact", "猩红契约", 2, Family.Blood, "伤害 +100%，最大生命 -20%。", 2),
        new("vampire", "鲜血领主", 3, Family.Blood, "吸血 +15%；每次击杀额外回复5%生命。", 1),
        new("laststand", "绝境飞升", 3, Family.Blood, "生命低于35%时，伤害 +200%。", 1),
        new("haste", "迅捷打击", 1, Family.Hunt, "武器攻击速度 +25%，所有伤害 -15%。"),
        new("hunter", "先发制人", 1, Family.Hunt, "对生命高于90%的敌人伤害 +70%。"),
        new("rhythm", "致命节奏", 2, Family.Hunt, "每第4次有效命中必定暴击，\n额外提高暴击伤害35%。"),
        new("frenzy", "猎杀时刻", 2, Family.Hunt, "击杀后4秒内：伤害 +60%，攻速 +40%。"),
        new("momentum", "无限成长", 2, Family.Hunt, "本局每10次击杀，永久增加5%伤害。"),
        new("predator", "万物皆猎物", 3, Family.Hunt, "命中向屏幕内其他敌人追加50%伤害。\n触发间隔2秒。", 1),
        new("bounty", "赏金猎人", 1, Family.Fortune, "每次击杀额外获得20金币、1核心碎片。"),
        new("scavenge", "拾荒大师", 1, Family.Fortune, "每次击杀额外获得1核心碎片；\n黄金/棱彩出现概率 +5%。"),
        new("recycle", "灵魂回收", 2, Family.Fortune, "每次击杀减少所有技能冷却0.7秒。"),
        new("investment", "复利投资", 2, Family.Fortune, "每持有1000金币，伤害 +12%。\n每级最多增加240%。"),
        new("lucky", "幸运暴击", 2, Family.Fortune, "有效命中有20%概率变为暴击，\n暴击伤害额外 +20%。"),
        new("jackpot", "棱彩奖池", 3, Family.Fortune, "每击杀一个精英或首领，\n额外获得一次海克斯三选一。", 1)
    ];
    public static readonly Augment[] Endless = [
        new("end_power", "无尽·力量", 2, Family.Assault, "所有伤害 +25%。可无限升级。", int.MaxValue),
        new("end_life", "无尽·生命", 2, Family.Fortress, "最大生命 +20%。可无限升级。", int.MaxValue),
        new("end_haste", "无尽·迅捷", 2, Family.Hunt, "攻击速度 +15%。可无限升级。", int.MaxValue)
    ];
    public static readonly Augment[] Routes = [
        new("route_assault", "兵器大师", 2, Family.Assault, "本局偏向征服强化。\n武器伤害+15%，受到攻击伤害+10%。", 1),
        new("route_fortress", "壁垒守卫", 2, Family.Fortress, "本局偏向壁垒强化。\n最大生命+20%，攻击速度-10%。", 1),
        new("route_arcane", "奥术工程师", 2, Family.Arcane, "本局偏向奥术强化。\n附加攻击伤害+25%，武器伤害-10%。", 1),
        new("route_blood", "猩红赌徒", 2, Family.Blood, "本局偏向血契强化。\n吸血+2%，最大生命-15%。", 1),
        new("route_hunt", "疾风猎手", 2, Family.Hunt, "本局偏向猎杀强化。\n攻击速度+15%，所有伤害-10%。", 1),
        new("route_fortune", "贪婪商人", 2, Family.Fortune, "本局偏向财富强化。\n每杀额外5金币，所有伤害-10%。", 1)
    ];
    public static readonly Augment[] Contracts = [
        new("forge_gold", "金币精锻", 1, Family.Assault, "共享锻造提升至+1档；红紫绿各+1。\n支付300金币。本局每档只提升一次。", 1, GoldCost:300, WeaponLevels:1, AllStats:1),
        new("forge_cells", "细胞熔铸", 2, Family.Arcane, "共享锻造提升至+2档；红紫绿各+1。\n支付15细胞。本局每档只提升一次。", 1, CellsCost:15, WeaponLevels:2, AllStats:1),
        new("forge_curse", "诅咒锻造", 3, Family.Blood, "共享锻造提升至+3档；红紫绿各+1。\n增加10诅咒；受伤可能立即死亡。", 1, CurseCost:10, WeaponLevels:3, AllStats:1),
        new("gold_loan", "染血借贷", 1, Family.Fortune, "获得1000金币。\n代价：增加12原版诅咒。", 3, CurseCost:12),
        new("soul_debt", "灵魂透支", 2, Family.Fortune, "获得30细胞。\n代价：本局所有治疗每级降低25%。", 2),
        new("purify", "赎罪契约", 1, Family.Fortress, "减少10原版诅咒，支付600金币。\n代价：本局所有伤害每级降低5%。", 3, GoldCost:600),
        new("cell_ritual", "细胞献祭", 2, Family.Blood, "红紫绿各+2，支付25细胞。\n代价：本局最大生命每级降低10%。", 3, CellsCost:25, AllStats:2),
        new("curse_power", "背负厄运", 2, Family.Assault, "所有伤害每级+40%。\n立即增加15原版诅咒；谨慎承受伤害。", 3, CurseCost:15)
    ];
    public static readonly Augment[] PoolChoices = [
        new("color_red", "暴虐 · 红色卷轴", 2, Family.Assault, "武器须包含红色标签。\n允许红紫、红绿；排除纯紫、纯绿。", 1),
        new("color_purple", "战术 · 紫色卷轴", 2, Family.Arcane, "武器须包含紫色标签。\n允许紫红、紫绿；排除纯红、纯绿。", 1),
        new("color_green", "生存 · 绿色卷轴", 2, Family.Fortress, "武器须包含绿色标签。\n允许绿红、绿紫；排除纯红、纯紫。", 1),
        new("pool_melee", "锁定近战池", 2, Family.Assault, "本局仅提供近战和颜色匹配的盾牌。\n排除远程；盾牌不单独分类。", 1),
        new("pool_ranged", "锁定远程池", 2, Family.Hunt, "本局仅提供远程和颜色匹配的盾牌。\n排除近战；盾牌不单独分类。", 1)
    ];
    public static readonly Augment[] Abilities = [
        new("phase_wall", "相位穿墙", 2, Family.Arcane, "绑定近战命中与绑定弹丸无视墙体。\n保持原版攻击范围；伤害降低10%。", 1),
        new("multishot", "散射弹幕", 2, Family.Hunt, "兼容的绑定远程一次发射3组。\n每发伤害为45%，额外弹丸不可回收。", 1)
    ];
    public static readonly Dictionary<string, Augment> ById = All.Concat(Endless).Concat(Routes).Concat(Contracts).Concat(PoolChoices).Concat(Abilities).ToDictionary(x => x.Id);
}
