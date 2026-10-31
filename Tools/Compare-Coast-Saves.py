"""Compare two consistent paused snapshots, never a live run against its startup state."""
import argparse
import base64
import hashlib
import json
import math
from pathlib import Path


def load(path):
    envelope = json.loads(Path(path).read_text(encoding="utf-8-sig"))
    payload = envelope["payload"]
    digest = base64.b64encode(hashlib.sha256(payload.encode("utf-8")).digest()).decode()
    if envelope["format"] != 1 or digest != envelope["sha256"]:
        raise ValueError(f"Invalid envelope checksum: {path}")
    return json.loads(payload)


def differences(before, after, path=""):
    if isinstance(before, dict) and isinstance(after, dict):
        return [item for key in sorted(before.keys() | after.keys())
                for item in differences(before.get(key), after.get(key), f"{path}.{key}".lstrip("."))]
    if isinstance(before, list) and isinstance(after, list) and len(before) == len(after):
        return [item for index, (a, b) in enumerate(zip(before, after))
                for item in differences(a, b, f"{path}[{index}]")]
    if type(before) in (int, float) and type(after) in (int, float):
        same = (before == after if type(before) is int and type(after) is int
                else math.isclose(before, after, rel_tol=0, abs_tol=1e-9))
    else:
        same = before == after
    return [] if same else [{"path": path, "before": before, "after": after}]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("before")
    parser.add_argument("after")
    parser.add_argument("--output", required=True)
    parser.add_argument("--allow-story-migration", action="store_true")
    parser.add_argument("--allow-progression-migration", action="store_true")
    args = parser.parse_args()
    before, after = load(args.before), load(args.after)
    progression_migrated = args.allow_progression_migration and "opening" not in before
    if progression_migrated:
        opening = after.get("opening", {})
        if opening != {"version": 1, "remainingMask": 0, "complete": True, "guided": False} or after.get("purchasedBinsMask") != 0:
            raise ValueError("Legacy progression should be complete with no paid containers granted")
        after = {key: value for key, value in after.items() if key not in ("opening", "purchasedBinsMask")}
    migrated = args.allow_story_migration and "story" not in before
    if migrated:
        if after.get("story", {}).get("version") != 1:
            raise ValueError("Expected initialized story version one")
        after = {key: value for key, value in after.items() if key != "story"}
    diff = differences(before, after)
    report = {"status": "PASS" if not diff else "FAIL", "environment": "Checksum-validated snapshot files; no simulation",
              "floatTolerance": 1e-9, "storyMigrationAllowed": migrated,
              "progressionMigrationAllowed": progression_migrated,
              "before": str(Path(args.before).resolve()), "after": str(Path(args.after).resolve()),
              "money": after["money"], "processed": sum(m["processed"] for m in after["materials"]),
              "elapsed": after["elapsed"], "differences": diff}
    Path(args.output).write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))
    return 0 if not diff else 1


if __name__ == "__main__":
    raise SystemExit(main())
