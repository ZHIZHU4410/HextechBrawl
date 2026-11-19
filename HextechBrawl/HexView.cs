using dc.h2d;
using dc.hxd;
using HaxeProxy.Runtime;
using ModCore.Utilities;
using Node = dc.h2d.Object;
using Text = dc.h2d.Text;
using Math = System.Math;

namespace HextechBrawl;

/// <summary>全部使用原生 Heaps 对象；以1120×630为设计尺寸，按窗口等比缩放。</summary>
internal sealed class HexView : IDisposable
{
    private readonly Node root;
    private readonly Action<Action> enqueue;
    private int revision;
    private sealed class CardVisual(Node node, Node glow, Node focus, Node flash, int index, int rarity)
    {
        public Node Node = node, Glow = glow, Focus = focus, Flash = flash;
        public int Index = index, Rarity = rarity;
        public double Hover;
    }
    private sealed record Spark(Node Node, double X, double Y, double VX, double VY, double Delay);
    private readonly List<CardVisual> cards = new();
    private readonly List<Spark> sparks = new();
    private Action<int> confirmChoice;
    private string offerSignature;
    private double elapsed, selectedTime;
    private int hovered = -1, focused, selected = -1;
    private bool choiceMode, completionQueued;
    public bool Selecting => selected >= 0;
    private const int White = 0xE9F3F3, Muted = 0x9BAEBB, Cyan = 0x72DBEC, Gold = 0xF2CE7E;
    private static readonly int[] Colors = [0, 0xB6D0DC, Gold, 0xD5AAFF];
    public HexView(Action<Action> enqueue)
    {
        this.enqueue = enqueue;
        root = new Node(dc.Main.Class.ME.root.getScene());
        Resize();
    }
    public void Resize()
    {
        var scene = root.getScene();
        if (scene == null) return;
        double scale = Math.Min(scene.width / 1120d, scene.height / 630d);
        root.scaleX = root.scaleY = scale;
        root.x = (scene.width - 1120 * scale) / 2;
        root.y = (scene.height - 630 * scale) / 2;
        root.posChanged = true;
    }
    private static void Rect(Node parent, double x, double y, double w, double h, int color, double alpha = 1)
    {
        var g = new Graphics(parent);
        g.beginFill(Ref<int>.In(color), Ref<double>.In(alpha)); g.drawRect(x, y, w, h); g.endFill();
    }
    private static Text Label(Node p, string value, double x, double y, int color = White, double scale = 1, double? width = null)
    {
        var text = new Text(dc.Assets.Class.font12, p) { x = x, y = y, scaleX = scale, scaleY = scale, posChanged = true };
        if (width.HasValue) text.set_maxWidth(width.Value / scale);
        text.set_textColor(color); text.set_text(value.AsHaxeString());
        return text;
    }
    private Interactive Click(Node node, double w, double h, Action action)
    {
        int token = revision;
        var hit = new Interactive(w, h, node, null) { propagateEvents = false };
        hit.onClick = new HlAction<Event>(e =>
        {
            e.propagate = false;
            enqueue(() => { if (token == revision && !Selecting) action(); });
        });
        return hit;
    }
    private void Button(string caption, double x, double y, double w, Action action, bool active = true)
    {
        var node = new Node(root) { x = x, y = y, posChanged = true };
        Rect(node, 0, 0, w, 38, active ? 0x204453 : 0x162832);
        Label(node, caption, 12, 11, active ? White : Muted);
        if (active) Click(node, w, 38, action);
    }
    private void Background(RunState state, string subtitle)
    {
        revision++;
        choiceMode = false; selected = hovered = -1; completionQueued = false;
        elapsed = selectedTime = 0; cards.Clear(); sparks.Clear();
        root.alpha = 1;
        root.removeChildren();
        Rect(root, 0, 0, 1120, 630, 0x050D19, .98);
        var blocker = new Interactive(1120, 630, root, null) { propagateEvents = false };
        blocker.onClick = new HlAction<Event>(e => e.propagate = false);
        Rect(root, 0, 0, 1120, 4, Cyan);
        Label(root, "海克斯大乱斗", 32, 25, Gold, 1.8);
        Label(root, "DEAD CELLS  /  HEXTECH BRAWL", 34, 62, Muted, .9);
        Label(root, subtitle, 34, 96, White, 1, 1000);
        Label(root, $"核心 Lv.{state.CoreLevel}    碎片 {state.Shards}    重随 {state.Rerolls}    已选 {state.Choices}    待选 {state.Pending}", 493, 34, Cyan);
        Rect(root, 32, 126, 1056, 1, 0x29404D);
    }
    public void Choices(RunState state, int cursor, Action<int> choose, Action weapon, Action reroll, Action core, Action exit)
    {
        state.EnsureOffers();
        string signature = string.Join(",", state.Offers) + ":" + state.Choices + ":" + state.OfferRerolls + ":" + state.TargetWeaponId + ":" + state.Notice + ":" + state.Wallet + ":" + string.Join(",", state.OfferWeapons) + ":" + state.OfferedBaseLevel + ":" + state.ReplacementQuality + ":" + state.SharedForgeTier + ":" + state.ArsenalLevelFloor;
        focused = cursor;
        // 方向键移动仅改变焦点，不重新生成卡牌或重播入场动画。
        if (choiceMode && offerSignature == signature) return;
        Background(state, state.OpeningPending ? (state.WeaponColor.Length == 0 ? "开局卷轴：先选武器颜色。双色只需包含该颜色；不允许其他纯色混入。" : "选择本局近战或远程池，两类互斥；盾牌遵守颜色条件并兼容所选池。") :
            (state.Notice.Length > 0 ? state.Notice : state.Wallet + "  |  池：" + state.PoolLabel + "  |  选中武器生成在脚边；装备保持不动"));
        choiceMode = true; offerSignature = signature; confirmChoice = choose;
        for (int i = 0; i < state.Offers.Count; i++)
            MakeSparks(202 + i * 358, 363, Colors[Catalogue.ById[state.Offers[i]].Rarity], i);
        for (int i = 0; i < state.Offers.Count; i++)
        {
            int index = i;
            var a = Catalogue.ById[state.Offers[i]];
            int color = a.Id == "color_red" ? 0xF06D74 : a.Id == "color_green" ? 0x73CF98 : a.Id == "color_purple" ? 0xB98AF2 : Colors[a.Rarity];
            double x = 32 + i * 358;
            // 卡牌以中心为缩放/转动原点，保证放大与回弹不会向右下角漂移。
            var motion = new Node(root) { x = x + 170, y = 325, posChanged = true };
            var card = new Node(motion) { x = -170, y = -174, posChanged = true };
            var glow = new Node(card);
            Border(glow, -8, -8, 356, 364, color, .13, 8);
            Border(glow, -3, -3, 346, 354, color, .35, 3);
            Rect(card, 0, 0, 340, 348, 0x12232F);
            Rect(card, 0, 0, 340, 4, color);
            Rect(card, 0, 344, 340, 4, color, .45);
            var focus = new Node(card);
            Border(focus, 0, 0, 340, 348, color, 1, 2);
            // 原生六边形印记；无需打包外部卡图。
            var seal = new Graphics(card);
            seal.beginFill(Ref<int>.In(color), Ref<double>.In(.12));
            seal.drawCircle(296, 40, 24, Ref<int>.In(6)); seal.endFill();
            Label(card, $"0{i + 1}   /   {Catalogue.Rarities[a.Rarity]}", 20, 23, color, 1.1);
            Label(card, a.Name, 20, 55, White, 1.45, 300);
            if (state.OpeningPending)
            {
                Label(card, "开局规则选择", 20, 113, color, .90, 300);
                Rect(card, 20, 149, 300, 1, 0x354653);
                Label(card, a.Description, 20, 173, White, 1.07, 300);
            }
            else
            {
                // 图标与发放共用同一个已保存的OfferWeapons索引，预览不创建InventItem，
                // 不消耗游戏随机数或物品ID，也不会在看牌时改变将获得的物品。
                string weaponId = state.OfferWeapons[i];
                WeaponPreview(card, weaponId, color);
                Label(card, WeaponName(weaponId), 122, 99, color, .92, 196);
                int level = state.RewardItemLevel(a.WeaponLevels);
                string quality = new[] { "普通", "+", "++", "S" }[Math.Clamp(state.ReplacementQuality, 0, 3)];
                Label(card, $"生成 Lv.{level} {quality} · 共享{state.Choices + 1}层", 122, 158, Muted, .75, 196);
                Rect(card, 20, 189, 300, 1, 0x354653);
                Label(card, a.Description, 20, 203, White, 1, 300);
            }
            string rank = state.Rank(a.Id) == 0 ? "新强化" : $"升级  {state.Rank(a.Id)} → {state.Rank(a.Id) + 1}";
            Label(card, rank + $"    /    上限 {(a.MaxRank == int.MaxValue ? "无限" : a.MaxRank)}", 20, 291, Muted);
            int checkLevel = !state.OpeningPending && state.CarriedBindings.TryGetValue(state.OfferWeapons[i], out var carried)
                ? Math.Max(state.OfferedBaseLevel, carried.Level) : state.OfferedBaseLevel;
            bool canPay = state.ChoiceError(a, state.Gold, state.Cells, state.Curse).Length == 0;
            Label(card, $"[{i + 1}] " + (state.OpeningPending ? "确立本局路线" : canPay ? "选择 / 契约会立即结算" : "资源或武器不足"), 20, 320, canPay ? color : 0xEF8B83, .86);
            var flash = new Node(card) { alpha = 0 };
            Rect(flash, 0, 0, 340, 348, color, .72);
            var hit = Click(card, 340, 348, () => Select(index));
            hit.onOver = new HlAction<Event>(e => { hovered = index; e.propagate = false; });
            hit.onOut = new HlAction<Event>(e => { if (hovered == index) hovered = -1; e.propagate = false; });
            cards.Add(new CardVisual(motion, glow, focus, flash, i, a.Rarity));
        }
        Label(root, state.OpeningPending ? "开局颜色与分类不消耗正式卷轴，选择后本局锁定。盾牌没有第三类。" : $"第 {state.Choices + 1} 次选择  ·  机制优先 · 锻造低频 · 列位随机  ·  重随 {state.OfferRerolls}/3", 34, 519, Muted);
        Button("[R] 重随三项", 34, 555, 180, reroll, !state.OpeningPending && state.Rerolls > 0 && state.OfferRerolls < 3);
        Button("[Tab] 海克斯核心", 230, 555, 220, core);
        Button("[Esc] 暂存选择", 868, 555, 220, exit);
        Button("[W] 品质参考 " + (state.TargetWeaponLabel.Length > 24 ? state.TargetWeaponLabel[..24] + "…" : state.TargetWeaponLabel), 466, 555, 386, weapon, !state.OpeningPending);
        Tick(0);
        Resize();
    }
    private static void WeaponPreview(Node card, string id, int color)
    {
        var frame = new Node(card) { x = 20, y = 89, posChanged = true };
        Rect(frame, 0, 0, 88, 88, 0x09151F);
        Border(frame, 0, 0, 88, 88, color, .85, 2);
        var tile = dc.Assets.Class.getItem.Invoke(id.AsHaxeString());
        if (tile == null || tile.width <= 0 || tile.height <= 0)
        {
            Label(frame, "缺少图标", 11, 37, Muted, .8);
            return;
        }
        // 仅克隆图集切片元数据，绝不修改原版共享Tile的尺寸或枢轴。
        var local = tile.clone(); local.dx = local.dy = 0;
        double scale = Math.Min(64d / local.width, 64d / local.height);
        _ = new Bitmap(local, frame)
        {
            x = (88 - local.width * scale) / 2, y = (88 - local.height * scale) / 2,
            scaleX = scale, scaleY = scale, posChanged = true
        };
    }
    private static string WeaponName(string id)
    {
        var weapon = WeaponPool.Get(id);
        if (weapon == null) return "武器待生成";
        static string Color(string c) => c == "Brutality" ? "红" : c == "Tactic" ? "紫" : c == "Survival" ? "绿" : "";
        return dc.Lang.Class.t.get(weapon.Name.AsHaxeString(), null).ToString() + "\n[" + Color(weapon.Color1) +
            (weapon.Color2.Length > 0 ? "/" + Color(weapon.Color2) : "") + "] " + (weapon.Group == 6 ? "盾牌·兼容项" : weapon.Group == 4 ? "近战" : "远程");
    }
    public void Reject() { choiceMode = false; selected = -1; offerSignature = null; }
    private static void Border(Node node, double x, double y, double w, double h, int color, double alpha, double thickness)
    {
        Rect(node, x, y, w, thickness, color, alpha);
        Rect(node, x, y + h - thickness, w, thickness, color, alpha);
        Rect(node, x, y, thickness, h, color, alpha);
        Rect(node, x + w - thickness, y, thickness, h, color, alpha);
    }
    private void MakeSparks(double x, double y, int color, int card)
    {
        for (int i = 0; i < 14; i++)
        {
            double angle = (i * 2.39996) - Math.PI;
            double velocity = 95 + i % 5 * 32;
            var node = new Graphics(root) { alpha = 0 };
            node.beginFill(Ref<int>.In(color), Ref<double>.In(1));
            node.drawCircle(0, 0, i % 3 == 0 ? 3 : 1.6, Ref<int>.In(4)); node.endFill();
            sparks.Add(new Spark(node, x, y, Math.Cos(angle) * velocity,
                Math.Sin(angle) * velocity - 75, .10 + card * .11));
        }
    }
    public void Select(int index)
    {
        if (!choiceMode || Selecting || index < 0 || index >= cards.Count || elapsed < .32 + index * .11) return;
        selected = index; selectedTime = 0; completionQueued = false;
    }
    public void Tick(double dt)
    {
        if (!choiceMode) return;
        dt = Math.Clamp(dt, 0, .1);
        elapsed += dt;
        root.alpha = Math.Clamp(elapsed / .18, 0, 1);
        if (Selecting) selectedTime += dt;
        foreach (var card in cards)
        {
            double t = Math.Max(0, elapsed - .10 - card.Index * .11);
            // 阻尼弹簧：先从下方升起，越过目标位置，再回落；三个卡槽错峰0.11秒。
            double spring = t >= 1.1 ? 1 : 1 - Math.Exp(-7.7 * t) * Math.Cos(12.3 * t);
            double targetHover = (hovered >= 0 ? hovered : focused) == card.Index ? 1 : 0;
            card.Hover += (targetHover - card.Hover) * Math.Min(1, dt * 14);
            double scale = .77 + .23 * spring + .025 * card.Hover;
            double y = 325 + (1 - spring) * 185 - card.Hover * 10;
            double alpha = Math.Clamp(t * 5.5, 0, 1);
            double exit = Math.Clamp(selectedTime / .43, 0, 1);
            if (Selecting)
            {
                if (card.Index == selected)
                {
                    scale += .10 * Math.Sin(exit * Math.PI / 2);
                    y -= exit * 14;
                    card.Flash.alpha = Math.Sin(exit * Math.PI) * .70;
                }
                else { alpha *= 1 - exit; y += exit * 48; scale -= exit * .08; }
            }
            card.Node.x = 202 + card.Index * 358;
            card.Node.y = y;
            card.Node.scaleX = card.Node.scaleY = scale;
            card.Node.rotation = (1 - spring) * (card.Index - 1) * .055;
            card.Node.alpha = alpha; card.Node.posChanged = true;
            double pulse = .5 + .5 * Math.Sin(elapsed * (card.Rarity == 3 ? 4 : 2.6) + card.Index);
            card.Glow.alpha = .40 + .22 * pulse + .55 * card.Hover + (Selecting && card.Index == selected ? exit : 0);
            card.Focus.alpha = .20 + .65 * card.Hover;
        }
        foreach (var spark in sparks)
        {
            double t = elapsed - spark.Delay;
            spark.Node.alpha = t <= 0 || t >= .8 ? 0 : (1 - t / .8) * .85;
            spark.Node.x = spark.X + spark.VX * Math.Max(0, t);
            spark.Node.y = spark.Y + spark.VY * Math.Max(0, t) + 95 * Math.Max(0, t * t);
            spark.Node.posChanged = true;
        }
        if (Selecting && selectedTime >= .43 && !completionQueued)
        {
            completionQueued = true;
            int token = revision, index = selected;
            // 动画结束后才结算；关闭面板会使旧回调失效，防止双击或重随导致错选。
            enqueue(() => { if (token == revision && choiceMode) confirmChoice(index); });
        }
    }
    public void Core(RunState state, int page, Action upgrade, Action choices, Action<int> turn, Action exit)
    {
        Background(state, state.Wallet + "  |  武器池：" + state.PoolLabel + $"  已绑定{state.BoundWeapons.Count}件  | 路线：" + (Catalogue.ById.GetValueOrDefault(state.Route)?.Name ?? "等待开局定向卷轴"));
        Rect(root, 32, 145, 300, 366, 0x112330);
        Label(root, "海克斯核心", 52, 165, Cyan, 1.4);
        var g = new Graphics(root);
        g.beginFill(Ref<int>.In(0x163C50), Ref<double>.In(1));
        g.drawCircle(181, 263, 55, Ref<int>.In(6));
        g.endFill();
        Label(root, "Lv." + state.CoreLevel, 148, 250, Gold, 1.7);
        Label(root, $"绑定伤害基准 ×2\n共享层数 {state.Choices} · 锻造+{state.SharedForgeTier}\n常驻代价 ×{state.DisplayDamagePenalty:0.00}\n核心伤害 ×{state.Growth:0.00}\n生命成长 ×{state.LifeGrowth:0.00}\n击杀 {state.Kills}  /  首领 {state.Bosses}\n附加伤害 {state.BonusDamage:N0}\n累计治疗 {state.Healing:N0}", 52, 335, White, 1.05, 260);
        Label(root, $"升级：{state.UpgradeCost} 碎片（{state.CoreUpgrades}/20）", 52, 464, Cyan);
        Label(root, "升级获得伤害/生命成长、1次重随；不治疗", 52, 488, Muted, .68);
        Rect(root, 350, 145, 738, 113, 0x12232F);
        for (int i = 0; i < 6; i++)
        {
            var family = (Family)i;
            int count = state.FamilyCount(family);
            double x = 364 + (i % 3) * 240, y = 158 + (i / 3) * 49;
            Label(root, $"{Catalogue.Families[i]}  {count} / {(count >= 4 ? "4 已成型" : "2 / 4")}", x, y, count >= 2 ? Gold : Muted);
            Label(root, ShortSynergy(i, count), x, y + 21, count >= 4 ? Cyan : Muted, .68, 226);
        }
        var owned = state.Ranks.Where(x => x.Value > 0 && Catalogue.ById.ContainsKey(x.Key)).ToArray();
        int pages = Math.Max(1, (owned.Length + 5) / 6);
        Label(root, $"已获得强化   {owned.Length} 种    第 {page + 1}/{pages} 页", 362, 272, Gold);
        for (int n = page * 6; n < Math.Min(owned.Length, page * 6 + 6); n++)
        {
            var entry = owned[n]; var a = Catalogue.ById[entry.Key];
            double y = 305 + (n % 6) * 32;
            Label(root, a.Name + "  Lv." + entry.Value, 364, y, Colors[a.Rarity], .91, 190);
            Label(root, a.Description.Replace("\n", ""), 566, y, Muted, .75, 508);
        }
        if (owned.Length == 0) Label(root, "尚未获得海克斯。拾取一个卷轴即可开始构筑。", 364, 335, Muted);
        Button("[G] 升级核心", 34, 555, 180, upgrade, state.Shards >= state.UpgradeCost && state.CoreUpgrades < 20);
        Button("[Tab] 返回选择", 230, 555, 180, choices, state.Pending > 0);
        Button("← 上一页", 426, 555, 140, () => turn(-1), pages > 1);
        Button("下一页 →", 582, 555, 140, () => turn(1), pages > 1);
        Button("[F6 / Esc] 关闭", 868, 555, 220, exit);
        Resize();
    }
    private static string ShortSynergy(int family, int count) => family switch
    {
        0 => "2：伤害+25%  4：暴击追加40%",
        1 => "2：减伤10%  4：生命+50%、再生",
        2 => "2：附伤+30%  4：脉冲间隔减半",
        3 => "2：吸血3%  4：击杀血爆",
        4 => "2：攻速+20%  4：击杀减CD",
        _ => "2：碎片+1  4：品质概率+15%"
    };
    public void Dispose() { revision++; root.remove(); }
}

internal sealed class CoreHud : IDisposable
{
    private readonly Node root = new(dc.Main.Class.ME.root.getScene());
    private readonly Text text;
    public CoreHud()
    {
        text = new Text(dc.Assets.Class.font12, root);
        text.set_textColor(0x91D9E8);
    }
    public void Draw(RunState state, bool visible)
    {
        root.set_visible(visible);
        if (!visible) return;
        var scene = root.getScene();
        double scale = Math.Max(.65, scene.height / 720d);
        root.x = 18 * scale; root.y = 58 * scale;
        root.scaleX = root.scaleY = scale; root.posChanged = true;
        text.set_text(($"[F6] 海克斯 Lv.{state.CoreLevel} | 碎片 {state.Shards} | 强化 {state.Choices}" +
            (state.Pending > 0 ? $" | 待选 {state.Pending}" : "")).AsHaxeString());
    }
    public void Dispose() => root.remove();
}
