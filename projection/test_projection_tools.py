import copy
import json
import tempfile
import unittest
from pathlib import Path

from projection_mapper import ProjectionConfigError, ProjectionMapper, load_config


CONFIG_PATH = Path(__file__).with_name("projection_config.json")


class ProjectionToolTests(unittest.TestCase):
    def test_default_config_covers_world_samples(self):
        mapper = ProjectionMapper.from_file(CONFIG_PATH)

        sample_x = [0, 280, 640, 1000, 1280]
        sample_y = [0, 200, 360, 520, 720]

        for x in sample_x:
            for y in sample_y:
                with self.subTest(x=x, y=y):
                    hits = mapper.projectors_for_world_point(x, y)
                    self.assertGreater(len(hits), 0)
                    self.assertAlmostEqual(sum(hit["weight"] for hit in hits), 1.0, places=6)

    def test_single_projector_corner_maps_to_expected_pixel(self):
        mapper = ProjectionMapper.from_file(CONFIG_PATH)

        hits = mapper.projectors_for_world_point(0, 0)
        self.assertEqual([hit["projector_id"] for hit in hits], ["P0"])
        self.assertAlmostEqual(hits[0]["pixel"][0], 0.0, places=5)
        self.assertAlmostEqual(hits[0]["pixel"][1], 0.0, places=5)
        self.assertAlmostEqual(hits[0]["weight"], 1.0, places=6)

    def test_overlap_region_returns_all_projectors_with_normalized_weights(self):
        mapper = ProjectionMapper.from_file(CONFIG_PATH)

        hits = mapper.projectors_for_world_point(640, 360)
        self.assertEqual({hit["projector_id"] for hit in hits}, {"P0", "P1", "P2", "P3"})
        self.assertAlmostEqual(sum(hit["weight"] for hit in hits), 1.0, places=6)
        for hit in hits:
            self.assertGreater(hit["weight"], 0.0)

    def test_world_points_outside_bounds_are_not_mapped(self):
        mapper = ProjectionMapper.from_file(CONFIG_PATH)

        self.assertEqual(mapper.projectors_for_world_point(-1, 100), [])
        self.assertEqual(mapper.projectors_for_world_point(100, 721), [])
        self.assertEqual(mapper.projectors_for_world_point(1281, 100), [])

    def test_homography_maps_skewed_projector_quad_corners(self):
        config = load_config(CONFIG_PATH)
        config["projectors"] = [
            {
                "id": "Skew",
                "name": "Skew Test",
                "enabled": True,
                "source_quad": [[0, 0], [100, 0], [100, 100], [0, 100]],
                "projector_quad": [[10, 20], [1010, 50], [980, 780], [20, 760]],
            }
        ]
        config["world"] = {"width": 100, "height": 100, "coordinate_system": "test"}

        mapper = ProjectionMapper(config)
        expected = {
            (0, 0): [10, 20],
            (100, 0): [1010, 50],
            (100, 100): [980, 780],
            (0, 100): [20, 760],
        }

        for world, pixel in expected.items():
            with self.subTest(world=world):
                hits = mapper.projectors_for_world_point(*world)
                self.assertEqual(len(hits), 1)
                self.assertAlmostEqual(hits[0]["pixel"][0], pixel[0], delta=1e-5)
                self.assertAlmostEqual(hits[0]["pixel"][1], pixel[1], delta=1e-5)

    def test_alignment_grid_contains_requested_points(self):
        mapper = ProjectionMapper.from_file(CONFIG_PATH)

        grid = mapper.generate_alignment_grid(columns=5, rows=4)
        self.assertEqual(len(grid), 20)
        self.assertEqual(grid[0]["world"], [0.0, 0.0])
        self.assertEqual(grid[-1]["world"], [1280.0, 720.0])
        self.assertTrue(all(entry["projectors"] for entry in grid))

    def test_config_rejects_duplicate_projector_ids(self):
        config = load_config(CONFIG_PATH)
        bad = copy.deepcopy(config)
        bad["projectors"][1]["id"] = bad["projectors"][0]["id"]

        with self.assertRaises(ProjectionConfigError):
            ProjectionMapper(bad)

    def test_cli_grid_file_shape(self):
        mapper = ProjectionMapper.from_file(CONFIG_PATH)

        with tempfile.TemporaryDirectory() as tmp_dir:
            grid_path = Path(tmp_dir) / "alignment_grid.json"
            grid_path.write_text(json.dumps(mapper.generate_alignment_grid(3, 3)), encoding="utf-8")
            grid = json.loads(grid_path.read_text(encoding="utf-8"))

        self.assertEqual(len(grid), 9)
        self.assertTrue(all("world" in entry and "projectors" in entry for entry in grid))


if __name__ == "__main__":
    unittest.main()
