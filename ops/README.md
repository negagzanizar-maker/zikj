# ZIKJ Operations Preflight

Local operations checks for the software side of venue readiness.

The preflight tool verifies:

- all six Windows Unity player executables and data folders exist
- player builds do not include disallowed test assemblies
- tracking config, marker PNGs, marker sheet, and UDP port are present
- projection config and generated alignment grid are present
- hardware safety sign-offs are explicitly tracked

Hardware sign-offs are warnings for local laptop testing. They become failures when `--require-hardware` is passed for venue acceptance.

## Commands

Run local preflight:

```powershell
python ops\preflight.py --root . --config ops\preflight_config.json
```

Run venue-gated preflight:

```powershell
python ops\preflight.py --root . --config ops\preflight_config.json --require-hardware
```

Run tests:

```powershell
python -m unittest discover -s ops -p "test_*.py" -v
```

The default config intentionally leaves hardware sign-offs as `false`. Do not set them to `true` until a real operator has verified the hard-wired emergency stop, independent speed governor, operator start/stop controls, staff race-control flow, safety checklist, and controlled-speed venue playtest.
