# System Scripts

Global managers and systems controlling game loops, UI, economy, and spawning.

- **`GameManager.cs`**: The core singleton or manager responsible for the main game state (start, pause, game over).
- **`EconomyManager.cs`**: Manages the game's currencies, points, or resource loops (e.g., collecting coins/XP).
- **`HUDManager.cs`**: Handles the screen-space Heads-Up Display (showing player health, currency, timers).
- **`Spawner.cs`**: Responsible for instantiating enemies, waves, or items into the level dynamically.
- **`ModifierManager.cs`**: Handles applying buffs, debuffs, or upgrades to entities globally.
- **`HealthbarDamp.cs`**: Utility script used to smoothly transition or "dampen" health bar UI changes for visual juice.
- **`PlayerSO.cs` & `PlayerStats.cs`**: Global references and ScriptableObjects to track the player's persistent stats across scenes or game loops.