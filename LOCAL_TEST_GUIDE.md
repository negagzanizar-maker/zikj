# 🚀 ZIKJ Local Laptop Test Guide

Run the full tracking + Unity game system on your laptop **without any cameras**.

## System Overview

```
[Tracking Simulator] (Python, simulates 6 karts moving around track)
         ↓
    [UDP 5555]
         ↓
  [Unity Game Scene]
    ↓         ↓
  Player   AI Karts (receive tracking data)
  (Keyboard input)
```

---

## Prerequisites

✅ **Python 3.8+** installed  
✅ **Unity 6 LTS** with ZIKJ project open  
✅ One of the game scenes loaded (Infected, BattleRoyale, or GrandPrix)

---

## Step 1: Set Up Python Environment

### Open PowerShell and navigate to project:

```powershell
cd c:\Users\Admin\Desktop\zikj
```

### Create Python virtual environment:

```powershell
python -m venv venv
```

### Activate environment:

```powershell
.\venv\Scripts\Activate.ps1
```

### Install dependencies:

```powershell
pip install numpy
```

*Note: We don't need OpenCV for the simulator, just NumPy*

---

## Step 2: Start Tracking Simulator

### In the same PowerShell terminal:

```powershell
cd tracking
python tracking_simulator.py --karts 6 --fps 60
```

### Expected output:

```
[SIM] Starting tracking simulator...
[SIM] Simulating 6 karts at 60 Hz
[SIM] Sending to 127.0.0.1:5555
[SIM] Open Unity scene and press T to see packets

[SIM] Packets sent: 360, Rate: 60.0 Hz, Avg per frame: 60.0
```

✅ **Leave this running** — it continuously sends simulated kart positions via UDP

---

## Step 3: Open Unity Scene

### In Unity Editor:

1. Open any game scene:
   - `Assets/Scenes/InfectedArena.unity`
   - `Assets/Scenes/BattleRoyaleArena.unity`
   - `Assets/Scenes/GrandPrixArena.unity`

2. **Press Play** (▶️ button)

3. **Press T** to show tracking debug HUD (top-left corner)

### Expected HUD:

```
═══ TRACKING STATUS ═══
FPS: 60.0
Latency: 2.3ms
Packets: 360
Errors: 0
Tracked Karts: 6
Interpolation: ON
```

✅ **You should see packets arriving** (Packets counter incrementing)

---

## Step 4: Play the Game

### Controls:

- **W/Up Arrow** — Accelerate (player kart only)
- **S/Down Arrow** — Brake
- **A/Left Arrow** — Steer left
- **D/Right Arrow** — Steer right

### What to observe:

1. **Player kart (you)** — Moves with keyboard input
2. **AI karts (1-5)** — Move automatically, then hand over to tracking as packets arrive
3. **Tracking HUD (T)** — Shows latency, packet count
4. **Game HUD** — Infection status / hearts / lap count depending on mode

---

## Troubleshooting

### ❌ Packets not arriving in Unity

**Problem:** HUD shows `Packets: 0` (not incrementing)

**Solution:**
1. Check PowerShell: Is simulator running and showing `Packets sent: 360`?
2. Check firewall: Allow Python through Windows Defender
3. Check Unity: `TrackingReceiver.cs` has `autoStart = true`
4. Check port: No other app using UDP 5555

**Verify port is open:**
```powershell
netstat -an | findstr "5555"
```
Should show: `UDP    127.0.0.1:5555`

---

### ❌ Simulator not running

**Problem:** Python error when starting simulator

**Solution:**
1. Check Python installed: `python --version` (should be 3.8+)
2. Check directory: `cd tracking` before running
3. Check file exists: `dir tracking_simulator.py`
4. Try again: `python tracking_simulator.py`

---

### ❌ AI karts not moving

**Problem:** Player kart moves, but AI karts frozen

**Solution:**
1. Check if AI karts have `KartAI.cs` component
2. Check tracking HUD (T): if packets are arriving, non-player karts should switch into tracking mode
3. Check `TrackingInputManager.cs` is present in the scene
4. Verify GameManager registered non-player karts with TrackingInputManager

---

## What Each Scene Does

### Infected Mode
- 1 kart starts red (infected)
- Touch a red kart = you get infected
- Green safe zones spawn to grant immunity
- **3-minute round** → last clean kart wins

### Battle Royale
- All karts start with 3 hearts
- Collisions deal damage
- Last kart standing wins
- **Real-time heart HUD** shows health

### Grand Prix
- 3-lap race
- Item boxes spawn (mushroom boost, banana peel, shield, shock)
- **First to finish 3 laps wins**
- Live lap counter + placements

---

## Performance Tips

### Laptop gets hot?
- Reduce player count: `--karts 3`
- Lower FPS: `--fps 30`
- Disable unity shadows: Project Settings → Quality

### Latency feels high?
- Check HUD: Should show < 5ms (all local)
- Close other apps (browsers, Discord)
- Make sure nothing else uses UDP 5555

### Karts jitter?
- Already tuned for laptop, but if bad:
  - In `TrackingInputManager.cs`, increase `interpolationDampTime`
  - Lower value = more snappy, higher = smoother

---

## Advanced: Modify Simulation

### Make karts faster:
Edit `tracking_simulator.py`, line 77:
```python
"speed": 10.0 + i * 2,  # ← Increase multiplier
```

### Add more karts:
```powershell
python tracking_simulator.py --karts 12
```

### Change output rate:
```powershell
python tracking_simulator.py --fps 30  # Slower for slower laptop
```

---

## Next Steps

### ✅ If test works:
1. Try all 3 game modes
2. Verify all karts respond to tracking
3. Check latency stays < 10ms
4. Confirm no packet loss (Errors: 0)

### 🚀 If test works great:
1. Modify `tracking_config.json` with real camera URLs
2. Calibrate cameras with `calibrate_camera.py`
3. Run real tracking service: `python tracking_service.py`
4. Deploy to venue!

---

## File Summary

| File | Purpose | Status |
|------|---------|--------|
| `tracking_simulator.py` | Simulates kart tracking data | ✅ Ready |
| `tracking_config.json` | Camera configuration | For real venue |
| `tracking_service.py` | Real camera tracking | For real venue |
| `debug_tracking.py` | Visualization | For real venue |

---

## One-Liner Test (Copy-Paste)

```powershell
cd c:\Users\Admin\Desktop\zikj\tracking; python tracking_simulator.py
```

Then in Unity: Press Play → Press T → See packets arrive ✅

---

## Questions?

Check console for errors:
- **Python:** Shows in PowerShell terminal
- **Unity:** Shows in bottom Console panel (Ctrl+Shift+C)

Common issues in Console:
- `[Tracking] Failed to receive` → Firewall blocking
- `Port already in use` → Change to 5556 in TrackingReceiver.cs
- `Null reference in TrackingInputManager` → Not registered with GameManager
