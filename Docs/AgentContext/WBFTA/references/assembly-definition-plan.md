# Assembly Definition Plan

This describes the implemented assembly split and the rules for future changes.

## Assemblies

Runtime assemblies:

- `WBFTA.Shared`
- `WBFTA.Enemy`
- `WBFTA.GameSystem`
- `WBFTA.ItemSystem`
- `WBFTA.Player`

Editor-only assemblies:

- `WBFTA.GameSystem.Editor`
- `WBFTA.ItemSystem.Editor`

Dependency direction:

```text
WBFTA.Shared
  -> WBFTA.Enemy
  -> WBFTA.GameSystem
  -> WBFTA.ItemSystem
  -> WBFTA.Player
```

Higher assemblies may reference lower assemblies. GameSystem must not reference Player.

## Runtime Asmdefs

Current assembly assets:

- `Assets/_Shared/WBFTA.Shared.asmdef`
- `Assets/_Enemy/WBFTA.Enemy.asmdef`
- `Assets/_Game System/WBFTA.GameSystem.asmdef`
- `Assets/_Item System/WBFTA.ItemSystem.asmdef`
- `Assets/_Scripts/Player/WBFTA.Player.asmdef`
- `Assets/_Player/WBFTA.Player.asmref`
- `Assets/_Scripts/Temp/WBFTA.Player.asmref`
- `Assets/Plugins/Demigiant/DOTween/Modules/DOTween.Modules.asmdef`

Current project references:

- `WBFTA.Enemy` references `WBFTA.Shared`.
- `WBFTA.GameSystem` references `WBFTA.Shared` and `WBFTA.Enemy`.
- `WBFTA.ItemSystem` references `WBFTA.Shared`, `WBFTA.Enemy`, and `WBFTA.GameSystem`.
- `WBFTA.Player` references `WBFTA.Shared`, `WBFTA.Enemy`, `WBFTA.GameSystem`, and `WBFTA.ItemSystem`.
- Gameplay assemblies that use DOTween shortcut extensions reference `DOTween.Modules`.

## Editor Asmdefs

Current editor assembly assets:

- `Assets/_Game System/Editor/WBFTA.GameSystem.Editor.asmdef`
- `Assets/_Item System/Scripts/Editor/WBFTA.ItemSystem.Editor.asmdef`

Editor assemblies must include only the Editor platform and reference their matching runtime assembly.

## Boundary Rules

- Shared contains only contracts, small data structs, and cross-system services.
- Enemy must not reference GameSystem, ItemSystem, or Player.
- GameSystem must not reference ItemSystem or Player.
- ItemSystem may reference GameSystem for economy/stage/grid behavior, but must not reference concrete Player classes.
- Player may reference all runtime assemblies.
- Keep `.asmref` use explicit and limited to folders that intentionally belong to another assembly.

## Validation

After asmdef or file-move changes:

1. Open Unity 6000.3.9f1 and wait for script compilation/project regeneration.
2. Run `dotnet build Project-WBFTA.sln --no-restore` after Unity regenerates project files.
3. Confirm no missing script references on core Player, Enemy, Item, and GameSystem prefabs/scenes.
4. Smoke test player damage, enemy death, reward grant, item pickup, item proc projectiles, chest opening, and stage transition.

Do not treat generated `*.csproj` files as implementation artifacts. Unity `.asmdef` and `.asmref` assets are the source of truth for assembly boundaries.
