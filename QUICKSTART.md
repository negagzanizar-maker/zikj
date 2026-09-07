# ZIKJ Local Laptop Test — Quick Start

## 🚀 Fastest Way to Test (3 steps)

### Step 1: Open PowerShell in the tracking folder
```powershell
cd c:\Users\Admin\Desktop\zikj\tracking
```

### Step 2: Start the simulator (copy-paste this entire block):
```powershell
python -m venv ..\venv 2>$null
& ..\venv\Scripts\Activate.ps1
pip install -q numpy
python tracking_simulator.py
```

### Step 3: In Unity
1. Press **Play** (▶️ button)
2. Press **T** to see tracking HUD (top-left corner)
3. Watch the **Packets counter** increment

✅ **Done!** You're now receiving simulated tracking data.

---

## 🎮 Play the Game

- **W** = Accelerate
- **S** = Brake  
- **A** = Turn left
- **D** = Turn right
- **T** = Toggle tracking debug HUD

---

## ⚡ Alternative: Double-Click Launcher

Instead of PowerShell, just:
1. Navigate to: `c:\Users\Admin\Desktop\zikj\tracking`
2. Double-click: **`run_test.bat`** (Windows)
   - Or: **`run_test.ps1`** (PowerShell)

The launcher automatically:
- ✓ Creates virtual environment
- ✓ Installs Python packages
- ✓ Starts simulator

---

## 🔧 Customize Test

### More karts:
```powershell
python tracking_simulator.py --karts 12
```

### Slower update rate (for older laptop):
```powershell
python tracking_simulator.py --fps 30
```

### Both:
```powershell
python tracking_simulator.py --karts 3 --fps 30
```

---

## ✅ Verify It's Working

In Unity Console (Ctrl+Shift+C), you should see:
```
[Tracking] Listening on UDP :5555
[STATS] Packets: 60, Rate: 60.0 Hz, Tracked karts: 6, Errors: 0
```

In the debug HUD (press T), you should see:
```
FPS: 60.0
Latency: 2.3ms
Packets: 360
Errors: 0
```

---

## ❌ Not Working?

### Packets not arriving?
1. Is Python simulator running? (PowerShell showing packets)
2. Is tracking HUD visible? (Press T in Unity)
3. Are there network errors? (Check Unity Console)

### Python error?
1. Have Python 3.8+? `python --version`
2. Are you in the `tracking` folder? `cd c:\Users\Admin\Desktop\zikj\tracking`
3. Does `tracking_simulator.py` exist? `dir tracking_simulator.py`

### Still stuck?
See [LOCAL_TEST_GUIDE.md](../LOCAL_TEST_GUIDE.md) for troubleshooting.

---

## 🎬 What Happens

1. **Python simulator** generates fake kart positions (6 karts moving around track)
2. **Sends UDP packets** to Unity at localhost:5555 (60 Hz)
3. **TrackingReceiver.cs** receives packets in Unity
4. **TrackingInputManager.cs** hands each non-player kart from AI to tracking when its first packet arrives
5. **KartController.cs** moves tracked karts to those positions
6. **You** drive the player kart with keyboard
7. **AI karts** keep driving locally if tracking is not running yet

---

## 📊 Expected Results

| Metric | Expected |
|--------|----------|
| Packets/sec | 60 Hz |
| Latency | 2-5 ms |
| FPS in Unity | 60 FPS |
| Errors | 0 |
| Tracked karts | 6 |
| Jitter | None (smooth motion) |

---

## 🚀 Next: Real Venue

Once local test works:
1. **Generate markers:** `python generate_markers.py --count 6 --sheet`
2. **Print & affix** markers to kart roofs
3. **Calibrate cameras:** `python calibrate_camera.py --camera 0`
4. **Run real service:** `python tracking_service.py`
5. **Deploy!** 🎉
