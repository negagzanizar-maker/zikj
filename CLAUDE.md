# ZIKJ — Mixed-Reality Karting Prototype

## Current Reality Override

This repo has moved beyond the original Phase 1-only note below. It now includes six playable Unity modes, per-mode Windows build commands, local tracking simulator support, and generated Unity scenes/builds under `unity/`.

## What This Project Is

A next-generation entertainment venue combining real electric karts, projection mapping, and video-game mechanics (Mario Kart-style). The floor of the karting track is projection-mapped with real-time visuals synced to kart positions tracked by overhead cameras.

**This folder is Phase 1 — a pure browser prototype.** No Unity, no hardware, no install. Double-click any HTML file and it runs. Everything here validates game rules and UX before the Unity + hardware build begins.

## Current Phase

**Phase 1 — Browser prototype** (complete)
- All 5 game modes playable in browser
- Shared physics/track engine
- Player app, operator dashboard, venue layout diagram

**Phase 2 — Unity port** (not started — user is currently learning Unity basics)
- User is on a Day 1–7 onboarding plan (Unity Hub + Unity Essentials pathway + first top-down car tutorial)
- After "Day 7 done", start porting Infected to Unity scene

## File Structure

```
zikj/
├── CLAUDE.md               ← this file
├── index.html              ← Hub landing page (entry point)
├── engine.js               ← Shared library: track, physics, AI, render helpers
├── modes/
│   ├── infected.html       ← Battle royale infection mode
│   ├── drift.html          ← Score attack, drift combos + hot zones
│   ├── grand-prix.html     ← 5-lap race with power-ups
│   ├── tron.html           ← Neon trails, cross one = lose a life
│   └── last-one-lit.html   ← Shrinking spotlights in total darkness
├── app/
│   └── index.html          ← Player app (profile, leaderboard, bookings, achievements)
├── operator/
│   └── index.html          ← Staff race-control dashboard
└── layout/
    └── index.html          ← SVG venue layout (projectors, cameras, power)
```

## engine.js — Shared Library

All modes load `engine.js` via `<script src="../engine.js"></script>`. It exposes `window.Engine` with:

- **Track geometry**: `clampToTrack(x,y)`, `centerlineAt(t)`, `WAYPOINTS[80]`, `spawnPositionForIndex(i, total)`
- **Constants**: `W/H`, `STRAIGHT_LEFT/RIGHT`, `TOP_Y/BOTTOM_Y`, `ARC_CY`, `ARC_RADIUS`, `TRACK_HALF_W`, `INNER_R`, `OUTER_R`, `KART_LEN/WID/RADIUS`, `COLORS`, `MAX_SPEED`, etc.
- **Kart class**: `new E.Kart(id, isPlayer, color, spawn)` — has `tags: {}` for mode-specific state
- **Input**: `E.playerInput()` → `{ throttle, steer, action }`
- **AI**: `E.aiInputToward(kart, target, baseThrottle)`, `E.advanceWaypoint(kart)`, `E.nearestKart(self, karts, pred, maxR)`
- **Physics**: `E.stepKart(kart, dt, input)`, `E.resolveOverlaps(karts)`
- **Render**: `E.drawTrack(ctx, opts)`, `E.drawKartBase(ctx, kart, body, outline, width)`, `E.drawTrail(ctx, kart, colorFn, width)`, `E.radialGlow(ctx, x, y, r, colorStop0, colorStop1)`
- **Helpers**: `E.fmtTime(sec)`, `E.pathStadium(ctx, l, r, cy, r)`

Each mode is ~200–350 lines of mode-specific rules + render code on top of the engine.

## Track Geometry

Stadium shape (two straights + two semicircular arcs):
- Top straight: `y = 200`, x from `280` to `1000`
- Bottom straight: `y = 520`, same x range
- Right arc: center `(1000, 360)`, radius `160`
- Left arc: center `(280, 360)`, radius `160`
- Track half-width: `70px` (inner radius `90`, outer radius `230`)
- Canvas: `1280 × 720`

## Game Modes — Rules Summary

### Infected (`modes/infected.html`)
- One random kart starts infected (red aura, red body)
- Proximity `36px` = infection transfers; infector gets `+10% speed` for `5 sec`
- Green safe zones spawn every `12–20 sec`, give `3 sec immunity`, clear infection
- Round `3 min`. Win: last clean kart. If all infected: shortest total infection time wins.

### Drift Arena (`modes/drift.html`)
- Score = `angularVelocity × speed × dt × combo × (hot zone ? 3 : 1)`
- Hot zone cycles around track every `30 sec`; covers `25%` of track length
- Combo multiplier builds when drifting continuously, resets after `1.5 sec` gap
- Drift threshold: `speed > 35% max` AND `|angVel| > 0.4 rad/s`
- Round `3 min`. Highest score wins.

### Grand Prix (`modes/grand-prix.html`)
- `5 laps`, `8 item boxes` on track respawning every `5 sec`
- Items: mushroom `+30% speed 5s`, banana `drop behind (slows hitter 40% for 1.8s)`, shield `absorb 1 hit`, lightning `slow all opponents 35% for 3s`
- Lap detected by crossing start/finish line `(x ≈ 340, top straight)` while moving forward
- First to finish 5 laps wins; fallback: lap count at 3 min

### Tron (`modes/tron.html`)
- Each kart paints a persistent trail (up to `600 segments` per kart)
- Crossing another kart's trail (proximity check per segment): `freeze 2 sec + -1 life`
- Own trail: skip last `60 points` (avoid self-collision just behind kart)
- `3 lives` each. Last alive wins.

### Last One Lit (`modes/last-one-lit.html`)
- Spotlight starts at `180px` radius, shrinks at `12 px/sec`
- Inside another kart's spotlight = shrink `3.5×` faster
- Eliminated at `18px`. Last with light wins. Flicker effect below `40px`.

## Design Rules (Hard Constraints)

1. **Power-ups never affect real kart hardware in ways that increase speed or remove driver control.** They can reduce speed (governor cap), never increase motor output. In the prototype this means `maxSpeedMul` can go above 1.0 for simulation, but real venue implementation caps at operator-set governor.
2. **Game logic is cosmetic + scoring, not control.** Banana peel = score penalty + visual stagger, not actual steering override.
3. **All modes use the same input contract**: `(kart_id, x, y, heading, speed)` from the tracking layer. No mode should need extra hardware.
4. **Safety**: Emergency stop in operator dashboard halts all kart motors via venue hardware. Game server cannot prevent this.

## Real Venue Architecture (for Unity phase)

```
[Karts w/ ArUco markers on roof]
    │
    ▼  2× overhead IP cameras (4K, 120° FoV)
[Python tracking service — OpenCV ArUco]
    │  UDP stream: (kart_id, x, y, heading, t)  ~60Hz
    ▼
[Unity Game Server — LAN, same rules as browser modes]
    │
    ├──▶ [Projection renderer — Unity cam output → 4× short-throw laser projectors]
    │     (MadMapper or Unity native blending)
    │
    └──▶ [Player app / leaderboard — Supabase + Next.js]
```

Tracking → Unity latency budget: `< 100ms` total (cam 16ms + CV 10ms + net 5ms + render 16ms + projector 16ms).

## Tech Stack (current prototype)

- Vanilla HTML/CSS/JS — no frameworks, no build tools, no npm
- Single `engine.js` shared library (IIFE pattern, exposes `window.Engine`)
- All files work over `file://` — no server needed
- Canvas 2D API for all rendering

## Tech Stack (planned — Unity phase)

| Layer | Technology |
|---|---|
| Game engine | Unity 6 LTS (URP) |
| Language | C# |
| Tracking | Python + OpenCV (ArUco) |
| Networking | UDP/OSC for tracking, Mirror for multiplayer |
| App backend | Supabase + Next.js |
| Mobile app | React Native or Flutter |
| Projection blending | MadMapper or Unity custom |

## User Context

- **Founder**: product-focused, 0 game development experience, learning Unity now
- Wants to understand systems deeply, not just use them
- Practical > perfect. Avoid overengineering.
- Currently on Unity learning week 1. When they say "Day 7 done", start Unity port of Infected.
- Language for explanations: clear, minimal jargon, explain WHY not just WHAT

## What's Next (Unity Phase — when user is ready)

1. New Unity 6 project (`karting-game`)
2. Create stadium track in Unity (ProBuilder or simple plane + colliders)
3. One drivable kart: `KartController.cs` (top-down arcade physics matching engine.js)
4. Port Infected rules: `InfectedMode.cs`
5. Port AI: `KartAI.cs` (waypoint follow + state machine)
6. HUD: `InfectedHUD.cs`
7. Polished build → Windows `.exe` for playtesting with friends
8. Then: add remaining 4 modes as separate Unity scenes
9. Then: Python tracking service + UDP listener in Unity
10. Then: projection mapping calibration

## File Naming Convention (future modes)

New modes go in `modes/[mode-name].html`. They must:
- Load `<script src="../engine.js"></script>`
- Expose a `startGame(sim)` function
- Store all mode state in `kart.tags.*`
- Have a `← Hub` link back to `../index.html`
- Support PLAY + SIMULATE modes
