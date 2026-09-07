#!/usr/bin/env python3
"""
ZIKJ Real-Time Kart Tracking Service
Detects ArUco markers on kart roofs via IP cameras, calculates position/heading, sends UDP to Unity.

Usage:
    python tracking_service.py --config config.json
    
Expected packet to Unity (60 Hz):
    {"kart_id": 0, "x": 150.5, "y": 200.3, "heading": 1.57, "speed": 25.5, "t": 1234567.89}
"""

import argparse
import json
import math
import socket
import sys
import threading
import time
from collections import deque
from dataclasses import dataclass, asdict
from datetime import datetime

import cv2
import numpy as np


@dataclass
class KartState:
    """Current state of a kart from tracking."""
    kart_id: int
    x: float
    y: float
    heading: float
    speed: float
    t: float

    def __post_init__(self):
        self.kart_id = int(self.kart_id)
        self.x = float(self.x)
        self.y = float(self.y)
        self.heading = float(self.heading)
        self.speed = float(self.speed)
        self.t = float(self.t)


class TrackingService:
    """Multi-camera ArUco tracking service."""

    def __init__(self, config_path: str):
        """Initialize tracking service from config file."""
        self.config = self._load_config(config_path)
        self.running = False
        self.lock = threading.Lock()

        # UDP socket
        self.udp_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.target_addr = (self.config["unity"]["host"], self.config["unity"]["port"])

        # Camera connections
        self.cameras = {}
        self.frames = {}
        self._connect_cameras()

        # Tracking state
        self.kart_positions = {}  # kart_id -> (x, y, heading, timestamp)
        self.position_history = {}  # kart_id -> deque of (x, y) tuples
        self.max_history = 5

        # ArUco detector
        dictionary_name = self.config.get("aruco", {}).get("dictionary", "5X5_250")
        aruco_dict = cv2.aruco.getPredefinedDictionary(
            self._aruco_dictionary_id(dictionary_name)
        )
        self.detector = cv2.aruco.ArucoDetector(aruco_dict)

        # Calibration (mapping camera coords to track coords)
        self.calibration = self.config.get("calibration", {})

        # Metrics
        self.packets_sent = 0
        self.detection_errors = 0
        self.last_stats_time = time.time()

    def _load_config(self, path: str) -> dict:
        """Load configuration from JSON file."""
        try:
            with open(path, "r") as f:
                return json.load(f)
        except FileNotFoundError:
            print(f"Config file not found: {path}")
            sys.exit(1)
        except json.JSONDecodeError as e:
            print(f"Invalid JSON in config: {e}")
            sys.exit(1)

    def _connect_cameras(self):
        """Connect to all configured cameras."""
        for cam_config in self.config.get("cameras", []):
            cam_id = cam_config["id"]
            url = cam_config["url"]
            timeout = cam_config.get("timeout", 5.0)

            try:
                cap = cv2.VideoCapture(url)
                if not cap.isOpened():
                    raise RuntimeError(f"Failed to open camera {cam_id} at {url}")

                # Set camera properties
                cap.set(cv2.CAP_PROP_BUFFERSIZE, 1)  # Single frame buffer (live, not delayed)
                cap.set(cv2.CAP_PROP_FPS, cam_config.get("fps", 60))

                self.cameras[cam_id] = {"cap": cap, "url": url}
                print(f"[TRACK] Connected to camera {cam_id}: {url}")
            except Exception as e:
                print(f"[ERROR] Failed to connect camera {cam_id}: {e}")

    def _capture_frames(self):
        """Background thread: continuously grab frames from all cameras."""
        while self.running:
            for cam_id, cam_data in self.cameras.items():
                ret, frame = cam_data["cap"].read()
                if ret:
                    with self.lock:
                        self.frames[cam_id] = frame
                else:
                    print(f"[WARN] Failed to read frame from camera {cam_id}")
            time.sleep(0.001)  # 1ms poll interval

    def _detect_markers_in_frame(self, frame, cam_id: int) -> dict:
        """Detect ArUco markers in a single camera frame."""
        if frame is None or frame.size == 0:
            return {}

        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        corners, ids, rejected = self.detector.detectMarkers(gray)

        detections = {}

        if ids is not None:
            for i, marker_id in enumerate(ids.flatten()):
                kart_id = int(marker_id)
                corner = corners[i][0]  # 4 corner points

                # Calculate marker center
                cx = np.mean(corner[:, 0])
                cy = np.mean(corner[:, 1])

                # Calculate heading from marker orientation
                # Vector from corner[0] to corner[1] (top edge of marker)
                p0 = corner[0]
                p1 = corner[1]
                dx = p1[0] - p0[0]
                dy = p1[1] - p0[1]
                angle_rad = math.atan2(dy, dx)

                detections[kart_id] = {
                    "cam_id": cam_id,
                    "cx": cx,
                    "cy": cy,
                    "angle_rad": angle_rad,
                    "corners": corner,
                }

        return detections

    def _triangulate_position(self, detections_by_cam: dict) -> dict:
        """Triangulate kart position from multi-camera detections."""
        triangulated = {}

        for kart_id, cam_detections in detections_by_cam.items():
            if len(cam_detections) == 1:
                # Single camera: use calibration to map to world coords
                det = list(cam_detections.values())[0]
                x, y = self._calibrate_position(det["cx"], det["cy"], det["cam_id"])
                heading = det["angle_rad"]
                triangulated[kart_id] = {
                    "x": x,
                    "y": y,
                    "heading": heading,
                    "confidence": 0.8,
                }
            elif len(cam_detections) >= 2:
                # Multi-camera: triangulate
                x, y, heading = self._triangulate_from_multiple(kart_id, cam_detections)
                triangulated[kart_id] = {
                    "x": x,
                    "y": y,
                    "heading": heading,
                    "confidence": 0.95,
                }

        return triangulated

    def _calibrate_position(self, px: float, py: float, cam_id: int) -> tuple:
        """Map camera pixel coordinates to world track coordinates."""
        cal = self.calibration.get(f"camera_{cam_id}", {})
        if not cal:
            return (px, py)  # Fallback: direct pixel coords

        if "homography" in cal:
            try:
                homography = np.array(cal["homography"], dtype=np.float32)
                if homography.shape == (3, 3):
                    point = np.array([[[px, py]]], dtype=np.float32)
                    world = cv2.perspectiveTransform(point, homography)[0, 0]
                    return (float(world[0]), float(world[1]))
            except Exception as e:
                print(f"[WARN] Invalid homography for camera {cam_id}: {e}")

        # Simple scaling + offset
        scale_x = cal.get("scale_x", 1.0)
        scale_y = cal.get("scale_y", 1.0)
        offset_x = cal.get("offset_x", 0.0)
        offset_y = cal.get("offset_y", 0.0)

        x = px * scale_x + offset_x
        y = py * scale_y + offset_y
        return (x, y)

    @staticmethod
    def _aruco_dictionary_id(name: str) -> int:
        """Return an OpenCV ArUco dictionary id from a config-friendly name."""
        normalized = name.upper()
        if not normalized.startswith("DICT_"):
            normalized = f"DICT_{normalized}"

        if not hasattr(cv2.aruco, normalized):
            raise ValueError(f"Unsupported ArUco dictionary: {name}")

        return getattr(cv2.aruco, normalized)

    def _triangulate_from_multiple(self, kart_id: int, detections: dict) -> tuple:
        """Triangulate position from 2+ camera views."""
        # Placeholder: average the calibrated positions
        xs, ys, angles = [], [], []
        for det in detections.values():
            x, y = self._calibrate_position(det["cx"], det["cy"], det["cam_id"])
            xs.append(x)
            ys.append(y)
            angles.append(det["angle_rad"])

        x = np.mean(xs)
        y = np.mean(ys)
        # Average heading (with circular mean for angles)
        cos_sum = np.sum(np.cos(angles))
        sin_sum = np.sum(np.sin(angles))
        heading = np.arctan2(sin_sum / len(angles), cos_sum / len(angles))

        return (x, y, heading)

    def _calculate_speed(self, kart_id: int, x: float, y: float) -> float:
        """Estimate speed from position history."""
        if kart_id not in self.position_history:
            self.position_history[kart_id] = deque(maxlen=self.max_history)

        history = self.position_history[kart_id]
        history.append((x, y))

        if len(history) < 3:
            return 0.0

        # Speed = average distance / time between samples
        distances = []
        for i in range(1, len(history)):
            dx = history[i][0] - history[i - 1][0]
            dy = history[i][1] - history[i - 1][1]
            distances.append(math.sqrt(dx * dx + dy * dy))

        avg_distance = np.mean(distances)
        dt = 1.0 / self.config["tracking"].get("fps", 60)  # Time per frame
        speed = avg_distance / dt
        return speed

    def _send_kart_state(self, state: KartState):
        """Send kart state to Unity via UDP."""
        try:
            payload = json.dumps(asdict(state))
            self.udp_socket.sendto(payload.encode(), self.target_addr)
            self.packets_sent += 1
        except Exception as e:
            print(f"[ERROR] Failed to send UDP: {e}")

    def _log_stats(self):
        """Periodically log tracking statistics."""
        now = time.time()
        if now - self.last_stats_time > 5.0:  # Every 5 seconds
            elapsed = now - self.last_stats_time
            rate = self.packets_sent / elapsed if elapsed > 0 else 0
            print(
                f"[STATS] Packets: {self.packets_sent}, Rate: {rate:.1f} Hz, "
                f"Tracked karts: {len(self.kart_positions)}, Errors: {self.detection_errors}"
            )
            self.packets_sent = 0
            self.detection_errors = 0
            self.last_stats_time = now

    def run(self):
        """Main tracking loop."""
        self.running = True
        print("[TRACK] Starting tracking service...")

        # Start capture thread
        capture_thread = threading.Thread(target=self._capture_frames, daemon=True)
        capture_thread.start()

        target_fps = self.config["tracking"].get("fps", 60)
        frame_time = 1.0 / target_fps
        last_frame_time = time.time()

        try:
            while self.running:
                # Rate limiting
                now = time.time()
                elapsed = now - last_frame_time
                if elapsed < frame_time:
                    time.sleep(frame_time - elapsed)
                    now = time.time()
                last_frame_time = now

                # Grab latest frames
                with self.lock:
                    frames_to_process = dict(self.frames)

                if not frames_to_process:
                    continue

                # Detect markers in each camera
                all_detections = {}  # cam_id -> {kart_id -> detection}
                for cam_id, frame in frames_to_process.items():
                    detections = self._detect_markers_in_frame(frame, cam_id)
                    all_detections[cam_id] = detections

                # Group detections by kart_id across cameras
                detections_by_kart = {}
                for cam_id, detections in all_detections.items():
                    for kart_id, det in detections.items():
                        if kart_id not in detections_by_kart:
                            detections_by_kart[kart_id] = {}
                        detections_by_kart[kart_id][cam_id] = det

                # Triangulate positions
                try:
                    triangulated = self._triangulate_position(detections_by_kart)
                except Exception as e:
                    print(f"[ERROR] Triangulation failed: {e}")
                    self.detection_errors += 1
                    continue

                # Calculate speeds and send states
                for kart_id, tri in triangulated.items():
                    speed = self._calculate_speed(kart_id, tri["x"], tri["y"])

                    state = KartState(
                        kart_id=kart_id,
                        x=tri["x"],
                        y=tri["y"],
                        heading=tri["heading"],
                        speed=speed,
                        t=now,
                    )

                    self.kart_positions[kart_id] = state
                    self._send_kart_state(state)

                self._log_stats()

        except KeyboardInterrupt:
            print("\n[TRACK] Shutting down...")
        finally:
            self.shutdown()

    def shutdown(self):
        """Clean up resources."""
        self.running = False
        for cam_data in self.cameras.values():
            cam_data["cap"].release()
        self.udp_socket.close()
        print("[TRACK] Tracking service stopped.")


def main():
    parser = argparse.ArgumentParser(description="ZIKJ Kart Tracking Service")
    parser.add_argument(
        "--config",
        type=str,
        default="tracking_config.json",
        help="Path to configuration file",
    )
    parser.add_argument(
        "--debug",
        action="store_true",
        help="Enable debug mode (verbose output)",
    )
    args = parser.parse_args()

    service = TrackingService(args.config)
    service.run()


if __name__ == "__main__":
    main()
