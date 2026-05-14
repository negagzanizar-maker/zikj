# ZIKJ

Mixed-reality karting project prototype.

This repository currently contains:

- Browser-playable game mode prototypes in `modes/`
- Shared browser rules/physics helper in `engine.js`
- Mock player app, operator dashboard, and venue layout
- Early Unity C# scripts in `unity-scripts/`

## Current Status

This repo is a prototype and planning base. The next production milestone is a Unity vertical slice:

1. One polished `Infected` scene.
2. Real Unity project structure under a Unity project folder.
3. Keyboard/simulator/tracking input abstraction.
4. Operator control path.
5. Repeatable Windows build.

## Safety Note

Game logic must never be the only safety layer. Real kart emergency stop, speed governor, and motor control must be handled by dedicated venue hardware.
