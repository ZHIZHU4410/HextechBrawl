using dc;
using dc.en;
using dc.en.mob;
using dc.tool;
using dc.tool.atk;
using HaxeProxy.Runtime;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.Game.Hero;
using ModCore.Mods;
using ModCore.Storage;
using ModCore.Utilities;
using Game = dc.pr.Game;
using Math = System.Math;

namespace HextechBrawl;

public sealed partial class Main(ModInfo info) : ModBase(info), IOnFrameUpdate, IOnHeroUpdate, IOnGameExit
{
    public static SaveData<RunState> Save { get; } = new("HextechBrawl");
    private RunState State => Save.Value;
    private readonly Queue<Action> actions = new();
    private readonly HashSet<int> held = new();
    private HexView view;
    private CoreHud hud;
    private Game pausedGame;
    private nint heroPointer, levelPointer;
    private bool deferred, corePage;
    private int cursor, page;
    private double openDelay, hudTimer, errorTimer;
    private static readonly HashSet<string> ScrollKinds = [
        "BrutalityUp", "TacticUp", "SurvivalUp", "AnyUp", "AllUp", "BTUp", "BSUp", "TSUp", "BossRushUp",
        "TwitchAnyUp", "TwitchBTUp", "TwitchBSUp", "TwitchTSUp"
    ];

    public override void Initialize()
    {
        base.Initialize();
        RegisterAbilities(true);
        Hook_Hero.applyItemPickEffect += OnPick;
        Hook_Hero.updateMaxLife += OnMaxLife;
        Hook_Hero.onMobDeath += OnKill;
        Hook_Hero.onDie += OnDie;
        Hook_Hero.heal += OnHealing;
        Hook_Weapon.prepare += OnPrepare;
        Hook__AttackUtils.hit += OnHit;
        Logger.Information("[HextechBrawl] v3.4.0 loaded: shared arsenal progression; switch without reset; diverse drafts and independent cultivation; double bound damage; reforge and safe choices; phase/volley; native loot scaling; weapon icon previews and bound weapon pools; F6 core, 1/2/3 select, R reroll");
    }

    private bool EnsureRun(Hero hero)
    {
        var data = hero?._level?.game?.data;
        if (data == null) return false;
        if (State == null || State.RunId != data.curRunId || State.Seed != data.gameSeed)
        {
            Close();
            Save.Value = new RunState { RunId = data.curRunId, Seed = data.gameSeed,
                RandomState = unchecked((uint)data.gameSeed ^ (uint)data.curRunId * 2654435761u) };
            heroPointer = levelPointer = 0;
            deferred = false;
            ResetCombat();
            Logger.Information("[HextechBrawl] Run initialized: run={Run}, seed={Seed}", data.curRunId, data.gameSeed);
        }
        State.GrantOpening();
        return true;
    }

    private void OnPick(Hook_Hero.orig_applyItemPickEffect orig, Hero hero, Entity from, InventItem item)
    {
        string id = item?._itemData?.id?.ToString() ?? "";
        if ((!ScrollKinds.Contains(id) && id != "QuarterUp") || !EnsureRun(hero)) { orig(hero, from, item); return; }
        // 在原版 inventory.add / TierSelect 之前替换，属性卷轴不会进入库存参与 computeTiers。
        if (id == "QuarterUp")
        {
            // 四分之一卷轴仍然是原版库存物品；仅完整四片给一次海克斯。
            if (!State.ClaimedItems.Add(item.permanentId)) return;
            hero.inventory.add(item);
            if (hero.inventory.countItemKind("QuarterUp".AsHaxeString()) >= 4)
            {
                hero.inventory.removeAll("QuarterUp".AsHaxeString());
                State.Pending++; State.Scrolls++; deferred = false;
            }
            hero.hudUpdateKeyStock();
        }
        else if (State.Claim(item.permanentId)) deferred = false;
        openDelay = .3;
        Logger.Information("[HextechBrawl] Scroll {Kind} #{Id}: scrolls={Scrolls}, pending={Pending}, vanillaTiers={B}/{T}/{S}",
            id, item.permanentId, State.Scrolls, State.Pending, hero.brutalityTier, hero.tacticTier, hero.survivalTier);
    }

    private double LifeFactor => State.LifeGrowth * Math.Max(.2, 1 + .35 * State.Rank("vitality") +
        .20 * State.Rank("end_life") + (State.Synergy(Family.Fortress, 4) ? .50 : 0) - .20 * State.Rank("pact") - .20 * State.Rank("echo") - .10 * State.Rank("cell_ritual") +
        (State.IsRoute(Family.Fortress) ? .20 : 0) - (State.IsRoute(Family.Blood) ? .15 : 0));
    private void OnMaxLife(Hook_Hero.orig_updateMaxLife orig, Hero hero)
    {
        orig(hero);
        if (!EnsureRun(hero) || State.Choices == 0 && State.Route.Length == 0) return;
        // 原版每次先从库存/属性计算基础上限；overrideMaxLife 保持比例，防止重复相乘。
        hero.overrideMaxLife((int)Math.Clamp(hero.maxLife * LifeFactor, 1, 10_000_000));
    }
    private void OnPrepare(Hook_Weapon.orig_prepare orig, Weapon weapon, double speed)
    {
        if (EnsureRun(weapon.owner))
            speed *= Math.Max(.2, Math.Pow(.90, State.Rank("might")) * Math.Pow(.92, State.Rank("armor")) *
                (State.IsRoute(Family.Fortress) ? .90 : 1)) * BalanceRules.BindingSpeed(State.BindingStacks(weapon.item.permanentId)) * Math.Min(4, 1 + (State.IsRoute(Family.Hunt) ? .15 : 0) + .25 * State.Rank("haste") + .15 * State.Rank("end_haste") +
                (State.Synergy(Family.Hunt, 2) ? .20 : 0) + (frenzyTime > 0 ? .40 * State.Rank("frenzy") : 0));
        orig(weapon, speed);
    }

    void IOnFrameUpdate.OnFrameUpdate(double dt)
    {
        errorTimer -= dt;
        try
        {
            bool f6 = Pressed(117), tab = Pressed(9), escape = Pressed(27);
            bool left = Pressed(37), right = Pressed(39), enter = Pressed(13), reroll = Pressed(82), upgrade = Pressed(71), weapon = Pressed(87);
            bool[] number = [Pressed(49), Pressed(50), Pressed(51)];
            var game = Game.Class.ME;
            var hero = game?.hero;
            if (game == null || game.destroyed || hero == null || hero.destroyed || hero.life <= 0 || dc.Main.Class.ME.isLoading)
            { Close(); hud?.Dispose(); hud = null; return; }
            if (!EnsureRun(hero)) return;
            if (view != null && (pausedGame?.HashlinkPointer != game.HashlinkPointer || levelPointer != hero._level.HashlinkPointer)) Close();
            openDelay = Math.Max(0, openDelay - Math.Min(dt, .1));
            if (view == null)
            {
                if (!game.paused && !game.hasCinematic() && openDelay <= 0 &&
                    (f6 || State.Pending > 0 && !deferred)) Open(game, State.Pending == 0);
            }
            else
            {
                view.Tick(dt);
                if (f6 || escape) { deferred = true; Close(); }
                else
                {
                    if (tab && !view.Selecting) { corePage = !corePage; page = 0; Redraw(); }
                    if (corePage && !view.Selecting)
                    {
                        if (left || right) { page = (page + (right ? 1 : -1) + PageCount) % PageCount; Redraw(); }
                        if (upgrade) UpgradeCore();
                    }
                    else if (!view.Selecting)
                    {
                        if (left || right) { cursor = (cursor + (right ? 1 : State.Offers.Count - 1)) % State.Offers.Count; Redraw(); }
                        if (weapon) CycleWeapon();
                        if (reroll) Reroll();
                        for (int i = 0; i < 3; i++) if (number[i]) { view.Select(i); break; }
                        if (enter && view != null) view.Select(cursor);
                    }
                    // 不在 Heaps 的鼠标事件遍历中删除显示对象。
                    if (view != null && actions.TryDequeue(out var action)) { actions.Clear(); action(); }
                    view?.Resize();
                }
            }
            hudTimer -= dt;
            if (hudTimer <= 0)
            {
                hudTimer = .3;
                hud ??= new CoreHud();
                hud.Draw(State, view == null && !game.paused && !game.hasCinematic());
            }
        }
        catch (Exception ex)
        {
            Close(); deferred = true;
            if (errorTimer <= 0) { errorTimer = 5; Logger.Error(ex, "[HextechBrawl] UI update failed; pending choices retained. F6 to retry"); }
        }
    }
    private bool Pressed(int key)
    {
        bool down = dc.hxd.Key.Class.isDown.Invoke(key);
        bool edge = down && !held.Contains(key);
        if (down) held.Add(key); else held.Remove(key);
        return edge;
    }
    private int PageCount => Math.Max(1, (State.Ranks.Count + 5) / 6);
    private void Open(Game game, bool core)
    {
        if (game.paused || game.hasCinematic()) return;
        RefreshWallet(game.hero);
        State.EnsureOffers();
        corePage = core; cursor = page = 0;
        // 只释放自己获得的暂停。相对鼠标模式由原版 modalPause/resume 配对管理。
        pausedGame = game;
        game.modalPause(Ref<bool>.In(false));
        dc.hxd.System.Class.setCursor.Invoke(new dc.hxd.Cursor.Default());
        view = new HexView(a => actions.Enqueue(a));
        Redraw();
    }
    private void Redraw()
    {
        actions.Clear();
        if (view == null) return;
        RefreshWallet(pausedGame.hero);
        if (corePage) view.Core(State, page, UpgradeCore, () => { if (State.Pending > 0) { corePage = false; Redraw(); } },
            step => { page = (page + step + PageCount) % PageCount; Redraw(); }, () => { deferred = true; Close(); });
        else view.Choices(State, cursor, Choose, CycleWeapon, Reroll, () => { corePage = true; Redraw(); }, () => { deferred = true; Close(); });
    }
    private void Choose(int index)
    {
        if (view == null || corePage) return;
        var id = index >= 0 && index < State.Offers.Count ? State.Offers[index] : "";
        var hero = pausedGame.hero;
        if (!ApplySelection(index, hero)) return;
        hero.updateMaxLife();
        // 选择不再免费回血；补给与锻造收益需要实际取舍。
        Logger.Information("[HextechBrawl] Selected {Id} rank={Rank}, choices={Choices}, core={Core}, pending={Pending}",
            id, State.Rank(id), State.Choices, State.CoreLevel, State.Pending);
        deferred = false; Close(); openDelay = .35;
    }
    private void Reroll() { if (view != null && !view.Selecting && State.Reroll()) Redraw(); }
    private void UpgradeCore()
    {
        if (!State.Upgrade()) return;
        pausedGame.hero.updateMaxLife();
        // 核心升级不再赠送治疗。
        Redraw();
    }
    private void Close()
    {
        var game = pausedGame; pausedGame = null;
        view?.Dispose(); view = null; actions.Clear();
        if (game != null && !game.destroyed) game.resume();
    }
    void IOnHeroUpdate.OnHeroUpdate(double dt)
    {
        var hero = Game.Class.ME?.hero;
        if (hero == null || hero.destroyed || hero.life <= 0 || !EnsureRun(hero)) return;
        if (heroPointer != hero.HashlinkPointer || levelPointer != hero._level.HashlinkPointer)
        {
            Close(); ResetCombat();
            heroPointer = hero.HashlinkPointer; levelPointer = hero._level.HashlinkPointer;
            hero.updateMaxLife();
        }
        TickCombat(hero, Math.Min(dt, .1));
    }
    void IOnGameExit.OnGameExit()
    {
        Close(); hud?.Dispose(); hud = null;
        RegisterAbilities(false);
        Hook_Hero.applyItemPickEffect -= OnPick;
        Hook_Hero.updateMaxLife -= OnMaxLife;
        Hook_Hero.onMobDeath -= OnKill;
        Hook_Hero.onDie -= OnDie;
        Hook_Hero.heal -= OnHealing;
        Hook_Weapon.prepare -= OnPrepare;
        Hook__AttackUtils.hit -= OnHit;
        ResetCombat();
    }
}
