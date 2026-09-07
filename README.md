# ZIKJ

Mixed-reality karting project prototype.

This repository currently contains:

- Browser-playable game mode prototypes in `modes/`
- Shared browser rules/physics helper in `engine.js`
- Mock player app, operator dashboard, and venue layout
- Unity project source, scenes, tracking integration, and Windows builds in `unity/`
- Python tracking simulator/service in `tracking/`
- Projection calibration helpers and a four-projector alignment config in `projection/`
- Operations preflight checks and hardware safety sign-off gates in `ops/`

## Current Status

The Unity vertical slice has expanded into one Unity launcher, six playable Unity modes, and a Unity projection alignment scene:

1. `Infected`
2. `Battle Royale`
3. `Grand Prix`
4. `Drift Arena`
5. `Tron Trails`
6. `Last One Lit`

Each mode has a generated Unity scene, keyboard/player controls, model-backed real 3D karts, imported 3D venue dressing, AI fallback, HUD, and a Windows build target. Unity scenes also auto-create tracking/debug/performance components so local UDP simulator testing works without manually wiring objects into every scene. Non-player karts drive with AI until their first live tracking packet arrives, then tracking takes over per kart.

`VenueLauncher` is the Unity entry point. It shows a Unity-built menu with buttons and number-key shortcuts for every game mode and Projection Alignment, and `Esc` returns to the launcher after you enter a scene.

Real 3D content is generated into every deployable scene from imported Kenney FBX assets. The build pipeline creates `unity/Assets/Prefabs/ZIKJ/RealKart.prefab`, assigns it to every mode scene, and adds a `Real 3D Asset Dressing` root with model-based gates, barriers, stands, props, cameras/projectors, and per-mode set dressing.

Current themed scene direction:

- `Drift Arena`: Moroccan tent-inspired drift venue with red/green fabric, star motifs, lanterns, carpet bands, and a themed start arch.
- `Battle Royale`: war-zone arena with bunker, sandbags, watchtowers, supply crates, hazard stripes, and drop-zone marker.
- `Infected`: zombie apocalypse / quarantine arena with boarded barricades, wrecked cars, toxic barrels, cracked asphalt, and quarantine signage.
- `Grand Prix`: colorful kart-racer arena with rainbow start arch, festival pennants, balloon clusters, floating item markers, item boxes, and respawning boost pads.

`ProjectionAlignment` is a first-class Unity scene for local venue/projector work. It shows the 1280 x 720 projection surface, four projector footprints, overlap zones, alignment grid/points, the track mesh, tracking marker previews, top-down camera, and tracking/debug/performance components.

## Unity Builds

Use `unity/` as the Unity project root.

Current Windows build outputs:

- `unity/Assets/Scenes/VenueLauncher.unity` -> `unity/Builds/Windows/ZIKJ-VenueLauncher.exe`
- `unity/Assets/Scenes/InfectedArena.unity` -> `unity/Builds/Windows/ZIKJ-Infected.exe`
- `unity/Assets/Scenes/BattleRoyaleArena.unity` -> `unity/Builds/Windows/ZIKJ-BattleRoyale.exe`
- `unity/Assets/Scenes/GrandPrixArena.unity` -> `unity/Builds/Windows/ZIKJ-GrandPrix.exe`
- `unity/Assets/Scenes/DriftArena.unity` -> `unity/Builds/Windows/ZIKJ-DriftArena.exe`
- `unity/Assets/Scenes/TronTrails.unity` -> `unity/Builds/Windows/ZIKJ-TronTrails.exe`
- `unity/Assets/Scenes/LastOneLit.unity` -> `unity/Builds/Windows/ZIKJ-LastOneLit.exe`
- `unity/Assets/Scenes/ProjectionAlignment.unity` -> `unity/Builds/Windows/ZIKJ-ProjectionAlignment.exe`

Run everything from one Unity launcher:

```powershell
.\unity\Builds\Windows\ZIKJ-VenueLauncher.exe
```

Build all deployables from PowerShell:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe' -quit -batchmode -projectPath 'C:\Users\Admin\Desktop\zikj\unity' -executeMethod ZIKJBuild.BuildWindowsAll
```

## Tracking

For local laptop testing, run:

```powershell
cd c:\Users\Admin\Desktop\zikj\tracking
python tracking_simulator.py
```

Then open any Unity scene, press Play, and press `T` to toggle the tracking debug HUD. If the simulator is not running, non-player karts keep driving with local AI so the built game remains playable.

## Operations Preflight

Run the local software preflight:

```powershell
python ops\preflight.py --root . --config ops\preflight_config.json
```

Use `--require-hardware` only during venue acceptance. It fails until the hard-wired emergency stop, independent speed governor, operator controls, staff flow, safety checklist, and controlled-speed playtest have been signed off.

## Safety Note

Game logic must never be the only safety layer. Real kart emergency stop, speed governor, and motor control must be handled by dedicated venue hardware.
