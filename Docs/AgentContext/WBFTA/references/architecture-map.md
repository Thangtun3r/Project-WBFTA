# WBFTA Architecture Map

This file describes the current project shape for coding agents. It reflects the source layout under `Assets`, not generated project files.

## Player

Primary location: `Assets/_Scripts/Player`

Additional Player assembly folders:

- `Assets/_Player` through `WBFTA.Player.asmref`
- `Assets/_Scripts/Temp` through `WBFTA.Player.asmref`

Main responsibilities:

- `Player.cs` wires core player components.
- `Movement.cs` reads input and applies player movement.
- `PlayerAttack.cs` deals damage through `IDamagable`, spawns floating damage, and publishes hit events through `GlobalEventManager`.
- `PlayerHealth.cs` owns health, shield, overheal, death, revive, and item-driven health grants.
- `PlayerStatMachine.cs` calculates attack, crit, and base health values, including item-system stat modifiers.
- Weapon scripts implement `IPlayerWeapon` and are selected by `PlayerWeaponManager`.
- `HUDManager` and `PlayerHealthBarUI` compile in Player because they display `PlayerHealth` data.

Important dependencies:

- Player references Shared, Enemy, GameSystem, and ItemSystem.
- Player implements item-system interfaces such as `IPlayerCombatStatSource`, `IPlayerHealthController`, and `IPlayerItemHealthGrantReceiver`.
- Player is the top gameplay assembly. Avoid adding references from GameSystem, ItemSystem, Enemy, or Shared back into Player.

## Enemy

Location: `Assets/_Enemy`

Main responsibilities:

- `BaseEnemy` owns health, damage, config, death, pooling return, and active registry registration.
- `EnemyFSM` and state classes drive enemy intent.
- Movement, sensor, patrol, and attack modules live in `Assets/_Enemy/Modules`.
- `EnemyStatusController` controls temporary states such as grabbed, frozen, stunned, slowed, and dead.
- `CurrentEnemyRegistry` and `EnemyPoolManager` are Enemy-owned services.

Important dependencies:

- Enemy references only Shared.
- Enemy modules request projectiles through shared `ProjectilePool`.
- Rewards are handled by GameSystem subscribing to Enemy kill events. Enemy must not reference GameSystem.

## Game System

Location: `Assets/_Game System`

Main responsibilities:

- `GameManager`, `EconomyManager`, `EnemyRewardManager`, stage transition, grid, map, prop, projectile, video, and spawner systems.
- Projectile implementations live under `Assets/_Game System/Projectile System`.
- Enemy spawning logic lives under `Assets/_Game System/Spawner Sys`.
- GameSystem editor tooling lives under `Assets/_Game System/Editor`.

Important dependencies:

- GameSystem references Shared and Enemy.
- GameSystem must not reference Player or ItemSystem.
- `EnemyEntryDrawer.cs` compiles in `WBFTA.GameSystem.Editor`.

## Item System

Locations:

- Runtime scripts: `Assets/_Item System/Scripts`
- Collectibles: `Assets/_Item System/Collectible System`
- Data assets: `Assets/_Item System/Scriptable Objects`

Main responsibilities:

- ScriptableObject definitions are the editor source of truth.
- `PlayerInventory` owns active `ItemRuntime` instances.
- `ItemSystemContext` handles item events, stat queries, trigger requests, drop weighting, and death handling.
- Logic classes under `Scripts/Item Logic` and `Scripts/Item Modifiers` implement item behavior.
- Database factories create runtime item/modifier instances and validate asset setup.

Important dependencies:

- ItemSystem references Shared, Enemy, and GameSystem.
- ItemSystem does not reference concrete Player classes.
- Use `IPlayerCombatStatSource`, `IPlayerHealthController`, and `IPlayerItemHealthGrantReceiver` for player-owned data/control.
- Item logic triggers shared services such as `ProjectilePool` and `WorldObjectSpawner`.
- ItemSystem editor tooling compiles in `WBFTA.ItemSystem.Editor`.

## Shared

Location: `Assets/_Shared`

Current shared contents:

- Contracts: `IDamagable`, `IHealthObservable`, `IProjectile`, `IWorldObjectSpawner`
- FSM base types: `BaseState<T>` and `StateMachine<T>`
- Projectile request data: `ProjectileRequest`
- Cross-system services: `ProjectilePool`, `WorldObjectSpawner`, `VFXStation`, `FloatingDamagePool`, `OnScreenEffect`

Rule:

- Shared must not reference Enemy, GameSystem, ItemSystem, or Player.

## Build Notes

Unity must regenerate generated project files after asmdef/file moves. If `dotnet build Project-WBFTA.sln --no-restore` reports missing moved source files, open Unity or regenerate project files first.
