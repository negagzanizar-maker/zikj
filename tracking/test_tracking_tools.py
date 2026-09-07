import json
import socket
import tempfile
import unittest
from dataclasses import asdict
from pathlib import Path

import cv2
import numpy as np

from generate_markers import generate_markers
from tracking_service import KartState, TrackingService
from tracking_simulator import KartState as SimulatorKartState


class TrackingToolTests(unittest.TestCase):
    def test_default_config_matches_unity_contract(self):
        config = json.loads(Path(__file__).with_name("tracking_config.json").read_text())

        self.assertEqual(config["unity"]["protocol"], "udp")
        self.assertEqual(config["unity"]["host"], "127.0.0.1")
        self.assertEqual(config["unity"]["port"], 5555)
        self.assertEqual(config["aruco"]["dictionary"], "5X5_250")
        self.assertGreater(config["aruco"]["marker_size_mm"], 0)

        camera_ids = [camera["id"] for camera in config["cameras"]]
        self.assertEqual(len(camera_ids), len(set(camera_ids)))
        for camera_id in camera_ids:
            self.assertIn(f"camera_{camera_id}", config["calibration"])

    def test_packet_dataclasses_use_t_schema(self):
        service_packet = asdict(KartState(1, 10.0, 20.0, 0.5, 3.0, 123.0))
        simulator_packet = asdict(SimulatorKartState(1, 10.0, 20.0, 0.5, 3.0, 123.0))

        for packet in (service_packet, simulator_packet):
            self.assertIn("t", packet)
            self.assertNotIn("timestamp", packet)
            self.assertEqual({"kart_id", "x", "y", "heading", "speed", "t"}, set(packet))

    def test_tracking_service_sends_unity_json_packet(self):
        receiver = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        receiver.bind(("127.0.0.1", 0))
        receiver.settimeout(1.0)
        port = receiver.getsockname()[1]

        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = Path(tmp_dir) / "tracking_config.json"
            config_path.write_text(json.dumps(self._service_config(port)))

            service = TrackingService(str(config_path))
            try:
                service._send_kart_state(KartState(2, 100.0, 220.0, 1.25, 12.5, 456.0))
                data, _ = receiver.recvfrom(2048)
                packet = json.loads(data.decode("utf-8"))
            finally:
                service.shutdown()
                receiver.close()

        self.assertEqual(packet["kart_id"], 2)
        self.assertEqual(packet["x"], 100.0)
        self.assertEqual(packet["y"], 220.0)
        self.assertEqual(packet["heading"], 1.25)
        self.assertEqual(packet["speed"], 12.5)
        self.assertEqual(packet["t"], 456.0)
        self.assertNotIn("timestamp", packet)

    def test_tracking_service_uses_homography_before_linear_fallback(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = Path(tmp_dir) / "tracking_config.json"
            config = self._service_config(5555)
            config["calibration"] = {
                "camera_0": {
                    "scale_x": 100.0,
                    "scale_y": 100.0,
                    "offset_x": 100.0,
                    "offset_y": 100.0,
                    "homography": [
                        [2.0, 0.0, 10.0],
                        [0.0, 3.0, 20.0],
                        [0.0, 0.0, 1.0],
                    ],
                },
                "camera_1": {
                    "scale_x": 0.5,
                    "scale_y": 0.25,
                    "offset_x": 7.0,
                    "offset_y": 11.0,
                },
            }
            config_path.write_text(json.dumps(config))

            service = TrackingService(str(config_path))
            try:
                homography_pos = service._calibrate_position(5.0, 7.0, 0)
                linear_pos = service._calibrate_position(10.0, 20.0, 1)
            finally:
                service.shutdown()

        self.assertEqual(homography_pos, (20.0, 41.0))
        self.assertEqual(linear_pos, (12.0, 16.0))

    def test_generate_markers_outputs_detectable_pngs(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            generate_markers(2, 120, tmp_dir)

            detected_ids = set()
            aruco_dict = cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_5X5_250)
            detector = cv2.aruco.ArucoDetector(aruco_dict)

            for marker_id in range(2):
                marker_path = Path(tmp_dir) / f"marker_{marker_id:02d}.png"
                self.assertTrue(marker_path.exists())

                image = cv2.imread(str(marker_path), cv2.IMREAD_GRAYSCALE)
                self.assertIsNotNone(image)
                corners, ids, _ = detector.detectMarkers(image)
                self.assertGreater(len(corners), 0)
                detected_ids.update(int(value) for value in ids.flatten())

        self.assertEqual(detected_ids, {0, 1})

    def test_synthetic_camera_frame_detects_calibrates_and_sends_packet(self):
        receiver = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        receiver.bind(("127.0.0.1", 0))
        receiver.settimeout(1.0)
        port = receiver.getsockname()[1]

        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = Path(tmp_dir) / "tracking_config.json"
            config = self._service_config(port)
            config["calibration"] = {
                "camera_0": {
                    "scale_x": 0.5,
                    "scale_y": 0.25,
                    "offset_x": 10.0,
                    "offset_y": 20.0,
                }
            }
            config_path.write_text(json.dumps(config))

            service = TrackingService(str(config_path))
            try:
                frame = self._synthetic_marker_frame(marker_id=4, top_left=(180, 120), marker_size=160)
                detections = service._detect_markers_in_frame(frame, cam_id=0)
                self.assertIn(4, detections)

                detection = detections[4]
                expected_cx = 180 + 79.5
                expected_cy = 120 + 79.5
                self.assertAlmostEqual(detection["cx"], expected_cx, delta=1.5)
                self.assertAlmostEqual(detection["cy"], expected_cy, delta=1.5)
                self.assertAlmostEqual(detection["angle_rad"], 0.0, delta=0.05)

                triangulated = service._triangulate_position({4: {0: detection}})
                self.assertIn(4, triangulated)

                state_data = triangulated[4]
                expected_x = expected_cx * 0.5 + 10.0
                expected_y = expected_cy * 0.25 + 20.0
                self.assertAlmostEqual(state_data["x"], expected_x, delta=1.0)
                self.assertAlmostEqual(state_data["y"], expected_y, delta=1.0)

                service._send_kart_state(
                    KartState(
                        kart_id=4,
                        x=state_data["x"],
                        y=state_data["y"],
                        heading=state_data["heading"],
                        speed=service._calculate_speed(4, state_data["x"], state_data["y"]),
                        t=789.0,
                    )
                )
                data, _ = receiver.recvfrom(2048)
                packet = json.loads(data.decode("utf-8"))
            finally:
                service.shutdown()
                receiver.close()

        self.assertEqual(packet["kart_id"], 4)
        self.assertAlmostEqual(packet["x"], expected_x, delta=1.0)
        self.assertAlmostEqual(packet["y"], expected_y, delta=1.0)
        self.assertAlmostEqual(packet["heading"], 0.0, delta=0.05)
        self.assertEqual(packet["t"], 789.0)
        self.assertNotIn("timestamp", packet)

    def _service_config(self, port):
        return {
            "tracking": {"fps": 60, "max_latency_ms": 100, "debug": False},
            "cameras": [],
            "aruco": {"dictionary": "5X5_250", "marker_size_mm": 100},
            "calibration": {},
            "kart_mapping": {"0": "Blue", "1": "Yellow"},
            "unity": {"host": "127.0.0.1", "port": port, "protocol": "udp"},
            "venue": {"track_width": 1280, "track_height": 720, "name": "Test"},
        }

    def _synthetic_marker_frame(self, marker_id, top_left, marker_size):
        frame = np.full((480, 640, 3), 255, dtype=np.uint8)
        aruco_dict = cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_5X5_250)
        marker = cv2.aruco.generateImageMarker(aruco_dict, id=marker_id, sidePixels=marker_size)
        marker_bgr = cv2.cvtColor(marker, cv2.COLOR_GRAY2BGR)

        x, y = top_left
        frame[y : y + marker_size, x : x + marker_size] = marker_bgr
        return frame


if __name__ == "__main__":
    unittest.main()
