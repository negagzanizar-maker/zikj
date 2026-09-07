# ZIKJ Unity Project

Use this folder as the Unity project root.

## Layout

- `Assets/Scripts/ZIKJ/` - gameplay, input, tracking, HUD, and mode scripts
- `Assets/Scenes/` - generated Unity scenes for launcher, each game mode, and projection alignment
- `Assets/Editor/ZIKJ/` - scene creation and Windows build commands
- `Packages/` - Unity package manifest/lock
- `ProjectSettings/` - Unity project settings

Generated folders such as `Library/`, `Temp/`, `Obj/`, and `Builds/` stay out of Git.

## Playable Modes

The project currently has six Unity game modes:

- `InfectedArena`
- `BattleRoyaleArena`
- `GrandPrixArena`
- `DriftArena`
- `TronTrails`
- `LastOneLit`

Each scene is generated from `InfectedSceneBootstrap`, then upgraded by `ZIKJReal3DSceneAssets`, which creates the camera, track mesh, scenery, HUD, game manager, model-backed real 3D karts, imported FBX venue dressing, AI, tracking receiver, tracking input manager, tracking debug HUD, and performance monitor. Non-player karts use AI fallback until their first tracking packet arrives; live tracking then takes over per kart and stale tracking stops that kart safely.

Current mode-specific scene themes:

- `InfectedArena`: zombie apocalypse / quarantine dressing.
- `BattleRoyaleArena`: war-zone dressing.
- `GrandPrixArena`: colorful kart-racer arena with item boxes and respawning boost pads.
- `DriftArena`: Moroccan tent-inspired drift venue with red/green fabric and star motifs.
- `LastOneLit`: spotlight-focused lighting arena.

`VenueLauncher` is generated from `VenueLauncherScene`. It creates a Unity menu with launch buttons for every game/tool scene, a preview track, number-key shortcuts, and an `Esc` return hotkey.

The project also includes `ProjectionAlignment`, generated from `ProjectionAlignmentScene`. It creates the 1280 x 720 projection surface, four projector coverage quads, overlap bands, calibration grid/points, track mesh, six tracking marker previews, top-down orthographic camera, tracking receiver, tracking input manager, tracking debug HUD, and performance monitor.

Real 3D scene dressing is build-safe: `ZIKJReal3DSceneAssets` creates `Assets/Prefabs/ZIKJ/RealKart.prefab` from imported Kenney kart FBX assets and places imported FBX props into each saved scene under `Real 3D Asset Dressing`. The launcher, every mode, and Projection Alignment receive their own model set.

Controls:

- `WASD` or arrow keys: drive
- `Space`: use item in Grand Prix
- `T`: toggle tracking debug HUD
- `C` / `V` / number keys: camera controls where supported
- Launcher `1`-`7`: open a scene
- `Esc`: return to the launcher

## Create Scenes

Open `C:\Users\Admin\Desktop\zikj\unity` in Unity Hub, then use:

- `ZIKJ > Create Infected Arena Scene`
- `ZIKJ > Create Battle Royale Arena Scene`
- `ZIKJ > Create Grand Prix Arena Scene`
- `ZIKJ > Create Drift Arena Scene`
- `ZIKJ > Create Tron Trails Scene`
- `ZIKJ > Create Last One Lit Scene`
- `ZIKJ > Create Projection Alignment Scene`
- `ZIKJ > Create Venue Launcher Scene`

## Build

Build one mode from the Unity menu:

- `ZIKJ > Build Windows Infected`
- `ZIKJ > Build Windows Battle Royale`
- `ZIKJ > Build Windows Grand Prix`
- `ZIKJ > Build Windows Drift Arena`
- `ZIKJ > Build Windows Tron Trails`
- `ZIKJ > Build Windows Last One Lit`
- `ZIKJ > Build Windows Projection Alignment`
- `ZIKJ > Build Windows Venue Launcher`

Build every deployable from PowerShell:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe' -quit -batchmode -projectPath 'C:\Users\Admin\Desktop\zikj\unity' -executeMethod ZIKJBuild.BuildWindowsAll
```

Build outputs are written to `unity/Builds/Windows/`, including the one-stop `ZIKJ-VenueLauncher.exe`.

Open the full Unity project locally with:

```powershell
.\unity\Builds\Windows\ZIKJ-VenueLauncher.exe
```

## Smoke Tests

Run the structural smoke validator for all modes from PowerShell:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe' -quit -batchmode -projectPath 'C:\Users\Admin\Desktop\zikj\unity' -executeMethod ZIKJSmokeTests.RunAll
```

The validator checks every mode scene/build pair, creates each mode from the bootstrapper, applies the real-3D asset pass, verifies the expected mode script/HUD/tracking infrastructure, verifies the real 3D kart prefab and imported dressing models, spawns karts, confirms non-player karts are registered with tracking while remaining AI-playable before packets arrive, and validates the Projection Alignment and Venue Launcher scene/build pairs.

Run the PlayMode runtime smoke tests from PowerShell:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe' -batchmode -projectPath 'C:\Users\Admin\Desktop\zikj\unity' -runTests -testPlatform PlayMode -testResults 'C:\Users\Admin\Desktop\zikj\unity-playmode-results.xml'
```

The PlayMode suite boots each mode in real play mode, waits for scene construction, advances fixed updates, verifies the mode, HUD, camera, tracking, and kart runtime objects stay alive, validates the Projection Alignment and Venue Launcher runtime scenes, confirms AI fallback moves non-player karts before tracking arrives, sends simulator-shaped UDP tracking data through `TrackingReceiver` into a registered kart on both an allocated test port and the deployment port `5555`, and checks first-frame tracking handoff, interpolation, stale-frame timeout, and AI/physics suppression for tracked karts.

## Latest Local Validation

Last verified on 2026-05-30:

- `ZIKJSmokeTests.RunAll` passed for all six modes plus Projection Alignment and Venue Launcher.
- PlayMode runtime smoke tests passed: 14 total, 14 passed, 0 failed.
- Real 3D editor smoke assertions passed for model-backed kart prefabs and imported FBX dressing in all deployable scenes.
- Drift Arena, Battle Royale, and Infected theme props are asserted by PlayMode and editor smoke tests.
- Grand Prix colorful arena props, item boxes, and boost pads are asserted by PlayMode and editor smoke tests.
- Unity received simulator-shaped tracking packets on deployment UDP port `5555`.
- AI fallback before tracking, first-frame tracking handoff, tracking interpolation, stale-frame timeout, and tracking-mode AI/physics suppression passed.
- `tracking/tracking_simulator.py` emitted valid UDP packets with `kart_id`, `x`, `y`, `heading`, `speed`, and `t`.
- `ZIKJBuild.BuildWindowsAll` completed successfully for all eight Windows deployables.
- Build-all regenerated all scenes before building, including `Real 3D Asset Dressing` roots and `RealKart.prefab` references.
- Player builds do not include `ZIKJ.PlayMode.Tests.dll`.
- `ZIKJ-VenueLauncher.exe` launched headlessly for 12 seconds with no error or exception lines in its player log.
- Tracking unit tests passed: 6 total, 6 passed, 0 failed.
- Projection unit tests passed: 8 total, 8 passed, 0 failed.
- Operations preflight unit tests passed: 5 total, 5 passed, 0 failed.
- Local software preflight completed with 32 pass, 6 hardware sign-off warnings, 0 fail.
