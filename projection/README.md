# ZIKJ Projection Calibration

Local, no-hardware helpers for preparing the projector alignment step.

## What This Covers

- Validates four-projector coverage over the `1280 x 720` ZIKJ world/prototype coordinate space.
- Maps world points into projector pixel coordinates with per-projector homographies.
- Returns normalized blend weights for overlap regions.
- Generates an alignment grid JSON that can be used during a venue calibration session.

Real projector mounting, lens shift, focus, blend curves, and final warp still require venue hardware and MadMapper or an equivalent projection tool.

## Commands

Install dependencies:

```powershell
python -m pip install -r projection\requirements.txt
```

Validate the projection config:

```powershell
python projection\projection_mapper.py --config projection\projection_config.json --validate
```

Map one world point:

```powershell
python projection\projection_mapper.py --config projection\projection_config.json --map 640 360
```

Generate an alignment grid:

```powershell
python projection\projection_mapper.py --config projection\projection_config.json --grid-out projection\alignment_grid.json --columns 5 --rows 4
```

Run local tests:

```powershell
python -m unittest discover -s projection -p "test_*.py" -v
```

## Default Layout

The default config models four 1920x1080 projectors covering the 1280x720 world with an 80 px overlap:

- `P0`: front left
- `P1`: front right
- `P2`: rear left
- `P3`: rear right

The center overlap point `640,360` maps to all four projectors with normalized blend weights.
