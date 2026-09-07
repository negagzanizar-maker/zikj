import json
import tempfile
import unittest
from pathlib import Path

from preflight import run_preflight


class PreflightTests(unittest.TestCase):
    def test_local_preflight_warns_for_pending_hardware_but_passes_software(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            root = Path(tmp_dir)
            config = self._create_ready_tree(root)

            report = run_preflight(root, config, require_hardware=False)

        self.assertEqual(report["status"], "warn")
        self.assertEqual(report["summary"]["fail"], 0)
        self.assertGreater(report["summary"]["warn"], 0)

    def test_require_hardware_fails_when_safety_signoffs_are_pending(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            root = Path(tmp_dir)
            config = self._create_ready_tree(root)

            report = run_preflight(root, config, require_hardware=True)

        self.assertEqual(report["status"], "fail")
        self.assertGreater(report["summary"]["fail"], 0)
        self.assertTrue(any(check["id"] == "hardware.emergency_stop_hardwired" for check in report["checks"]))

    def test_require_hardware_passes_when_all_signoffs_are_complete(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            root = Path(tmp_dir)
            config = self._create_ready_tree(root)
            config["hardware_signoffs"] = {key: True for key in config["hardware_signoffs"]}

            report = run_preflight(root, config, require_hardware=True)

        self.assertEqual(report["status"], "pass")
        self.assertEqual(report["summary"]["fail"], 0)
        self.assertEqual(report["summary"]["warn"], 0)

    def test_disallowed_test_assembly_fails_preflight(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            root = Path(tmp_dir)
            config = self._create_ready_tree(root)
            managed = root / "unity/Builds/Windows/ZIKJ-Infected_Data/Managed"
            managed.mkdir(parents=True, exist_ok=True)
            (managed / "ZIKJ.PlayMode.Tests.dll").write_bytes(b"test")

            report = run_preflight(root, config, require_hardware=False)

        self.assertEqual(report["status"], "fail")
        self.assertTrue(
            any(
                check["id"].startswith("unity.no_test_assembly.ZIKJ-Infected")
                and check["status"] == "fail"
                for check in report["checks"]
            )
        )

    def test_missing_build_output_fails_preflight(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            root = Path(tmp_dir)
            config = self._create_ready_tree(root)
            (root / "unity/Builds/Windows/ZIKJ-TronTrails.exe").unlink()

            report = run_preflight(root, config, require_hardware=False)

        self.assertEqual(report["status"], "fail")
        self.assertTrue(any(check["id"] == "unity.player.ZIKJ-TronTrails.exe" for check in report["checks"]))

    def _create_ready_tree(self, root: Path) -> dict:
        config = {
            "unity": {
                "build_dir": "unity/Builds/Windows",
                "required_players": [
                    "ZIKJ-Infected.exe",
                    "ZIKJ-BattleRoyale.exe",
                    "ZIKJ-GrandPrix.exe",
                    "ZIKJ-DriftArena.exe",
                    "ZIKJ-TronTrails.exe",
                    "ZIKJ-LastOneLit.exe",
                    "ZIKJ-ProjectionAlignment.exe",
                    "ZIKJ-VenueLauncher.exe",
                ],
                "disallowed_player_assemblies": ["ZIKJ.PlayMode.Tests.dll"],
            },
            "tracking": {
                "config": "tracking/tracking_config.json",
                "markers_dir": "tracking/markers",
                "marker_count": 6,
                "marker_sheet": "tracking/markers_sheet.png",
                "unity_udp_port": 5555,
            },
            "projection": {
                "config": "projection/projection_config.json",
                "alignment_grid": "projection/alignment_grid.json",
                "world_width": 1280,
                "world_height": 720,
            },
            "hardware_signoffs": {
                "emergency_stop_hardwired": False,
                "independent_speed_governor": False,
                "operator_start_stop_controls": False,
                "staff_race_control_flow": False,
                "safety_checklist_completed": False,
                "controlled_speed_playtest_completed": False,
            },
        }

        build_dir = root / config["unity"]["build_dir"]
        build_dir.mkdir(parents=True)
        for player in config["unity"]["required_players"]:
            (build_dir / player).write_bytes(b"exe")
            (build_dir / f"{Path(player).stem}_Data" / "Managed").mkdir(parents=True)

        tracking_dir = root / "tracking"
        tracking_dir.mkdir()
        (tracking_dir / "tracking_config.json").write_text(
            json.dumps({"unity": {"port": 5555}}), encoding="utf-8"
        )
        markers_dir = tracking_dir / "markers"
        markers_dir.mkdir()
        for i in range(6):
            (markers_dir / f"marker_{i:02d}.png").write_bytes(b"png")
        (tracking_dir / "markers_sheet.png").write_bytes(b"sheet")

        projection_dir = root / "projection"
        projection_dir.mkdir()
        (projection_dir / "projection_config.json").write_text(
            json.dumps({"world": {"width": 1280, "height": 720}}), encoding="utf-8"
        )
        (projection_dir / "alignment_grid.json").write_text("[]", encoding="utf-8")

        return config


if __name__ == "__main__":
    unittest.main()
