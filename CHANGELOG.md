# Changes

## 3.5.0
- Generate the card's weapon as a new bound item drop at the player, leaving equipped weapons and backpack slots untouched.
- Removed automatic replacement and same-name merge; W now chooses only the normal quality reference.
- Guarded unsafe scaled player damage conversions before 32-bit integer casts; saturate high values and preserve native damage caps and boss protection phases.
- Added overflow, infinity, NaN and zero-cap regression coverage; 112,910 checks pass.
- Added an owner-checked updater targeting the existing Workshop item 3841855747 only.
- Collector encounter and in-game drop behavior have not been reproduced in this revision; runtime verification remains pending.

## 3.4.0
- Replaced item-age growth with shared run mastery; newly bound weapons immediately inherit all current layers.
- Replaced additive reforging with absolute shared +1/+2/+3 forge tiers. Completed/lower tiers cannot be selected repeatedly.
- Shared earned level floor and normal forge quality with newly obtained bound weapons and catch up carried bound items on confirmation.
- Preserved legacy high-level investments as an arsenal floor; no direct downgrade of old items.
- Removed the mandatory current-weapon cultivation slot. New drafts provide at least two not-currently-carried weapon kinds where the legal pool permits.
- Updated card preview to show switching inheritance and core forge state.
- Passed 112,904 rule checks, including repeated forge rejection, fixed-depth non-compounding growth, migration and 2,000 switch-draft hands.
- Runtime inventory integration and gameplay balance remain pending user testing.

## 3.3.0
- Removed the mandatory forge card from every draft; forging now has a 25% insertion attempt and a cooldown after selection.
- Shuffled augment positions and independently randomized the cultivation weapon slot.
- Preferred unseen and recent-history-excluded augments while retaining route and rarity preferences.
- Guaranteed a combat mechanic whenever the eligible mechanic pool is not exhausted.
- Preserved saved current offers and native item affixes; no free reroll on load or slot switch.
- Passed 116,642 checks. Regression sample: 2,000 cultivation hands, 363 forge hands, mean 18.75 distinct effects over 20 cultivation selections.
- New distribution has not been evaluated in game; user gameplay testing remains pending.

## 3.2.0
- Doubled direct attack damage for bound weapons, once at the attack boundary.
- Offered a carried bound weapon as a continued cultivation path where possible.
- Reforged carried bound duplicates in place, preserving instance identity, layers and affixes.
- Inherited normal forge quality for new weapon types (up to S), without copying legendary or other affixes.
- Kept at least one no-entry-cost option per newly generated draft.
- Capped combined passive damage penalties at a 0.60 multiplier; wall/volley tradeoffs remain separate.
- Added card previews for reforging, quality and level, plus core damage/penalty status.
- Passed 108,289 rule checks; runtime balance and equipment integration remain pending user testing.

## 3.1.0
- Added phase-wall augment for bound melee hit checks and native projectiles, with a damage tradeoff.
- Added compatible native ranged volleys: three groups at 45% damage per projectile, limited to the ranged pool.
- Matched reward item levels to native map/Boss Rush loot levels.
- Added one native primary-color stat per normal choice; removed duplicate exponential automatic growth.
- Capped bound item growth at +100% damage and +45% attack speed.
- Added capability, damage-budget and reward-level checks; 98,068 rule checks pass.
- New runtime hooks compile; in-game validation remains pending.

## 3.0.1
- Added native weapon icons and reward levels to each augment card.

## 3.0.0
- Added exclusive color/category weapon pools, instant weapon rewards and instance-bound growth.

## 2.0.0
- Added resource contracts, negative tradeoffs and strategic choices.

## 1.1.0
- Added native card entry, spring, hover and confirmation animations.
