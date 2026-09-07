#!/usr/bin/env python3
"""ZIKJ operations preflight checks.

This script intentionally separates local software readiness from physical
venue safety sign-off. Local checks can pass on a laptop; hardware sign-offs
must be explicitly required for venue acceptance.
"""

import argparse
import json
import sys
from dataclasses import dataclass, asdict
from pathlib import Path


@dataclass
class Check:
    id: str
    status: str
    message: str


def load_config(path: str | Path) -> dict:
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def run_preflight(root: str | Path, config: dict, require_hardware: bool = False) -> dict:
    root = Path(root)
    checks: list[Check] = []

    _check_unity_builds(root, config.get("unity", {}), checks)
    _check_tracking(root, config.get("tracking", {}), checks)
    _check_projection(root, config.get("projection", {}), checks)
    _check_hardware_signoffs(config.get("hardware_signoffs", {}), checks, require_hardware)

    status = "pass"
    if any(check.status == "fail" for check in checks):
        status = "fail"
    elif any(check.status == "warn" for check in checks):
        status = "warn"

    return {
        "status": status,
        "checks": [asdict(check) for check in checks],
        "summary": {
            "pass": sum(1 for check in checks if check.status == "pass"),
            "warn": sum(1 for check in checks if check.status == "warn"),
            "fail": sum(1 for check in checks if check.status == "fail"),
        },
    }


def _check_unity_builds(root: Path, unity_config: dict, checks: list[Check]) -> None:
    build_dir = root / unity_config.get("build_dir", "")
    required_players = unity_config.get("required_players", [])
    disallowed = unity_config.get("disallowed_player_assemblies", [])

    if not build_dir.exists():
        checks.append(Check("unity.build_dir", "fail", f"Missing Unity build directory: {build_dir}"))
        return
    checks.append(Check("unity.build_dir", "pass", f"Found Unity build directory: {build_dir}"))

    for player in required_players:
        player_path = build_dir / player
        status = "pass" if player_path.exists() and player_path.stat().st_size > 0 else "fail"
        checks.append(Check(f"unity.player.{player}", status, _exists_message(player_path, "player executable")))

        data_dir = build_dir / f"{Path(player).stem}_Data"
        status = "pass" if data_dir.exists() else "fail"
        checks.append(Check(f"unity.data.{Path(player).stem}", status, _exists_message(data_dir, "player data folder")))

        managed_dir = data_dir / "Managed"
        for assembly in disallowed:
            assembly_path = managed_dir / assembly
            status = "fail" if assembly_path.exists() else "pass"
            message = (
                f"Disallowed test assembly present: {assembly_path}"
                if assembly_path.exists()
                else f"Disallowed test assembly absent: {assembly}"
            )
            checks.append(Check(f"unity.no_test_assembly.{Path(player).stem}.{assembly}", status, message))


def _check_tracking(root: Path, tracking_config: dict, checks: list[Check]) -> None:
    config_path = root / tracking_config.get("config", "")
    markers_dir = root / tracking_config.get("markers_dir", "")
    marker_sheet = root / tracking_config.get("marker_sheet", "")
    marker_count = int(tracking_config.get("marker_count", 0))

    checks.append(Check("tracking.config", "pass" if config_path.exists() else "fail", _exists_message(config_path, "tracking config")))

    if config_path.exists():
        try:
            data = json.loads(config_path.read_text(encoding="utf-8"))
            port = data.get("unity", {}).get("port")
            expected = tracking_config.get("unity_udp_port")
            status = "pass" if port == expected else "fail"
            checks.append(Check("tracking.unity_port", status, f"Unity tracking UDP port is {port}; expected {expected}"))
        except json.JSONDecodeError as exc:
            checks.append(Check("tracking.config_json", "fail", f"Invalid tracking config JSON: {exc}"))

    markers = sorted(markers_dir.glob("marker_*.png")) if markers_dir.exists() else []
    status = "pass" if len(markers) == marker_count else "fail"
    checks.append(Check("tracking.markers", status, f"Found {len(markers)} marker PNGs; expected {marker_count}"))
    checks.append(Check("tracking.marker_sheet", "pass" if marker_sheet.exists() else "fail", _exists_message(marker_sheet, "marker sheet")))


def _check_projection(root: Path, projection_config: dict, checks: list[Check]) -> None:
    config_path = root / projection_config.get("config", "")
    grid_path = root / projection_config.get("alignment_grid", "")
    expected_width = projection_config.get("world_width")
    expected_height = projection_config.get("world_height")

    checks.append(Check("projection.config", "pass" if config_path.exists() else "fail", _exists_message(config_path, "projection config")))
    if config_path.exists():
        try:
            data = json.loads(config_path.read_text(encoding="utf-8"))
            world = data.get("world", {})
            status = "pass" if world.get("width") == expected_width and world.get("height") == expected_height else "fail"
            checks.append(
                Check(
                    "projection.world_size",
                    status,
                    f"Projection world size is {world.get('width')}x{world.get('height')}; expected {expected_width}x{expected_height}",
                )
            )
        except json.JSONDecodeError as exc:
            checks.append(Check("projection.config_json", "fail", f"Invalid projection config JSON: {exc}"))

    checks.append(Check("projection.alignment_grid", "pass" if grid_path.exists() else "fail", _exists_message(grid_path, "alignment grid")))


def _check_hardware_signoffs(signoffs: dict, checks: list[Check], require_hardware: bool) -> None:
    required = [
        "emergency_stop_hardwired",
        "independent_speed_governor",
        "operator_start_stop_controls",
        "staff_race_control_flow",
        "safety_checklist_completed",
        "controlled_speed_playtest_completed",
    ]

    for key in required:
        value = bool(signoffs.get(key, False))
        if value:
            checks.append(Check(f"hardware.{key}", "pass", f"Hardware sign-off complete: {key}"))
        else:
            status = "fail" if require_hardware else "warn"
            checks.append(Check(f"hardware.{key}", status, f"Hardware sign-off pending: {key}"))


def _exists_message(path: Path, label: str) -> str:
    return f"Found {label}: {path}" if path.exists() else f"Missing {label}: {path}"


def print_report(report: dict) -> None:
    print(f"ZIKJ preflight status: {report['status'].upper()}")
    print(f"Summary: {report['summary']['pass']} pass, {report['summary']['warn']} warn, {report['summary']['fail']} fail")
    for check in report["checks"]:
        print(f"[{check['status'].upper()}] {check['id']}: {check['message']}")


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description="Run ZIKJ operations preflight checks")
    parser.add_argument("--root", default=".", help="Repository root")
    parser.add_argument("--config", default="ops/preflight_config.json", help="Preflight config path")
    parser.add_argument("--require-hardware", action="store_true", help="Fail if physical safety sign-offs are pending")
    parser.add_argument("--json", action="store_true", help="Print machine-readable JSON")
    args = parser.parse_args(argv)

    config = load_config(args.config)
    report = run_preflight(args.root, config, require_hardware=args.require_hardware)
    if args.json:
        print(json.dumps(report, indent=2))
    else:
        print_report(report)

    return 0 if report["status"] in {"pass", "warn"} and not args.require_hardware else (0 if report["status"] == "pass" else 1)


if __name__ == "__main__":
    raise SystemExit(main())
