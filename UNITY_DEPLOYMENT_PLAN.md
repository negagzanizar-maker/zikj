# ZIKJ Unity Deployment Plan

This is the plan from the current repo state to a full venue-ready deployment.

## 1. Local Unity Game Deployment

Status: implemented.

- Six playable Unity modes exist as scenes.
- Eight Windows deployables exist: six game modes plus Projection Alignment and Venue Launcher.
- Local keyboard player and AI karts work through the shared `GameManager`/`KartController` stack.
- Tracking receiver, tracking input manager, tracking debug HUD, and performance monitor are auto-created in generated scenes.
- `Assets/Scenes/VenueLauncher.unity` exists as the Unity entry point for all game/tool scenes.
- `Assets/Scenes/ProjectionAlignment.unity` exists as an actual Unity calibration scene with projector coverage, overlap bands, alignment grid/points, track mesh, marker previews, camera, and tracking/debug components.
- Mode-specific themed scenery is implemented for Drift Arena, Battle Royale, Infected, and Grand Prix.

Validation targets:

- Build all eight Windows deployables. Verified 2026-05-30 with `ZIKJBuild.BuildWindowsAll`.
- Run structural scene smoke checks. Verified 2026-05-30 with `ZIKJSmokeTests.RunAll`.
- Run PlayMode runtime smoke checks. Verified 2026-05-30 with 13 passed, 0 failed.
- Verify requested themes remain present in generated Unity scenes. Verified 2026-05-30 for Moroccan tent Drift Arena, war-zone Battle Royale, and zombie quarantine Infected.
- Verify Grand Prix colorful kart-racer dressing, item boxes, and respawning boost pads. Verified 2026-05-30.
- Verify local simulator-shaped UDP packets reach Unity tracking and move a registered kart. Verified 2026-05-27 by `TrackingSimulatorPacketMovesRegisteredKart`.
- Verify Unity receives simulator-shaped tracking packets at deployment UDP port `5555`. Verified 2026-05-29 by `TrackingDefaultPort5555MovesRegisteredKart`.
- Verify `tracking/tracking_simulator.py` emits the current UDP packet schema with `t`. Verified locally 2026-05-27.
- Run `tracking/tracking_simulator.py`, press `T` in Unity, and verify packet count increments during manual operator testing.

## 2. Gameplay Polish

Status: playable baseline, not final art.

Next polish work:

- Replace runtime primitive karts, items, safe zones, lights, and themed scenery blockouts with production prefabs.
- Add audio, countdowns, round reset flow, pause/operator flow, and result screens that match venue branding.
- Tune AI behavior per mode.
- Playtest mode durations, damage, drift scoring, trail collision, and light shrink rates.

## 3. Tracking Deployment

Status: local simulator ready; real cameras require venue hardware.

Next hardware/software work:

- Configure camera RTSP URLs in `tracking/tracking_config.json`.
- Generate ArUco markers. Verified locally 2026-05-27 with `tracking/markers/marker_00.png` through `marker_05.png` and `tracking/markers_sheet.png`.
- Print ArUco markers at 100mm x 100mm and affix to kart roofs.
- Calibrate each camera with `tracking/calibrate_camera.py`.
- Run `tracking/tracking_service.py` and validate marker detection. Synthetic frame detection, calibration, and UDP output verified locally 2026-05-27.
- Verify Unity receives real UDP packets at port `5555`. Verified locally 2026-05-29 with simulator-shaped UDP payloads; real camera packets still require venue hardware.
- Tune interpolation and timeout values in `TrackingInputManager`. Verified 2026-05-29 with PlayMode tests for fresh interpolation, stale-frame stopping, and AI/physics suppression for tracking-mode karts.

## 4. Projection Deployment

Status: local calibration tooling and Unity alignment scene ready; physical projector alignment requires venue hardware.

Implemented locally:

- Four-projector config, homography mapping, overlap weights, and alignment grid in `projection/`.
- Unity Projection Alignment scene at `unity/Assets/Scenes/ProjectionAlignment.unity`.
- Windows calibration player at `unity/Builds/Windows/ZIKJ-ProjectionAlignment.exe`.
- Unity Venue Launcher scene at `unity/Assets/Scenes/VenueLauncher.unity` and Windows launcher at `unity/Builds/Windows/ZIKJ-VenueLauncher.exe`.
- Scene/build smoke validation in `ZIKJSmokeTests.RunAll`.

Required venue work:

- Mount projectors and cameras.
- Calibrate projector blend/warp in MadMapper or equivalent. Local four-projector config, homography mapping, overlap weights, Unity alignment scene, and alignment grid verified 2026-05-29.
- Match Unity world coordinates to physical track/projector coordinates. Local `1280 x 720` world-to-projector mapping verified 2026-05-29.
- Validate latency from kart movement to projected visuals.

## 5. Safety And Operations

Status: local software preflight ready; physical safety sign-off still required.

Required venue work:

- Hard-wired emergency stop. Tracked by `ops/preflight.py --require-hardware`.
- Independent speed governor. Tracked by `ops/preflight.py --require-hardware`.
- Operator start/stop controls. Tracked by `ops/preflight.py --require-hardware`.
- Staff race-control flow. Tracked by `ops/preflight.py --require-hardware`.
- Safety checklist and failure-mode testing. Tracked by `ops/preflight.py --require-hardware`.
- Local software preflight checks Unity builds, absence of test assemblies in players, tracking artifacts, projection artifacts, and hardware sign-off state. Verified 2026-05-29 with `python ops\preflight.py --root . --config ops\preflight_config.json`.

## 6. Production Readiness

Final checklist:

- Repeatable all-deployables Windows build. Verified locally 2026-05-30.
- Scene smoke test for all six modes plus Projection Alignment and Venue Launcher. Verified locally 2026-05-30.
- PlayMode runtime test for all six modes, Projection Alignment, Venue Launcher, allocated-port UDP tracking smoke, `5555` UDP tracking smoke, interpolation, timeout, and tracking-mode AI/physics suppression. Verified locally 2026-05-30.
- Local simulator-shaped UDP test and Python simulator packet emission. Verified locally 2026-05-27.
- Python tracking tool tests and generated ArUco markers. Verified locally 2026-05-27.
- Synthetic camera-frame marker detection through tracking service UDP output. Verified locally 2026-05-27.
- Projection config, homography mapping, overlap blending weights, Unity alignment scene, Windows alignment player, Unity launcher, and alignment grid. Verified locally 2026-05-29.
- Operations preflight software checks. Verified locally 2026-05-30 with 32 pass, 6 hardware warnings, 0 fail.
- Venue-gated hardware preflight correctly fails until physical safety sign-offs are complete. Verified locally 2026-05-29.
- Real tracking test.
- Physical projection alignment test.
- Operator/safety acceptance test.
- Venue playtest with controlled speeds before public use.
