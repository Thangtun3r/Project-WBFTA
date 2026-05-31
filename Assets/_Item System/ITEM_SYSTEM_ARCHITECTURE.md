# Item System Architecture

This document is the working guide for adding, tuning, and debugging items and item modifiers.

The system is built around one rule: ScriptableObjects are the editor source of truth, runtime classes hold state, and logic classes contain behavior. Avoid hardcoded item-to-item references unless the behavior is intentionally unique.

## Source Of Truth

Item assets live here:

```text
Assets/_Item System/Scriptable Objects/Items
```

Modifier assets live here:

```text
Assets/_Item System/Scriptable Objects/Modifiers
```

Scene database lists are build-time caches. They are not the source of truth. The editor auto-registration system rebuilds them from ScriptableObjects.

## Runtime Flow

The normal item pickup flow is:

```text
ItemDefinition asset
  -> ItemDatabaseFactory
  -> PlayerInventory.ProcessPickup(itemId)
  -> ItemRuntime
  -> ItemSystemContext
```

The normal modifier attachment flow is:

```text
ModifierDefinition asset
  -> ModifierDatabaseFactory
  -> PlayerInventory.AttachModifierToItem(...)
  -> ModifierRuntime attached to ItemRuntime
  -> ItemSystemContext
```

`PlayerInventory` owns the active `ItemRuntime` list. It creates one `ItemSystemContext`, which is the communication layer between items, modifiers, player stats, item events, proc triggers, drop weighting, and death handling.

## Important Runtime Classes

`ItemRuntime` owns:

- `ItemDefinition Definition`
- `int StackSize`
- `GameObject OwnerObject`
- optional `IItemLogic Logic`
- attached `ModifierRuntime` list
- helper methods: `GetItemStat`, `GetParameter`, `RequestTrigger`, `AttachModifier`, `RemoveModifier`

`ModifierRuntime` owns:

- `ModifierDefinition Definition`
- optional `IModifierLogic Logic`
- attached `ItemRuntime`
- attached `ItemSystemContext`
- helper method: `GetParameter`

Definitions are data. Runtime classes are state. Logic classes are behavior.

## Folder Layout

Use these folders for new item-system work:

- `Scripts/Core`: shared stat, event, query, and parameter-key types.
- `Scripts/Definitions`: ScriptableObject data definitions.
- `Scripts/Runtime`: runtime state and lifecycle classes.
- `Scripts/Database`: database factories and catalog creation.
- `Scripts/Interfaces`: behavior contracts.
- `Scripts/Item Logic`: item behavior implementations.
- `Scripts/Item Modifiers`: modifier behavior implementations.
- `Scripts/ItemEffect`: spawned prefab effect behavior.
- `Scripts/UI`: inventory and debug UI.
- `Scripts/Drops`: pickup/drop behavior.
- `Scripts/Editor`: auto-registration and editor tooling.

## ItemDefinition Fields

Every item asset should have:

- `itemId`: stable ID used by code, saves, databases, and debug tools.
- `itemName`: display name.
- `description`: player/debug description.
- `logicClassName`: concrete `IItemLogic` class name, if behavior is needed.
- `itemStats`: item-local stats such as proc chance, damage multiplier, radius, cooldown.
- `playerStats`: stats contributed to the owner, such as crit chance or max health.
- `parameters`: string-keyed item-specific tunables.

Stat-only items can leave `logicClassName` empty if all behavior is represented by `playerStats`.

## ModifierDefinition Fields

Every modifier asset should have:

- `modifierId`: stable ID used by code, databases, and debug tools.
- `modifierName`: display name.
- `description`: player/debug description.
- `logicClassName`: concrete `IModifierLogic` class name, if behavior is needed.
- `procCoefficient`: general coefficient used by some modifier logic.
- `parameters`: string-keyed modifier-specific tunables.
- `itemStatModifiers`: data-driven changes to the attached item's item stats.
- `playerStatModifiers`: data-driven changes to owner/player stat queries.
- `parameterModifiers`: data-driven changes to the attached item's parameters.

Modifiers do not currently stack per item. Attaching the same modifier definition again returns the existing runtime modifier.

## Database Auto-Registration

`ItemDatabaseFactory` and `ModifierDatabaseFactory` can auto-register assets in the editor.

Current behavior:

- Finds item/modifier ScriptableObjects under `Assets/_Item System/Scriptable Objects`.
- Sorts entries by ID for predictable scene diffs.
- Skips duplicate IDs and logs warnings.
- Logs warnings for missing IDs, missing definitions, missing descriptions, missing logic class names, and logic class names that do not resolve.
- Rebuilds serialized scene caches only when entries actually changed.

The postprocessor watches asset changes under:

```text
Assets/_Item System/Scriptable Objects/Items
Assets/_Item System/Scriptable Objects/Modifiers
```

Manual database editing should be rare. Prefer creating or editing the ScriptableObject asset, then let auto-registration update the scene cache.

## Database Cache Rules

Scene database blocks must stay separated:

- `ItemDatabaseFactory.itemEntries` contains only `ItemID` rows.
- `ModifierDatabaseFactory.modifierEntries` contains only `ModifierID` rows.

If scene YAML ever has `ModifierID` inside an item database block, or `ItemID` inside a modifier database block, the scene cache is corrupted and should be normalized.

## Stat And Parameter Queries

Item logic should not read raw definition fields directly when a value needs to support modifiers.

Use:

```csharp
float procChance = Owner.GetItemStat(ItemStatType.ProcChance, fallbackProcChance);
float radius = Owner.GetItemStat(ItemStatType.Radius, fallbackRadius);
float launchSpeed = Owner.GetParameter("HomingMissile.LaunchSpeed", fallbackLaunchSpeed);
```

Resolution order:

1. Use the value from `ItemDefinition.itemStats`, `playerStats`, or `parameters` if present.
2. Otherwise use the fallback passed by logic.
3. Apply matching data-driven modifier entries.
4. Apply custom provider logic.
5. Return `(base + flatBonus) * multiplier`.

Use enum stats when many systems can understand the value. Use namespaced string parameters for item-specific or modifier-specific tunables.

Good parameter key examples:

```text
HomingMissile.LaunchSpeed
PiercingSpike.ProjectileSpeed
Revive.BaseHealthPercent
HealthDamage.Divisor
```

## Dynamic Description Tokens

Item and modifier descriptions can include data-driven tokens. Use explicit namespaces in assets:

```text
{item:ProcChance|percent}
{item:DamageMultiplier|x}
{player:MaxHealth}
{param:HealOrb.HealAmount|percent}
{modifier:procCoefficient|percent}
```

Supported namespaces:

- `item`: an `ItemStatType` value.
- `player`: this item's own `PlayerStatEntry` contribution.
- `param`: an exact item or modifier parameter key.
- `modifier`: a supported modifier field such as `procCoefficient`.
- `modifier:procCoefficientTotal` resolves `procCoefficient * attached item stack count`.
- `modifier:procCoefficientPerStack` resolves the per-stack coefficient value.
- `modifier:attachedItemStack` and `modifier:attachedItemAdditionalStacks` resolve the current attached stack count.

Supported formatters are `number` (the default), `percent`, and `x`. Short aliases such as `{ProcChance}` and `{HealOrb.HealAmount}` are supported, but explicit namespaces are preferred for asset authoring.

Definition previews resolve first-stack authored values. Owned-item UI resolves live stack-scaled item stats and parameters after modifier providers are applied. Unknown or malformed tokens stay visible and log a deduplicated warning in editor or development builds.

## Player Stats

Player-facing stats flow through `ItemSystemContext.CalculatePlayerStat`.

Example:

```csharp
float maxHealth = inventory.ItemContext.CalculatePlayerStat(PlayerStatType.MaxHealth, baseMaxHealth);
```

Passive stat items should prefer `ItemDefinition.playerStats`. If a legacy logic class is still needed, inherit `PlayerStatItemLogicBase`.

## Events

`ItemSystemContext` publishes inventory-wide `ItemEvent` messages.

Current event types:

- `HitEnemy`
- `EnemyKilled`
- `PlayerDamaged`
- `PlayerDied`
- `ItemTriggered`
- `ItemEquipped`
- `ItemRemoved`

Use `IItemEventListener` when item or modifier logic reacts to events.

Example:

```csharp
public class MyModifierLogic : ItemModifier, IItemEventListener
{
    public void OnItemEvent(ItemEvent itemEvent)
    {
        if (itemEvent.Type != ItemEventType.ItemTriggered)
        {
            return;
        }

        // React to trigger.
    }
}
```

`ItemSystemContext` snapshots active items and modifiers before event/death iteration. This allows logic such as revive-on-death to remove item stacks safely during handling.

## Trigger Lifecycle

Triggerable items implement `ITriggerableItem`:

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

Current trigger order:

1. `ItemRuntime.RequestTrigger(...)`
2. `ItemSystemContext.RequestTrigger(...)`
3. Trigger depth guard checks for infinite chains.
4. `ITriggerableItem.CanTrigger(...)` runs first.
5. Trigger preprocessors run only after `CanTrigger` succeeds.
6. `ITriggerableItem.Trigger(...)` executes.
7. `ItemTriggered` event is published.

This means trigger preprocessors can alter execution values such as damage, but they do not alter proc chance unless explicitly built into item logic later.

The default max trigger depth is `8`.

## Proc Item Base Classes

Most proc items should inherit one of these:

- `ProcOnHitItemLogicBase`: rolls from hit events.
- `ProcOnKillItemLogicBase`: rolls from kill events.
- `ProcItemLogicBase`: use when the event source is custom.

Concrete proc items usually:

- Override `ProcChance` with `GetProcChance(fallbackProcChance)`.
- Optionally override `Cooldown`.
- Implement `ExecuteTrigger`.
- Read tunables through `GetItemStat`, `GetDamageMultiplier`, and `GetParameter`.

## Modifier Logic Interfaces

Custom modifier logic should inherit `ItemModifier` and implement only the interfaces it needs:

- `IItemEventListener`: react to events.
- `IItemStatProvider`: modify item stat queries.
- `IPlayerStatProvider`: modify player stat queries.
- `IItemParameterProvider`: modify parameter queries.
- `IItemTriggerPreprocessor`: edit trigger context after proc/cooldown validation and before trigger execution.
- `IItemDropWeightProvider`: modify item drop weights.
- `IPlayerDeathHandler`: handle player death before final death resolution.
- `IModifierParameterRequirements`: declare required `ModifierDefinition.parameters` keys for validation.

Use `Owner.AttachedItem` for the item being modified. Use `Owner.AttachedItemStackSize` when behavior scales from the attached item's stack size.

## Required Modifier Parameters

Modifier logic that depends on specific parameter keys should implement `IModifierParameterRequirements`.

Example:

```csharp
public class MyModifierLogic : ItemModifier, IModifierParameterRequirements
{
    private static readonly string[] RequiredKeys =
    {
        ModifierParameterKeys.HealthDamageDivisor
    };

    public IReadOnlyList<string> RequiredParameterKeys => RequiredKeys;
}
```

The modifier database validates these keys and logs warnings when assets are missing required parameters.

Current shared modifier keys live in `ModifierParameterKeys` inside `ItemStats.cs`.

## Current Modifier Set

The current production modifier assets are:

- `IncreaseAttachedItemDropChance`: increases future drop weight for the attached item.
- `TriggerOtherItemsOnProc`: when the attached item triggers, requests triggers on other active items.
- `TriggerOnPlayerDamaged`: when the player takes damage, has a proc-coefficient chance to trigger the attached item.
- `HalveAttachedBoostOthers`: halves attached item stats/parameters and boosts all other active items.
- `DoubleStatsBelow10PercentHealth`: doubles attached item stats/parameters while player health is below threshold.
- `DiceRollStatsOnTrigger`: rolls 1-6 before trigger execution; odd/even rolls change attached item stats/parameters for that trigger.
- `GainGoldOnProc`: grants money based on current money when the attached item triggers.
- `ScaleStatsWithCurrentHealth`: scales attached item stats/parameters from low to high multiplier based on current health.
- `ReviveBySacrificingAttachedItem`: on death, removes one attached item stack and revives the player.
- `UseHealthAsTriggerDamage`: before trigger execution, replaces trigger damage with current or max health divided by a tunable divisor.

## Drop Weighting

Item drops use:

```csharp
ItemDatabaseFactory.Instance.GetRandomItemId(playerInventory);
```

When an inventory is provided, each candidate item weight is passed through `ItemSystemContext.CalculateItemDropWeight`. Modifiers implementing `IItemDropWeightProvider` can adjust the result.

`ItemDropSpawner` caches a serialized `PlayerInventory` reference and only resolves it if missing. Prefer assigning the target inventory in the scene.

## Death And Revive

`PlayerHealth` has a death guard. Once health reaches zero, death handling runs once until the player is revived.

Death flow:

1. Player takes lethal damage.
2. `_isDead` is set.
3. `ItemSystemContext.TryHandlePlayerDeath(...)` publishes `PlayerDied`.
4. Active items/modifiers are snapshotted.
5. Death handlers run.
6. If a handler revives, `PlayerHealth.Revive(...)` clears `_isDead`.
7. If no handler revives, `Die()` runs.

This prevents repeated death/revive execution while health remains below zero.

## Debug Overlay

The runtime debug overlay supports:

- Viewing available items.
- Viewing available modifiers.
- Hover/click descriptions.
- Giving items.
- Attaching/removing modifiers from active items.

Use it for smoke testing in play mode.

## Adding A New Item

1. Create an `ItemDefinition` in `Assets/_Item System/Scriptable Objects/Items`.
2. Set `itemId`, `itemName`, `description`, and rarity/icon fields.
3. Fill `itemStats`, `playerStats`, and `parameters` where possible.
4. If behavior is needed, create a class in `Scripts/Item Logic`.
5. In logic, read tunables through `Owner.GetItemStat(...)` and `Owner.GetParameter(...)`.
6. Set `logicClassName` on the item asset to the exact class name.
7. Let auto-registration update the scene database cache.
8. Verify it appears in the runtime debug overlay.

Stat-only items can skip the logic class.

## Adding A New Modifier

1. Create a `ModifierDefinition` in `Assets/_Item System/Scriptable Objects/Modifiers`.
2. Set `modifierId`, `modifierName`, `description`, `rarity`, and `procCoefficient`.
3. Fill data-driven modifier lists if the modifier only changes stats or parameters.
4. If behavior is needed, create a class in `Scripts/Item Modifiers` inheriting `ItemModifier`.
5. Implement only the optional interfaces needed by the behavior.
6. If the logic requires parameter keys, implement `IModifierParameterRequirements`.
7. Set `logicClassName` on the modifier asset to the exact class name.
8. Let auto-registration update the scene database cache.
9. Verify it appears in the runtime debug overlay and can attach to an active item.

## Production Checklist

Before considering an item/modifier change complete:

- `dotnet build Project-WBFTA.sln --no-restore` passes.
- `git diff --check` passes.
- The asset has a stable ID.
- The asset has a clear description.
- `logicClassName` resolves, or is intentionally empty for data-only behavior.
- Required parameter keys are present on the modifier asset.
- Debug overlay shows the item/modifier.
- Relevant play-mode smoke tests pass.

## Compatibility Notes

- `ItemRuntime.TriggerEffect(EffectContext)` and `ItemDefinition.effectPrefab` are still supported.
- Generated `.csproj` files may lag behind newly added scripts until Unity regenerates them. Unity remains the source of truth for editor compilation.
- Existing no-logic stat items are valid if their ScriptableObject data fully defines their behavior.
- Avoid direct references to concrete item classes from modifiers. Prefer events, stat queries, trigger requests, and parameters.
