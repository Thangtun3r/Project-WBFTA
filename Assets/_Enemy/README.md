# Enemy Scripts

Contains logic for enemy generic behaviors, specific enemy types, and their stats.

- **`BaseEnemy.cs`**: The foundation/base class that all enemy variations inherit from, handling common logic like references and base behavior.
- **`DefaultEnemy.cs`**: A concrete implementation of a standard, run-of-the-mill enemy.
- **`EnemyConfig.cs`**: Contains base attributes (health, speed, damage, etc.) for spawning enemies.
- **`EnemyVisual.cs`**: Controls the visual representation, sprite flipping, and animation triggers for the enemy.
- **`Projectile.cs`**: Handles behavior for ranged attacks fired by enemies.
- **`FloatingHealthBar.cs`**: Attached to enemies to display an in-world localized health UI above their heads.

## Interfaces
- **`IEnemyAttack.cs`**: Standardizes how enemies perform attacks.
- **`IhealthObservable.cs`**: Allows systems (like UI health bars) to listen for health changes without tight coupling.

## Sub-Folders
- **`Enemy FSM/`, `Modules/`, `EnemySO/`**: Modular Finite State Machine approach to enemy AI and data-driven ScriptableObject setups.