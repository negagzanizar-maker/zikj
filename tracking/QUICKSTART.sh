#!/bin/bash
# ZIKJ Tracking Service — Quick Start

echo "========================================"
echo "ZIKJ Kart Tracking Service Setup"
echo "========================================"
echo ""

# Step 1: Install dependencies
echo "[1/4] Installing Python dependencies..."
cd tracking
pip install -r requirements.txt || exit 1
cd ..

echo ""
echo "[2/4] Configuration checklist:"
echo "  ✓ Check tracking/tracking_config.json"
echo "    - Update camera URLs (RTSP streams)"
echo "    - Verify Unity host/port (127.0.0.1:5555)"
echo ""

echo "[3/4] Generate ArUco markers:"
echo "  Run: python tracking/generate_markers.py --count 6 --sheet"
echo "  Then: Print PDF at 100mm × 100mm, affix to kart roofs"
echo ""

echo "[4/4] Calibrate cameras:"
echo "  Run: python tracking/calibrate_camera.py --camera 0"
echo "       python tracking/calibrate_camera.py --camera 1"
echo ""

echo "========================================"
echo "To Start Tracking:"
echo "========================================"
echo ""
echo "Run tracking service:"
echo "  python tracking/tracking_service.py --config tracking/tracking_config.json"
echo ""
echo "In another terminal, run debug visualizer:"
echo "  python tracking/debug_tracking.py --config tracking/tracking_config.json"
echo ""
echo "In Unity:"
echo "  - Open any game scene"
echo "  - Press T to show tracking HUD"
echo "  - Watch for incoming UDP packets"
echo ""
echo "========================================"
