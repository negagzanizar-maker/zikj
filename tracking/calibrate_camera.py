#!/usr/bin/env python3
"""
Camera Calibration Utility for ZIKJ Tracking
Interactive tool to capture calibration points and generate homography matrices.

Usage:
    python calibrate_camera.py --config tracking_config.json --camera 0

Click on known track positions in the live video feed to record calibration points.
- Left click: Add calibration point
- 'c' key: Clear all points
- 's' key: Save calibration
- 'q' key: Quit
"""

import argparse
import json
import sys
import cv2
import numpy as np


class CameraCalibrator:
    """Interactive camera calibration tool."""

    def __init__(self, config_path: str, camera_id: int):
        """Initialize calibrator."""
        self.config_path = config_path
        with open(config_path, "r") as f:
            self.config = json.load(f)

        self.camera_id = camera_id
        self.camera_config = next(
            (c for c in self.config["cameras"] if c["id"] == camera_id), None
        )

        if not self.camera_config:
            print(f"Camera {camera_id} not found in config")
            sys.exit(1)

        # Connect to camera
        self.cap = cv2.VideoCapture(self.camera_config["url"])
        if not self.cap.isOpened():
            print(f"Failed to open camera {camera_id}")
            sys.exit(1)

        self.frame = None
        self.points_camera = []  # (x, y) in image space
        self.points_world = []  # (x, y) in world space

        print(f"Connected to camera {camera_id}")
        print("Instructions:")
        print("  - Left click: Add calibration point (then enter world coords)")
        print("  - 'c': Clear all points")
        print("  - 's': Save calibration")
        print("  - 'q': Quit")

    def _on_mouse_click(self, event, x, y, flags, param):
        """Handle mouse clicks to add calibration points."""
        if event == cv2.EVENT_LBUTTONDOWN:
            self.points_camera.append((x, y))
            print(f"[CAL] Added camera point: ({x}, {y})")
            print("Enter world coordinates (x y): ", end="")
            try:
                world_x, world_y = map(float, input().split())
                self.points_world.append((world_x, world_y))
                print(f"      World point: ({world_x}, {world_y})")
            except ValueError:
                self.points_camera.pop()
                print("[CAL] Invalid input, discarding.")

    def run(self):
        """Main calibration loop."""
        cv2.namedWindow(f"Calibrate Camera {self.camera_id}")
        cv2.setMouseCallback(
            f"Calibrate Camera {self.camera_id}", self._on_mouse_click
        )

        try:
            while True:
                ret, frame = self.cap.read()
                if not ret:
                    print("[ERROR] Failed to read frame")
                    break

                self.frame = frame.copy()

                # Draw calibration points
                for i, (x, y) in enumerate(self.points_camera):
                    cv2.circle(self.frame, (int(x), int(y)), 5, (0, 255, 0), -1)
                    cv2.putText(
                        self.frame,
                        f"{i}",
                        (int(x) + 10, int(y) - 10),
                        cv2.FONT_HERSHEY_SIMPLEX,
                        0.5,
                        (0, 255, 0),
                    )

                # Display instructions
                cv2.putText(
                    self.frame,
                    f"Points: {len(self.points_camera)}  (c)lear (s)ave (q)uit",
                    (10, 30),
                    cv2.FONT_HERSHEY_SIMPLEX,
                    0.7,
                    (0, 255, 0),
                )

                cv2.imshow(f"Calibrate Camera {self.camera_id}", self.frame)

                key = cv2.waitKey(1) & 0xFF
                if key == ord("q"):
                    break
                elif key == ord("c"):
                    self.points_camera.clear()
                    self.points_world.clear()
                    print("[CAL] Cleared all points")
                elif key == ord("s"):
                    self._save_calibration()

        finally:
            cv2.destroyAllWindows()
            self.cap.release()

    def _save_calibration(self):
        """Compute and save calibration parameters."""
        if len(self.points_camera) < 4:
            print("[ERROR] Need at least 4 calibration points")
            return

        points_camera = np.array(self.points_camera, dtype=np.float32)
        points_world = np.array(self.points_world, dtype=np.float32)

        # Compute homography
        H, status = cv2.findHomography(points_camera, points_world)

        if H is None:
            print("[ERROR] Failed to compute homography")
            return

        # Extract simple parameters (approximate)
        scale_x = H[0, 0]
        scale_y = H[1, 1]
        offset_x = H[0, 2]
        offset_y = H[1, 2]

        calibration_data = {
            "scale_x": float(scale_x),
            "scale_y": float(scale_y),
            "offset_x": float(offset_x),
            "offset_y": float(offset_y),
            "rotation_rad": 0.0,
            "homography": H.tolist(),
            "points_camera": self.points_camera,
            "points_world": self.points_world,
        }

        # Update config
        key = f"camera_{self.camera_id}"
        self.config.setdefault("calibration", {})
        self.config["calibration"][key] = calibration_data

        # Save config
        with open(self.config_path, "w") as f:
            json.dump(self.config, f, indent=2)

        print(f"[CAL] Calibration saved for camera {self.camera_id}")
        print(f"      Scale: ({scale_x:.4f}, {scale_y:.4f})")
        print(f"      Offset: ({offset_x:.2f}, {offset_y:.2f})")

        # Verify by reprojecting
        print("\n[CAL] Verification:")
        for i, (cp, wp) in enumerate(zip(self.points_camera, self.points_world)):
            reprojected = cv2.perspectiveTransform(
                np.array([[[cp[0], cp[1]]]], dtype=np.float32), H
            )
            rp = reprojected[0, 0]
            error = np.linalg.norm(rp - np.array(wp))
            print(f"      Point {i}: World {wp} -> Reprojected {rp} (error: {error:.2f})")


def main():
    parser = argparse.ArgumentParser(description="ZIKJ Camera Calibration")
    parser.add_argument("--config", default="tracking_config.json", help="Config file path")
    parser.add_argument("--camera", type=int, required=True, help="Camera ID to calibrate")
    args = parser.parse_args()

    calibrator = CameraCalibrator(args.config, args.camera)
    calibrator.run()


if __name__ == "__main__":
    main()
