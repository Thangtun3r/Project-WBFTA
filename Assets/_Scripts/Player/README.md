# Player Scripts

Manages the core functionality, stats, and behaviors of the player character.

- **`Player.cs`**: The core controller script that likely ties all player components together.
- **`Movement.cs`**: Handles player locomotion, input reading, and physics movement.
- **`PlayerAttack.cs`**: Manages the player's combat, weapon logic, and attack inputs.
- **`PlayerHealth.cs`**: Handles the player's HP, taking damage, and passive health regeneration logic.
- **`PlayerConfig.cs`**: Stores the base configuration and default stat values for the player (ScriptableObject).
- **`PlayerStatMachine.cs`**: Responsible for calculating real-time combat stats (e.g., dynamically computing critical hit chance and base damage multipliers based on `PlayerConfig`).
- **`PlayerVisual.cs`**: Manages visual elements like animations, sprite flipping, or damage feedback.
- **`Cursor.cs`**: Handles custom mouse aiming, crosshair locking, or pointer visual updates.