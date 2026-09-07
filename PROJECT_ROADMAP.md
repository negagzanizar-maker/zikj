# ZIKJ Project Roadmap

Last updated: 2026-09-07

## Read This First

This file is the living memory for this project. Before making any change in this repository, read this file first, then update it after finishing a task or making a meaningful change.

Default working rule: the user wants autonomous execution. Do not stop to ask questions unless the work is genuinely blocked or a choice would be risky and cannot be discovered from the repository.

## Goal

Build ZIKJ into a real 3D, final-product-ready kart venue system:

- Real 3D Unity scenes, not prototype primitives.
- Working venue launcher and all game modes.
- Tracking, projection alignment, operator/preflight tooling, and Windows deployables ready for real venue testing.
- Clear remaining blockers documented until physical hardware and venue checks are signed off.

## Current State

The project has been upgraded from prototype-style Unity scenes toward a real 3D deployable product.

Completed so far:

- Added model-backed real 3D kart content through `unity/Assets/Editor/ZIKJ/ZIKJReal3DSceneAssets.cs`.
- Generated `unity/Assets/Prefabs/ZIKJ/RealKart.prefab` from imported 3D assets.
- Added imported FBX scene dressing through `Real 3D Asset Dressing` roots.
- Added BattleKart-inspired projected battle-race map visuals through `unity/Assets/Scripts/ZIKJ/ProjectedBattleMapBuilder.cs`.
- Wired the projected map layer into every gameplay scene with bright floor roads, lane dashes, bonus boxes, boost arrows, rocket warnings, oil slicks, shield pickups, no-cut markings, and mode-specific projection themes.
- Regenerated the venue launcher, projection alignment scene, and all six gameplay scenes.
- Updated scene creation so game scenes, launcher, and projection alignment apply the real 3D asset pass.
- Updated `ZIKJBuild.BuildWindowsAll()` so builds regenerate scenes first.
- Wired the saved game scenes to use `RealKart.prefab`.
- Built Windows deployables for:
  - `ZIKJ-Infected.exe`
  - `ZIKJ-BattleRoyale.exe`
  - `ZIKJ-GrandPrix.exe`
  - `ZIKJ-DriftArena.exe`
  - `ZIKJ-TronTrails.exe`
  - `ZIKJ-LastOneLit.exe`
  - `ZIKJ-ProjectionAlignment.exe`
  - `ZIKJ-VenueLauncher.exe`
- Improved tracking handoff: non-player karts can run local AI fallback, then switch to tracking mode when live packets arrive.
- Added first-frame tracking enable behavior in `TrackingInputManager`.
- Fixed `LastOneLitMode` spotlight overlap shrink so overlapping lights apply one shrink multiplier instead of stacking multiple times.
- Updated camera/help text for Drift, Tron, and Last One Lit.
- Expanded smoke and PlayMode coverage for real 3D scene assets, AI fallback, and first tracking-frame handoff.
- Updated docs including `README.md`, `unity/README.md`, `QUICKSTART.md`, and `LOCAL_TEST_GUIDE.md`.

## Latest Verification

Git publication verification on 2026-09-07:

- Re-ran the Python suites: tracking 6 passed, projection 8 passed, ops preflight 5 passed (19 total, 0 failures).
- Fetched `origin` and confirmed local `main` matched `origin/main` before committing the accumulated project work.
- Pushed project commit `90bf581` to `origin/main`, confirmed the remote branch hash matched local HEAD, and verified a clean working tree before this publication record update.
- Checked commit candidates for oversized files and common secret patterns; none found. Largest file is approximately 3.5 MB.
- `git diff --check` reported trailing whitespace in Unity-generated scene serialization; retained generated scene content as-is.
- Unity tests and Windows builds were not re-run for this publication task; the results below are historical.

Latest known verification from the real 3D pass:

- Unity smoke tests passed.
- Unity scene regeneration passed.
- Unity PlayMode tests passed: 14 total, 14 passed, 0 failed.
- Unity Windows build passed for all eight deployables.
- Built launcher headless smoke run completed without error lines.
- Python tracking tests passed: 6 tests.
- Python projection tests passed: 8 tests.
- Python ops preflight tests passed: 5 tests.
- Software preflight result: 32 pass, 6 warn, 0 fail.
- Manual launcher run on 2026-05-31 started successfully from `unity/Builds/Windows/ZIKJ-VenueLauncher.exe`.
- Manual launcher run on 2026-06-03 started successfully from `unity/Builds/Windows/ZIKJ-VenueLauncher.exe`; process `ZIKJ-VenueLauncher` was observed running.
- Projected-map pass on 2026-05-31:
  - Scene regeneration passed.
  - Unity smoke tests passed with projected map object checks.
  - Unity PlayMode tests passed: 14 total, 14 passed, 0 failed.
  - Unity Windows build passed for all eight deployables.
  - Manual `ZIKJ-GrandPrix.exe` run started successfully to show the new Battle Race-style projected map.

Useful command to run the product locally:

```powershell
.\unity\Builds\Windows\ZIKJ-VenueLauncher.exe
```

## Missing Or Not Final Yet

The biggest remaining gap is not ordinary software implementation. It is physical venue and safety validation.

Known preflight warnings still requiring real-world signoff:

- `emergency_stop_hardwired`
- `independent_speed_governor`
- `operator_start_stop_controls`
- `staff_race_control_flow`
- `safety_checklist_completed`
- `controlled_speed_playtest_completed`

Other work that can still improve final readiness:

- Add a visual render/screenshot validation pass that opens the real scenes, renders them, saves images, and checks they are not blank.
- Inspect generated screenshots to confirm the scenes look like real 3D environments, not just structurally valid scenes.
- If the user wants the maps pushed further, make multiple truly different playable layouts instead of only changing the projected floor artwork around the current shared circuit.
- Run a real hardware tracking test with live packets, not only simulated/local test coverage.
- Run projection calibration on the actual venue floor/projectors.
- Run operator workflow rehearsal from launcher to mode selection to race start/stop.
- Do a controlled low-speed playtest after safety controls are physically verified.
- Keep docs aligned with any new setup, build, or operator behavior changes.

## Needs Fixing If Seen Again

Watch these areas closely on future changes:

- Scene regeneration must preserve real 3D dressing and `RealKart.prefab` references.
- Tracking fallback must keep AI/local behavior working until real tracking packets arrive.
- First live tracking packet should switch a kart into tracking mode cleanly.
- Last One Lit spotlight overlap should not over-shrink from multiple nearby spotlights.
- Build scripts should keep regenerating scenes before producing Windows deployables.
- Tests should cover mode-specific behavior when shared kart, tracking, or scene-generation code changes.
- Future map work should preserve the projected battle-race layer, especially `ProjectedBattleMapBuilder`, yellow bonus boxes, boost arrows, weapon/hazard projection, and the mode-specific projection themes.

## Update Protocol

Every future work session should do this:

1. Read this file before editing or testing.
2. Make the requested change autonomously when possible.
3. Run the most relevant verification for the change.
4. Update this file with:
   - What changed.
   - What passed or failed.
   - New missing work or blockers.
   - Any change to the goal or final-product readiness.
5. Keep this file concise and current. Move obsolete details out instead of letting it become noisy.

## Recent Change Log

- 2026-09-07: Committed and pushed the accumulated Unity modes, real 3D assets, projected maps, tracking/projection/operator tools, validation artifacts, and documentation to `origin/main` as `90bf581`; remote publication verified. All 19 Python tests passed. Existing ignore rules keep local builds and caches out of Git. Physical venue validation remains pending.
- 2026-06-03: Opened the Windows venue launcher for a visible local simulation run; no code changes were made and remaining physical venue validation blockers are unchanged.
- 2026-05-31: Added BattleKart-inspired projected map layer to gameplay scenes, regenerated scenes, passed Unity smoke tests, passed PlayMode tests, rebuilt all Windows deployables, and launched `ZIKJ-GrandPrix.exe` for a visible manual check.
- 2026-05-31: Launched `ZIKJ-VenueLauncher.exe` from the Windows build folder for a visible manual run check.
- 2026-05-31: Created this living roadmap so future work starts from the same project memory and updates the roadmap after meaningful changes.
