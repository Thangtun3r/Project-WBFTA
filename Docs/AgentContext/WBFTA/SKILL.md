---
name: wbfta-unity-context
description: Project-specific context for coding agents working in the WBFTA Unity project. Use when modifying Player, Enemy, Game System, Item System, shared gameplay contracts, or Unity assembly definitions.
---

# WBFTA Unity Context

Use this context before changing gameplay code in this repository.

## First Steps

1. Read `references/architecture-map.md` for the current runtime shape and dependency rules.
2. Read `references/assembly-definition-plan.md` before changing `.asmdef` or `.asmref` files.
3. Prefer the current folder ownership:
   - Player code: `Assets/_Scripts/Player`
   - Player legacy/weapon adjuncts: `Assets/_Player` and `Assets/_Scripts/Temp` through `WBFTA.Player.asmref`
   - Enemy code: `Assets/_Enemy`
   - Game systems: `Assets/_Game System`
   - Item systems: `Assets/_Item System`
   - Shared contracts/services: `Assets/_Shared`
4. After asmdef changes, open Unity 6000.3.9f1 so generated project files are refreshed before relying on `dotnet build`.

## Working Rules

- Treat Unity as the source of truth for assembly/project generation. Root `*.csproj` files are generated and can lag behind asset moves.
- Keep assembly references acyclic. Do not add a reference from a lower-level assembly back into a higher-level one.
- Preserve serialized Unity references when moving scripts. Move `.cs` files together with their `.meta` files.
- Keep refactors scoped. Avoid combining assembly split work with enemy behavior, item balance, or player weapon changes.
- Do not introduce new global singletons as a shortcut around assembly dependency direction.

## Assembly Direction

Implemented runtime direction:

```text
WBFTA.Shared
  -> WBFTA.Enemy
  -> WBFTA.GameSystem
  -> WBFTA.ItemSystem
  -> WBFTA.Player
```

Higher assemblies may reference lower assemblies. `WBFTA.Player` references the other runtime assemblies. `WBFTA.ItemSystem` references GameSystem because chest/gold item logic uses economy, stage, and grid systems. GameSystem must not reference Player.
