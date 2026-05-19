# Enemy Architecture Notes

## Current Shape

Enemies are built from small modules:

- `EnemyFSM` owns high-level state: `Patrol`, `Chase`, `Attack`, `Dead`.
- Movement modules implement `IMovement`.
- Sensor modules implement `ITargetSensor`.
- Attack modules implement `IEnemyAttack`.
- `BaseEnemy` owns shared health, config, death, rewards, pooling reset.
- `EnemyStatusController` is the shared control layer for temporary states and effects.

The FSM should decide intent. Modules should execute that intent, but must also respect `EnemyStatusController`.

## Status Controller

`EnemyStatusController` answers shared control questions:

- `CanThink`: FSM can update states.
- `CanMove`: movement modules can apply movement.
- `CanAttack`: attack modules can fire, damage, spawn hazards, or run attack behavior.
- `MoveSpeedMultiplier`: movement speed modifier for slow effects.
- `AllowsExternalMovement`: external systems like drag/throw can move the enemy.

Blocking statuses:

- `Grabbed`
- `Frozen`
- `Stunned`
- `Dead`

Modifier status:

- `Slowed`

Use `EnemyStatusController.FindFor(this)` from modules so status lookup remains safe even if Unity `Awake` order changes.

## Module Rules

Movement modules should check:

```csharp
if (_status != null && !_status.CanMove)
{
    _currentVelocity = Vector2.zero;
    return;
}

float speedMultiplier = _status != null ? _status.MoveSpeedMultiplier : 1f;
```

Attack modules should check:

```csharp
if (_status != null && !_status.CanAttack)
{
    return;
}
```

FSM updates should stop when:

```csharp
if (_status != null && !_status.CanThink)
{
    return;
}
```

Do not disable `EnemyFSM` just to pause an enemy. Apply a status instead.

## Drag / Grab Behavior

Dragging applies `EnemyStatusType.Grabbed`.

Grabbed means:

- no FSM thinking
- no movement module control
- no attacks
- external drag movement is allowed

On release, remove `Grabbed` and re-enter the current FSM state so attack/patrol state setup is restored cleanly.

## Adding Future Effects

Slow:

```csharp
status.ApplyStatus(EnemyStatusType.Slowed, duration: 3f, moveSpeedMultiplier: 0.5f);
```

Freeze:

```csharp
status.ApplyStatus(EnemyStatusType.Frozen, duration: 2f);
```

Stun:

```csharp
status.ApplyStatus(EnemyStatusType.Stunned, duration: 1f);
```

Death should use `Dead` and be cleared only when the enemy is reset from the pool.

## Refactor Boundaries

Keep these separate:

- Status/control architecture.
- Rigidbody-based movement refactor.
- New enemy behaviors.

Do not combine them in one change unless necessary. The status layer is meant to preserve current enemy behavior while making control effects easier to add.
