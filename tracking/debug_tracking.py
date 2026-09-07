#!/usr/bin/env python3
"""
Debug Visualization for ZIKJ Tracking
Real-time overlay showing detected markers, kart IDs, positions, headings, and confidence.

Usage:
    python debug_tracking.py --config tracking_config.json
"""

import argparse
import json
import math
import sys
import threading
import time
from collections import deque

import cv2
import numpy as np

from tracking_service import TrackingService


class DebugTrackingVisualizer(TrackingService):
    """Extended tracking service with real-time visualization."""

    def __init__(self, config_path: str):
        """Initialize with visualization."""
        super().__init__(config_path)
        self.detection_overlay = {}  # cam_id -> annotated frame
        self.display_lock = threading.Lock()
        self.trails = {}  # kart_id -> deque of (x, y)
        self.trail_length = 30

    def _detect_markers_in_frame(self, frame, cam_id: int) -> dict:
        """Detect markers and create annotated overlay."""
        detections = super()._detect_markers_in_frame(frame, cam_id)

        # Create overlay
        overlay = frame.copy()

        if detections:
            for kart_id, det in detections.items():
                cx, cy = det["cx"], det["cy"]
                angle_rad = det["angle_rad"]

                # Draw marker center
                cv2.circle(overlay, (int(cx), int(cy)), 8, (0, 255, 0), -1)

                # Draw heading arrow
                arrow_len = 50
                arrow_x = int(cx + arrow_len * math.cos(angle_rad))
                arrow_y = int(cy + arrow_len * math.sin(angle_rad))
                cv2.arrowedLine(
                    overlay, (int(cx), int(cy)), (arrow_x, arrow_y), (0, 255, 0), 2
                )

                # Label
                text = f"K{kart_id}"
                cv2.putText(
                    overlay,
                    text,
                    (int(cx) + 15, int(cy) - 10),
                    cv2.FONT_HERSHEY_SIMPLEX,
                    0.6,
                    (0, 255, 0),
                    2,
                )

        with self.display_lock:
            self.detection_overlay[cam_id] = overlay

        return detections

    def _draw_world_view(self) -> np.ndarray:
        """Create a top-down world view with all tracked karts."""
        venue = self.config["venue"]
        width, height = int(venue["track_width"]), int(venue["track_height"])

        # Scale for display
        scale = 0.5
        display_w = int(width * scale)
        display_h = int(height * scale)

        canvas = np.zeros((display_h, display_w, 3), dtype=np.uint8)

        # Draw track border (simple rectangle)
        cv2.rectangle(canvas, (10, 10), (display_w - 10, display_h - 10), (100, 100, 100), 2)

        # Draw karts
        for kart_id, state in self.kart_positions.items():
            x = int(state.x * scale)
            y = int(state.y * scale)

            # Clamp to canvas
            x = max(10, min(x, display_w - 10))
            y = max(10, min(y, display_h - 10))

            # Draw kart as circle
            color = (0, 255, 255) if kart_id % 2 == 0 else (255, 0, 255)
            cv2.circle(canvas, (x, y), 5, color, -1)

            # Draw heading
            heading = state.heading
            arrow_len = 20
            arrow_x = int(x + arrow_len * math.cos(heading))
            arrow_y = int(y + arrow_len * math.sin(heading))
            cv2.arrowedLine(canvas, (x, y), (arrow_x, arrow_y), color, 2)

            # Label
            cv2.putText(
                canvas,
                f"{kart_id}",
                (x + 8, y - 8),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.4,
                color,
                1,
            )

            # Speed indicator
            speed_text = f"{state.speed:.1f} u/s"
            cv2.putText(
                canvas,
                speed_text,
                (x + 8, y + 12),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.3,
                color,
                1,
            )

        cv2.putText(
            canvas,
            f"World View - {len(self.kart_positions)} karts",
            (10, display_h - 10),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.5,
            (0, 255, 0),
            1,
        )

        return canvas

    def run(self):
        """Run with real-time visualization."""
        self.running = True
        print("[VIZ] Starting tracking with visualization...")

        # Start capture thread
        capture_thread = threading.Thread(target=self._capture_frames, daemon=True)
        capture_thread.start()

        target_fps = self.config["tracking"].get("fps", 60)
        frame_time = 1.0 / target_fps
        last_frame_time = time.time()

        try:
            while self.running:
                now = time.time()
                elapsed = now - last_frame_time
                if elapsed < frame_time:
                    time.sleep(frame_time - elapsed)
                    now = time.time()
                last_frame_time = now

                # Grab frames
                with self.display_lock:
                    frames_to_process = dict(self.frames)

                if not frames_to_process:
                    continue

                # Detect markers
                all_detections = {}
                for cam_id, frame in frames_to_process.items():
                    detections = self._detect_markers_in_frame(frame, cam_id)
                    all_detections[cam_id] = detections

                # Group by kart
                detections_by_kart = {}
                for cam_id, detections in all_detections.items():
                    for kart_id, det in detections.items():
                        if kart_id not in detections_by_kart:
                            detections_by_kart[kart_id] = {}
                        detections_by_kart[kart_id][cam_id] = det

                # Triangulate
                try:
                    triangulated = self._triangulate_position(detections_by_kart)
                except Exception as e:
                    print(f"[ERROR] Triangulation failed: {e}")
                    self.detection_errors += 1
                    continue

                # Calculate speeds
                for kart_id, tri in triangulated.items():
                    speed = self._calculate_speed(kart_id, tri["x"], tri["y"])
                    state = type("KartState", (), {
                        "kart_id": kart_id,
                        "x": tri["x"],
                        "y": tri["y"],
                        "heading": tri["heading"],
                        "speed": speed,
                        "timestamp": now,
                    })()
                    self.kart_positions[kart_id] = state
                    self._send_kart_state(state)

                # Display
                self._display_frames()
                self._log_stats()

                # Quit on 'q'
                key = cv2.waitKey(1) & 0xFF
                if key == ord("q"):
                    break

        except KeyboardInterrupt:
            print("\n[VIZ] Shutting down...")
        finally:
            cv2.destroyAllWindows()
            self.shutdown()

    def _display_frames(self):
        """Display camera overlays and world view."""
        with self.display_lock:
            # Show camera overlays
            for cam_id, overlay in self.detection_overlay.items():
                # Resize for display
                display_size = (800, 600)
                resized = cv2.resize(overlay, display_size)
                cv2.imshow(f"Camera {cam_id} Overlay", resized)

            # Show world view
            world_view = self._draw_world_view()
            cv2.imshow("World View", world_view)


def main():
    parser = argparse.ArgumentParser(description="ZIKJ Tracking Debug Visualizer")
    parser.add_argument("--config", default="tracking_config.json", help="Config file")
    args = parser.parse_args()

    visualizer = DebugTrackingVisualizer(args.config)
    visualizer.run()


if __name__ == "__main__":
    main()
