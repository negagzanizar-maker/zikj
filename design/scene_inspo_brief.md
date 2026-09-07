# ZIKJ Scene Inspo Brief

Put visual references in:

```text
unity/Assets/StreamingAssets/Inspo/
```

Use `.png`, `.jpg`, or `.jpeg`.

For each image, useful notes:

- What should we steal from it: layout, lighting, materials, color, signage, props, camera mood, projection look, or track shape?
- Which game mode should it influence?
- What should we avoid?

Current scene design target:

- Replace the placeholder oval/arena look.
- Keep Unity gameplay/tracking functional.
- Build scenes from reference-driven art direction instead of primitive placeholder map pieces.
- Keep Projection Alignment useful for venue setup.

User direction captured on 2026-05-30:

- Drift Arena: full Moroccan tent / festival-tent feeling, with red and green, star motifs, lanterns, carpet/pattern energy.
- Battle Royale: war-themed arena dressing.
- Infected: zombie apocalypse / quarantine theme.
- Grand Prix: immersive colorful kart-racer arena, bright and playful, with visible boost pads and item/powerup boxes spawning.

Current Unity implementation:

- `DriftArena` uses `InfectedEnvironmentBuilder.ArenaTheme.MoroccanTent`.
- `BattleRoyaleArena` uses `InfectedEnvironmentBuilder.ArenaTheme.WarZone`.
- `InfectedArena` uses `InfectedEnvironmentBuilder.ArenaTheme.ZombieApocalypse`.
- `GrandPrixArena` uses `InfectedEnvironmentBuilder.ArenaTheme.ColorfulKartArena`.
