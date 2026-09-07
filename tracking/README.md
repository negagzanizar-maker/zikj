# ZIKJ Kart Tracking Service

Real-time ArUco marker detection and multi-camera triangulation for karting arena position tracking.

## Overview

The tracking service detects ArUco markers on kart roofs via IP cameras, calculates real-world position and heading, and streams updates to Unity via UDP at **60 Hz** with **< 100ms latency**.

## System Architecture

```
[IP Cameras 1, 2]  (120 FPS, 1080p)
        ↓
[OpenCV ArUco Detection]
        ↓
[Multi-camera Triangulation]
        ↓
[Speed Estimation]
        ↓
[UDP Stream → Unity] (60 Hz JSON packets)
```

**Packet Format:**
```json
{"kart_id": 0, "x": 150.5, "y": 200.3, "heading": 1.57, "speed": 25.5, "t": 1234567.89}
```

## Installation

### 1. Install Dependencies

```bash
cd tracking
pip install -r requirements.txt
```

**Requirements:**
- Python 3.8+
- OpenCV 4.8+ (with contrib for ArUco)
- NumPy 1.24+

### 2. Configure Cameras

Edit `tracking_config.json`:

```json
{
  "cameras": [
    {
      "id": 0,
      "url": "rtsp://192.168.1.101:554/stream",
      "fps": 120,
      "resolution": [1920, 1080]
    },
    {
      "id": 1,
      "url": "rtsp://192.168.1.102:554/stream",
      "fps": 120,
      "resolution": [1920, 1080]
    }
  ],
  "unity": {
    "host": "127.0.0.1",
    "port": 5555
  }
}
```

**Camera URL Examples:**
- Hikvision: `rtsp://admin:password@192.168.1.101:554/Streaming/Channels/101`
- Axis: `rtsp://root:password@192.168.1.101:554/axis-media/media.amp`
- Generic IP Cam: `rtsp://192.168.1.101:554/stream`

### 3. Calibrate Cameras

Before running, calibrate each camera to map image pixels → world track coordinates.

```bash
python calibrate_camera.py --config tracking_config.json --camera 0
```

**Calibration Process:**
1. Live video stream appears
2. Click on known track positions (e.g., center line markers, corners)
3. Enter world coordinates (x, y) for each point
4. Press **'s'** to save calibration (generates homography matrix)
5. Repeat for camera 1

**Example World Coordinates (from browser prototype):**
- Top-left track corner: (280, 200)
- Top-right track corner: (1000, 200)
- Bottom-right track corner: (1000, 520)
- Bottom-left track corner: (280, 520)

## Usage

### Run Tracking Service

```bash
python tracking_service.py --config tracking_config.json
```

**Output:**
```
[TRACK] Connected to camera 0: rtsp://192.168.1.101:554/stream
[TRACK] Connected to camera 1: rtsp://192.168.1.102:554/stream
[TRACK] Starting tracking service...
[STATS] Packets: 60, Rate: 60.0 Hz, Tracked karts: 6, Errors: 0
```

### Run Debug Visualizer

Real-time visualization of marker detection and world view:

```bash
python debug_tracking.py --config tracking_config.json
```

**Display Windows:**
1. **Camera 0/1 Overlays** — Shows detected markers with IDs and heading arrows
2. **World View** — Top-down view of all tracked karts with speeds

**Controls:**
- Press **'q'** to quit
- Monitor latency and packet rate in console

### Run Camera Calibration

```bash
python calibrate_camera.py --config tracking_config.json --camera 0
```

**Calibration Controls:**
- **Left-click**: Add calibration point
- **Type world coords**: Enter x, y coordinates
- **'c' key**: Clear all points
- **'s' key**: Save and compute homography
- **'q' key**: Quit

## Configuration Reference

### Tracking Parameters
```json
{
  "tracking": {
    "fps": 60,                    // Output rate to Unity
    "max_latency_ms": 100         // Warning threshold
  }
}
```

### Camera Setup
```json
{
  "cameras": [
    {
      "id": 0,                    // Unique camera ID
      "url": "rtsp://...",        // Stream URL
      "fps": 120,                 // Input camera FPS
      "timeout": 5.0,             // Connection timeout
      "resolution": [1920, 1080]
    }
  ]
}
```

### ArUco Markers
```json
{
  "aruco": {
    "dictionary": "5X5_250",      // 5×5 bit 250-marker dictionary
    "marker_size_mm": 100         // Physical marker size
  }
}
```

### Calibration
```json
{
  "calibration": {
    "camera_0": {
      "scale_x": 0.25,            // Pixel-to-world scale (x-axis)
      "scale_y": 0.25,            // Pixel-to-world scale (y-axis)
      "offset_x": 0,              // World offset
      "offset_y": 0,
      "rotation_rad": 0.0,        // Rotation correction
      "homography": [[...], ...]  // Full homography matrix (optional)
    }
  }
}
```

### Unity Target
```json
{
  "unity": {
    "host": "127.0.0.1",          // Unity machine IP
    "port": 5555,                 // UDP listen port
    "protocol": "udp"
  }
}
```

## Troubleshooting

### No markers detected
- Check ArUco marker visibility and contrast
- Verify marker IDs match config
- Adjust camera exposure/focus
- Run debug visualizer to see detected corners

### High latency (> 100ms)
- Check network RTT (ping)
- Verify camera frame rate (should be 120 FPS minimum)
- Reduce camera resolution if CPU-bound
- Check for frame drops in console logs

### Karts jittering
- Add more calibration points (8-12 minimum)
- Verify homography error in calibration output
- Increase interpolation damp time in TrackingInputManager
- Check for rolling shutter distortion

### UDP packets not reaching Unity
- Verify `unity.host` and `.port` in config
- Check firewall (allow UDP 5555)
- Confirm TrackingReceiver.cs is running (`autoStart = true`)
- Run `netstat -an` to verify port is open

### Poor depth accuracy
- Add second camera for stereo triangulation
- Increase physical marker size
- Place cameras at wider baseline (> 2m)
- Verify camera calibration matrices

## Performance Notes

- **60 Hz output**: 1 kart state per 16.7 ms
- **< 100 ms budget**: 16ms camera + 10ms CV + 5ms network + 16ms render + 16ms projector
- **Per-frame processing**: ArUco detection (~5ms), triangulation (~2ms), UDP send (~1ms)
- **Optimal setup**: 2× 4K cameras at 120 FPS (over-capture for latency margin)

## ArUco Marker Setup

1. **Generate markers**:
   ```bash
   python generate_markers.py --count 6 --size 600 --output markers
   python generate_markers.py --count 6 --size 600 --output markers_sheet.pdf --sheet
   ```

2. **Print markers** at 100mm × 100mm on white paper
3. **Affix to kart roofs** in visible orientation
4. **Verify in debug visualizer** before running venue

Generated local artifacts:

- `tracking/markers/marker_00.png` through `marker_05.png`
- `tracking/markers_sheet.png` when `reportlab` is not installed, or `tracking/markers_sheet.pdf` when it is installed

## Local Verification

Run the no-camera tracking checks:

```bash
python -m unittest discover -s tracking -p "test_*.py" -v
```

This verifies config shape, `t`-based Unity packet schema, UDP packet emission, homography calibration mapping, generated marker detectability, and a synthetic camera frame flowing through marker detection, calibration, and UDP packet output.

## Network Setup

For real hardware deployment:

```
[Tracking PC — 192.168.1.10]
    ↓ UDP broadcast
[Network switch] ← [Cameras 192.168.1.101, 102]
    ↓ UDP unicast
[Unity PC — 192.168.1.20]
```

**Latency Budget (per link):**
- Network RTT: 5-10 ms
- Camera capture: 16 ms (60 FPS) / 8 ms (120 FPS)
- Python processing: 5-10 ms
- UDP transit: 1 ms
- Unity receive + render: 16 ms

## Future Enhancements

- [ ] Kalman filtering for smoother tracking
- [ ] IMU fusion (heading correction)
- [ ] Dynamic marker detection (multiple IDs per kart)
- [ ] Occlusion handling (predict position when hidden)
- [ ] Multi-vehicle collision detection
- [ ] Real-time latency compensation

## References

- OpenCV ArUco: https://docs.opencv.org/master/d5/dae/tutorial_aruco_detection.html
- 5×5_250 dictionary: 250 unique IDs, robust detection
- Homography: https://en.wikipedia.org/wiki/Homography_(computer_vision)
