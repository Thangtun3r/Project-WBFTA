# Item System Architecture Reference

This document is the reference for future item and modifier work. The system is intentionally incremental: old item logic still works, while new item stats, player stat queries, modifiers, and item-to-item communication can be added without hardcoded item pairs.

## Runtime Shape

The main flow is:

```text
ItemDatabaseFactory
  -> ItemDefinition + optional IItemLogic
  -> PlayerInventory.ProcessPickup(itemId)
  -> ItemRuntime
  -> ItemSystemContext
```

`PlayerInventory` owns the active `ItemRuntime` list and creates one `ItemSystemContext`. The context is the shared communication layer for all owned items and modifiers.

`ItemRuntime` owns:

- `ItemDefinition Definition`
- `int StackSize`
- `GameObject OwnerObject`
- optional `IItemLogic Logic`
- attached `ModifierRuntime` list
- helper methods: `GetItemStat`, `GetParameter`, `RequestTrigger`, `AttachModifier`, `RemoveModifier`

`ModifierRuntime` owns:

- `ModifierDefinition Definition`
- attached `ItemRuntime`
- optional `IModifierLogic`

Modifiers do not stack. Attaching the same modifier definition again returns the existing runtime modifier. Modifier stat/parameter scaling uses the attached item's `StackSize`.

Definitions are data. Runtime classes are state. Logic classes are behavior.

## Folder Layout

Use these folders when adding new item-system files:

- `Scripts/Core`: shared event/stat/query types and cross-system managers.
- `Scripts/Definitions`: `ScriptableObject` data definitions.
- `Scripts/Runtime`: runtime state and lifecycle classes.
- `Scripts/Database`: factories/catalogs that create runtime definitions and logic.
- `Scripts/Interfaces`: behavior contracts.
- `Scripts/Item Logic`: item behavior implementations.
- `Scripts/Item Modifiers`: modifier behavior implementations.
- `Scripts/ItemEffect`: spawned visual/effect prefab logic.
- `Scripts/UI`: item inventory/debug UI.
- `Scripts/Drops`: item pickup/drop scene behavior.

## Definition Data

`ItemDefinition` now supports three data lists:

- `itemStats`: common item-local stats.
- `playerStats`: stats that affect the player/inventory owner.
- `parameters`: string-keyed special values for unique item behavior.

Common item stats use `ItemStatType`:

```text
ProcChance
DamageMultiplier
Radius
BounceCount
Cooldown
Duration
TriggerCount
```

Player stats use `PlayerStatType`:

```text
AttackDamage
CritChance
CritDamage
MoveSpeedMultiplier
MaxHealth
```

Special item-only values should use namespaced string keys:

```text
BlackHole.PullForce
BlackHole.PullDuration
Bomb.FuseTime
Missile.TurnSpeed
ChainDamage.VisualJitter
```

Use common enum stats when many items/modifiers can understand the value. Use parameters when the value is specific to one item family.

## Stat Query Rules

Item logic should not read mutable fields directly when the value needs to be modifier-compatible.

Use:

```csharp
float procChance = Owner.GetItemStat(ItemStatType.ProcChance, fallbackProcChance);
float radius = Owner.GetItemStat(ItemStatType.Radius, fallbackRadius);
float pullForce = Owner.GetParameter("BlackHole.PullForce", fallbackPullForce);
```

Resolution order:

1. Use the value from `ItemDefinition.itemStats` or `ItemDefinition.parameters` if present.
2. Otherwise use the fallback passed by the logic class.
3. Apply matching modifier entries and providers.
4. Return `(base + flatBonus) * multiplier`.

This keeps existing assets working. Old logic fields remain useful as fallbacks until each item asset is fully data-authored.

Player stats are resolved through `PlayerStatMachine`:

```csharp
inventory.ItemContext.CalculatePlayerStat(PlayerStatType.CritChance, baseCritRate);
```

Data-only stat items can work by filling `ItemDefinition.playerStats`, even if they have no logic class.

Legacy passive stat logic such as crit, speed, and max health now participates through `IPlayerStatProvider`. Player systems consume those values through `ItemSystemContext` instead of item logic mutating player components directly.

## Event And Trigger Channels

`ItemSystemContext` publishes inventory-wide `ItemEvent` messages. Current bridge events include:

- `HitEnemy`
- `EnemyKilled`
- `ItemEquipped`
- `ItemRemoved`
- `ItemTriggered`

Use `IItemEventListener` for behavior that reacts to events:

```csharp
public class MyLogic : ItemLogicBase, IItemEventListener
{
    public void OnItemEvent(ItemEvent itemEvent)
    {
        if (itemEvent.Type != ItemEventType.HitEnemy)
        {
            return;
        }

        Owner.RequestTrigger(new ItemTriggerContext
        {
            SourceItem = Owner,
            Owner = Owner.OwnerObject,
            Target = itemEvent.Target,
            Damage = itemEvent.Damage,
            IsCrit = itemEvent.IsCrit
        });
    }
}
```

Use `ITriggerableItem` when an item can be triggered by itself or another item:

```csharp
public bool CanTrigger(ItemTriggerContext context)
{
    return context.Target != null;
}

public void Trigger(ItemTriggerContext context)
{
    // Execute effect.
}
```

The context has a trigger depth guard. Default max depth is `8`. This prevents infinite loops when items trigger other items.

Most proc items should not implement this plumbing directly. Use:

- `ProcOnHitItemLogicBase` for items that roll from hit events.
- `ProcOnKillItemLogicBase` for items that roll from kill events.
- `ProcItemLogicBase` when an item needs a custom event source.

Concrete proc items should usually override `ProcChance`, optionally override `Cooldown`, and implement `ExecuteTrigger`.

## Modifier Rules

Use `ModifierDefinition` for modifier data:

- `itemStatModifiers`: modifies the attached item's item stats.
- `playerStatModifiers`: modifies player stat queries.
- `parameterModifiers`: modifies attached item parameters.
- optional `LogicClassName` in `ModifierDatabaseFactory` creates custom behavior.

Modifiers are one attachment per modifier definition per item. Their numeric entries scale from the attached item's stack count, not from a modifier-local stack.

Use `DefinitionStatModifierLogic` for data-only modifiers. The base `ModifierRuntime` already applies definition stat and parameter modifier entries.

Use custom `IModifierLogic` when a modifier needs behavior. Example: `TriggerOtherItemsOnOwnerTriggerLogic` listens for owner item trigger events and requests triggers on other triggerable items.

Runtime attachment API:

```csharp
inventory.AttachModifierToItem(itemRuntime, modifierId);
inventory.AttachModifierToItem(itemId, modifierId);
inventory.RemoveModifierFromItem(itemRuntime, modifierId);
inventory.RemoveModifierFromItem(itemId, modifierId);
```

Modifier pickups, sockets, and UI are not implemented yet. Future systems should call these APIs instead of mutating `ItemRuntime.Modifiers` directly.

The runtime debug overlay has a `Modifiers` tab. Use it in play mode to select an active item, select a modifier from `ModifierDatabaseFactory`, then attach or remove the modifier.

## Fast Recipes

Data-only stat item:

1. Create an `ItemDefinition`.
2. Fill `playerStats`, for example `CritChance` or `AttackDamage`.
3. Add it to `ItemDatabaseFactory` with an empty `LogicClassName`.
4. Pick it up normally. `PlayerStatMachine` reads the value through `ItemSystemContext`.

Proc item:

1. Create a class inheriting `ProcOnHitItemLogicBase` or `ProcOnKillItemLogicBase`.
2. Add readable fallback fields, for example `fallbackProcChance`, `fallbackDamageMultiplier`, or `fallbackRadius`.
3. Override `ProcChance` with `GetProcChance(fallbackProcChance)`.
4. Override `Cooldown` only when the item has a non-zero fallback cooldown.
5. Implement `ExecuteTrigger` for the unique effect.
6. Read tunables through `GetItemStat`, `GetDamageMultiplier`, and `GetParameter`.

Passive fallback stat item:

1. Prefer data-only `ItemDefinition.playerStats` with no logic.
2. If legacy code still needs a logic class, inherit `PlayerStatItemLogicBase`.
3. Override `StatType`, `FallbackBaseValue`, and `FallbackPerStackValue`.
4. The base class skips fallback logic when `ItemDefinition.playerStats` already contains the same stat, preventing double application.
5. Add the logic class name to the item database entry.

Data-only modifier:

1. Create a `ModifierDefinition`.
2. Fill `itemStatModifiers`, `playerStatModifiers`, or `parameterModifiers`.
3. Add it to `ModifierDatabaseFactory`.
4. Use `DefinitionStatModifierLogic` as the logic class if the modifier should be explicit in the database.
5. Attach it at runtime through the overlay or `PlayerInventory.AttachModifierToItem`.

Behavior modifier:

1. Create a class inheriting `ItemModifier`.
2. Implement only the needed optional interfaces.
3. Use `Owner.AttachedItem` for the item being modified.
4. Use `Owner.AttachedItemStackSize` when behavior should scale with the attached item.
5. Add it to `ModifierDatabaseFactory`.

Examples:

- `ExampleProcItemLogic` shows the event listener + triggerable item pattern.
- `ExampleModifierLogic` shows modifier event/stat provider patterns.
- Example classes are templates only; do not add them to databases as production gameplay logic.

## Adding A New Item

1. Create or update an `ItemDefinition`.
2. Add common `itemStats`, `playerStats`, and `parameters` where possible.
3. If the item needs behavior, create an `IItemLogic` class, usually by inheriting `ItemLogicBase`.
4. Add the item to `ItemDatabaseFactory`.
5. In logic, read modifier-compatible values with `Owner.GetItemStat` and `Owner.GetParameter`.
6. If the item reacts to hits/kills/etc., implement `IItemEventListener`.
7. If other items/modifiers can trigger it, implement `ITriggerableItem`.

Stat-only items do not need logic. `PlayerInventory.ProcessPickup` accepts items with no logic as long as the definition exists.

## Adding A New Modifier

1. Create a `ModifierDefinition`.
2. Fill `itemStatModifiers`, `playerStatModifiers`, or `parameterModifiers`.
3. If it is data-only, use `DefinitionStatModifierLogic` or leave logic empty if no custom lifecycle is needed.
4. If it needs behavior, create a class inheriting `ItemModifier` and implement optional interfaces such as `IItemEventListener`, `IItemStatProvider`, `IPlayerStatProvider`, or `IItemParameterProvider`.
5. Add it to `ModifierDatabaseFactory`.
6. Attach it through `PlayerInventory.AttachModifierToItem`.

Do not hardcode direct references to concrete item classes unless the item is intentionally unique. Prefer item events, stat queries, and parameter keys.

## Compatibility Notes

- Existing item logic fields should remain until their item assets are fully migrated to definition stats.
- `ItemRuntime.TriggerEffect(EffectContext)` and `ItemDefinition.effectPrefab` are still supported.
- Current item logic should use `IItemEventListener`, `ITriggerableItem`, and stat/query providers instead of subscribing directly to global gameplay events.
- Generated `.csproj` files may not include new scripts until Unity regenerates them. Unity is the source of truth for compilation.
