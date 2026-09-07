#!/usr/bin/env python3
"""Projection calibration helpers for ZIKJ.

Maps venue/world coordinates from the 1280x720 prototype coordinate system into
individual projector pixel coordinates using per-projector homographies.
"""

import argparse
import json
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

import numpy as np


class ProjectionConfigError(ValueError):
    """Raised when a projection config is structurally invalid."""


@dataclass(frozen=True)
class ProjectorMap:
    id: str
    name: str
    enabled: bool
    source_quad: np.ndarray
    projector_quad: np.ndarray
    homography: np.ndarray
    bounds: tuple[float, float, float, float]


def load_config(path: str | Path) -> dict:
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def validate_config(config: dict) -> None:
    world = config.get("world", {})
    width = world.get("width")
    height = world.get("height")
    if not _positive_number(width) or not _positive_number(height):
        raise ProjectionConfigError("world.width and world.height must be positive numbers")

    resolution = config.get("projector_resolution")
    if not _valid_pair(resolution) or resolution[0] <= 0 or resolution[1] <= 0:
        raise ProjectionConfigError("projector_resolution must be [width, height]")

    blend_px = config.get("blend_px", 0)
    if not _positive_or_zero(blend_px):
        raise ProjectionConfigError("blend_px must be zero or positive")

    projectors = config.get("projectors")
    if not isinstance(projectors, list) or not projectors:
        raise ProjectionConfigError("projectors must be a non-empty list")

    ids = set()
    for projector in projectors:
        projector_id = projector.get("id")
        if not isinstance(projector_id, str) or not projector_id:
            raise ProjectionConfigError("each projector needs a non-empty id")
        if projector_id in ids:
            raise ProjectionConfigError(f"duplicate projector id: {projector_id}")
        ids.add(projector_id)

        source = _quad(projector.get("source_quad"), f"{projector_id}.source_quad")
        target = _quad(projector.get("projector_quad"), f"{projector_id}.projector_quad")
        if abs(_quad_area(source)) < 1e-3:
            raise ProjectionConfigError(f"{projector_id}.source_quad has zero area")
        if abs(_quad_area(target)) < 1e-3:
            raise ProjectionConfigError(f"{projector_id}.projector_quad has zero area")

        min_x, min_y, max_x, max_y = _bounds(source)
        if min_x < -1e-3 or min_y < -1e-3 or max_x > width + 1e-3 or max_y > height + 1e-3:
            raise ProjectionConfigError(f"{projector_id}.source_quad extends outside world bounds")


class ProjectionMapper:
    def __init__(self, config: dict):
        validate_config(config)
        self.config = config
        self.world_width = float(config["world"]["width"])
        self.world_height = float(config["world"]["height"])
        self.projector_resolution = tuple(float(v) for v in config["projector_resolution"])
        self.blend_px = float(config.get("blend_px", 0))
        self.projectors = [self._build_projector(p) for p in config["projectors"]]

    @classmethod
    def from_file(cls, path: str | Path) -> "ProjectionMapper":
        return cls(load_config(path))

    def projectors_for_world_point(self, x: float, y: float) -> list[dict]:
        """Return projector pixel mappings for a world point.

        The result can contain multiple projectors in overlap regions. Weights
        are normalized so downstream blend code can combine them directly.
        """

        x = float(x)
        y = float(y)
        if x < 0 or y < 0 or x > self.world_width or y > self.world_height:
            return []

        hits = []
        for projector in self.projectors:
            if not projector.enabled or not _inside_bounds(x, y, projector.bounds):
                continue

            px, py = _transform_point(projector.homography, x, y)
            raw_weight = _edge_weight(projector.bounds, x, y, self.blend_px)
            hits.append(
                {
                    "projector_id": projector.id,
                    "projector_name": projector.name,
                    "pixel": [px, py],
                    "raw_weight": raw_weight,
                }
            )

        total = sum(hit["raw_weight"] for hit in hits)
        if total <= 0:
            return []

        for hit in hits:
            hit["weight"] = hit["raw_weight"] / total
        return hits

    def generate_alignment_grid(self, columns: int = 5, rows: int = 4) -> list[dict]:
        if columns < 2 or rows < 2:
            raise ValueError("columns and rows must be at least 2")

        grid = []
        for row in range(rows):
            y = self.world_height * row / (rows - 1)
            for col in range(columns):
                x = self.world_width * col / (columns - 1)
                grid.append(
                    {
                        "world": [x, y],
                        "projectors": self.projectors_for_world_point(x, y),
                    }
                )
        return grid

    def projector(self, projector_id: str) -> ProjectorMap:
        for projector in self.projectors:
            if projector.id == projector_id:
                return projector
        raise KeyError(projector_id)

    def _build_projector(self, projector: dict) -> ProjectorMap:
        source = _quad(projector["source_quad"], f"{projector['id']}.source_quad")
        target = _quad(projector["projector_quad"], f"{projector['id']}.projector_quad")
        return ProjectorMap(
            id=projector["id"],
            name=projector.get("name", projector["id"]),
            enabled=bool(projector.get("enabled", True)),
            source_quad=source,
            projector_quad=target,
            homography=_homography(source, target),
            bounds=_bounds(source),
        )


def _positive_number(value) -> bool:
    return isinstance(value, (int, float)) and value > 0


def _positive_or_zero(value) -> bool:
    return isinstance(value, (int, float)) and value >= 0


def _valid_pair(value) -> bool:
    return (
        isinstance(value, list)
        and len(value) == 2
        and all(isinstance(v, (int, float)) for v in value)
    )


def _quad(value, label: str) -> np.ndarray:
    if not isinstance(value, list) or len(value) != 4:
        raise ProjectionConfigError(f"{label} must contain four [x, y] points")
    if not all(_valid_pair(point) for point in value):
        raise ProjectionConfigError(f"{label} points must be numeric [x, y] pairs")
    return np.array(value, dtype=float)


def _quad_area(points: np.ndarray) -> float:
    x = points[:, 0]
    y = points[:, 1]
    return 0.5 * float(np.dot(x, np.roll(y, -1)) - np.dot(y, np.roll(x, -1)))


def _bounds(points: np.ndarray) -> tuple[float, float, float, float]:
    return (
        float(np.min(points[:, 0])),
        float(np.min(points[:, 1])),
        float(np.max(points[:, 0])),
        float(np.max(points[:, 1])),
    )


def _inside_bounds(x: float, y: float, bounds: tuple[float, float, float, float]) -> bool:
    min_x, min_y, max_x, max_y = bounds
    eps = 1e-6
    return min_x - eps <= x <= max_x + eps and min_y - eps <= y <= max_y + eps


def _homography(source: np.ndarray, target: np.ndarray) -> np.ndarray:
    rows = []
    values = []
    for (x, y), (u, v) in zip(source, target):
        rows.append([x, y, 1, 0, 0, 0, -u * x, -u * y])
        values.append(u)
        rows.append([0, 0, 0, x, y, 1, -v * x, -v * y])
        values.append(v)

    h = np.linalg.solve(np.array(rows, dtype=float), np.array(values, dtype=float))
    return np.array(
        [
            [h[0], h[1], h[2]],
            [h[3], h[4], h[5]],
            [h[6], h[7], 1.0],
        ],
        dtype=float,
    )


def _transform_point(homography: np.ndarray, x: float, y: float) -> tuple[float, float]:
    mapped = homography @ np.array([x, y, 1.0], dtype=float)
    if abs(mapped[2]) < 1e-9:
        raise ZeroDivisionError("homography mapped point to infinity")
    return (float(mapped[0] / mapped[2]), float(mapped[1] / mapped[2]))


def _edge_weight(bounds: tuple[float, float, float, float], x: float, y: float, fade_px: float) -> float:
    if fade_px <= 0:
        return 1.0

    min_x, min_y, max_x, max_y = bounds
    dx = min(x - min_x, max_x - x)
    dy = min(y - min_y, max_y - y)
    edge_distance = max(0.0, min(dx, dy))
    return max(0.001, min(1.0, edge_distance / fade_px))


def _json_default(value):
    if isinstance(value, np.ndarray):
        return value.tolist()
    if isinstance(value, np.generic):
        return value.item()
    raise TypeError(f"Object of type {type(value).__name__} is not JSON serializable")


def main(argv: Iterable[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Validate and query ZIKJ projection calibration")
    parser.add_argument("--config", default="projection_config.json", help="Projection config JSON path")
    parser.add_argument("--validate", action="store_true", help="Validate config and exit")
    parser.add_argument("--map", nargs=2, type=float, metavar=("X", "Y"), help="Map one world point")
    parser.add_argument("--grid-out", help="Write alignment grid JSON")
    parser.add_argument("--columns", type=int, default=5, help="Alignment grid columns")
    parser.add_argument("--rows", type=int, default=4, help="Alignment grid rows")
    args = parser.parse_args(argv)

    mapper = ProjectionMapper.from_file(args.config)

    if args.validate:
        print(f"[OK] Projection config valid: {args.config}")

    if args.map:
        x, y = args.map
        print(json.dumps(mapper.projectors_for_world_point(x, y), indent=2, default=_json_default))

    if args.grid_out:
        grid = mapper.generate_alignment_grid(columns=args.columns, rows=args.rows)
        Path(args.grid_out).write_text(json.dumps(grid, indent=2, default=_json_default), encoding="utf-8")
        print(f"[OK] Wrote alignment grid: {args.grid_out}")

    if not args.validate and not args.map and not args.grid_out:
        parser.print_help()

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
