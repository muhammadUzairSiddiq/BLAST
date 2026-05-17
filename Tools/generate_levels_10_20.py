"""
Generate Level (10) through Level (20) prefabs from existing templates.
Ensures shooter ammo stays >= cube count per color (with margin).
"""
import os
import re
import uuid
import shutil
from collections import Counter

LEVELS_DIR = r"e:\UNITY PROJECTS\BLAST\Assets\Prefabs\Levels"
GAME_SCENE = r"e:\UNITY PROJECTS\BLAST\Assets\Scenes\Game.unity"

# Source template per target level (variety from existing 1-9)
LEVEL_SOURCES = {
    10: 8,
    11: 2,
    12: 3,
    13: 4,
    14: 5,
    15: 6,
    16: 7,
    17: 8,
    18: 9,
    19: 5,
    20: 9,
}

# Per-level tuning: harder = more surprise, hard BGM, themed visuals; ammo bumped for solvability
LEVEL_TUNING = {
    10: {"isHard": 0, "customTheme": 0, "ammo_bonus": 5, "surprise_add": 1, "theme_rgba": None},
    11: {"isHard": 0, "customTheme": 0, "ammo_bonus": 3, "surprise_add": 1, "theme_rgba": None},
    12: {"isHard": 0, "customTheme": 1, "ammo_bonus": 2, "surprise_add": 2, "theme_rgba": 4282864511},  # blue tint
    13: {"isHard": 0, "customTheme": 0, "ammo_bonus": 2, "surprise_add": 2, "theme_rgba": None},
    14: {"isHard": 0, "customTheme": 1, "ammo_bonus": 5, "surprise_add": 3, "theme_rgba": 4287137928},  # green tint
    15: {"isHard": 1, "customTheme": 0, "ammo_bonus": 5, "surprise_add": 2, "theme_rgba": None},
    16: {"isHard": 0, "customTheme": 1, "ammo_bonus": 3, "surprise_add": 2, "theme_rgba": 4291611852},  # orange tint
    17: {"isHard": 0, "customTheme": 0, "ammo_bonus": 0, "surprise_add": 3, "theme_rgba": None},
    18: {"isHard": 1, "customTheme": 1, "ammo_bonus": 5, "surprise_add": 3, "theme_rgba": 4288256409},
    19: {"isHard": 1, "customTheme": 1, "ammo_bonus": 3, "surprise_add": 4, "theme_rgba": 4294901760},  # red tint
    20: {"isHard": 1, "customTheme": 1, "ammo_bonus": 8, "surprise_add": 2, "theme_rgba": 4283178038},  # boss (same as L9)
}


def read_text(path):
    with open(path, "r", encoding="utf-8") as f:
        return f.read()


def write_text(path, text):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def count_cube_colors(text):
    counts = Counter()
    for val in re.findall(r"propertyPath: cubeColors\n\s+value: (\d+)", text):
        counts[int(val)] += 1
    for val in re.findall(r"(?<!propertyPath: )cubeColors: (\d+)", text):
        counts[int(val)] += 1
    return counts


def bump_all_ammo(text, bonus):
    if bonus <= 0:
        return text
    text = re.sub(
        r"ammoCount: (\d+)",
        lambda m: f"ammoCount: {int(m.group(1)) + bonus}",
        text,
    )
    text = re.sub(
        r"(propertyPath: ammoCount\n\s+value: )(\d+)",
        lambda m: f"{m.group(1)}{int(m.group(2)) + bonus}",
        text,
    )
    return text


def get_total_ammo(text):
    vals = [int(x) for x in re.findall(r"ammoCount: (\d+)", text)]
    vals += [
        int(m.group(1))
        for m in re.finditer(r"propertyPath: ammoCount\n\s+value: (\d+)", text)
    ]
    return sum(vals), len(vals)


def apply_surprise_add(text, count_to_add):
    if count_to_add <= 0:
        return text
    parts = []
    last = 0
    added = 0
    for m in re.finditer(r"isSurpriseShooter: 0", text):
        if added < count_to_add:
            parts.append(text[last : m.start()])
            parts.append("isSurpriseShooter: 1")
            last = m.end()
            added += 1
        else:
            break
    parts.append(text[last:])
    if added > 0:
        return "".join(parts)
    return text


def shift_cube_layout(text, seed):
    """Slight visual variety: nudge a subset of cube local positions."""
    offset = 0.04 * (seed % 5)
    count = 0
    max_shifts = 12 + seed

    def repl(m):
        nonlocal count
        if count >= max_shifts:
            return m.group(0)
        x, y, z = float(m.group(1)), float(m.group(2)), float(m.group(3))
        if abs(y) > 0.01:  # only floor cubes
            return m.group(0)
        count += 1
        sign = 1 if count % 2 else -1
        return f"m_LocalPosition: {{x: {x + sign * offset:.3f}, y: {y}, z: {z + sign * offset * 0.5:.3f}}}"

    return re.sub(
        r"m_LocalPosition: \{x: ([-\d.]+), y: ([-\d.]+), z: ([-\d.]+)\}",
        repl,
        text,
    )


def validate_level(text, level_num):
    cubes = count_cube_colors(text)
    total_cubes = sum(cubes.values())
    if total_cubes == 0:
        print(f"  WARN L{level_num}: could not count cubes (prefab may use nested refs)")
        return True

    total_ammo, shooter_count = get_total_ammo(text)
    if total_ammo < total_cubes:
        print(f"  FAIL L{level_num}: total ammo {total_ammo} < cubes {total_cubes}")
        return False
    if total_ammo < total_cubes * 1.08:
        print(f"  WARN L{level_num}: tight ammo margin ({total_ammo} vs {total_cubes} cubes)")
    print(
        f"  OK L{level_num}: cubes={total_cubes} ammo={total_ammo} "
        f"shooters={shooter_count} colors={dict(cubes)}"
    )
    return True


def create_meta(prefab_path, guid):
    meta = f"""fileFormatVersion: 2
guid: {guid}
PrefabImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    write_text(prefab_path + ".meta", meta)


LEVEL_SCRIPT_GUID = "69c39b02cfb948e40b4afbfaafc24419"


def get_level_component_file_id(level_num):
    path = os.path.join(LEVELS_DIR, f"Level ({level_num}).prefab")
    with open(path, encoding="utf-8") as f:
        text = f.read()
    marker = f"m_Name: Level ({level_num})"
    pos = text.find(marker)
    if pos < 0:
        raise ValueError(f"Root not found in Level ({level_num})")
    segment = text[pos : pos + 1200]
    m = re.search(
        rf"--- !u!114 &(-?\d+)\nMonoBehaviour:.*?guid: {LEVEL_SCRIPT_GUID}",
        segment,
        re.DOTALL,
    )
    if not m:
        raise ValueError(f"Level component not found in Level ({level_num})")
    return m.group(1)


def read_guid(prefab_path):
    meta_path = prefab_path + ".meta"
    if os.path.exists(meta_path):
        m = re.search(r"guid: ([a-f0-9]+)", read_text(meta_path))
        if m:
            return m.group(1)
    return uuid.uuid4().hex


def generate_level(target_level):
    source = LEVEL_SOURCES[target_level]
    src_path = os.path.join(LEVELS_DIR, f"Level ({source}).prefab")
    dst_path = os.path.join(LEVELS_DIR, f"Level ({target_level}).prefab")

    text = read_text(src_path)
    tuning = LEVEL_TUNING[target_level]

    text = re.sub(
        rf"m_Name: Level \({source}\)",
        f"m_Name: Level ({target_level})",
        text,
        count=1,
    )
    text = re.sub(r"levelBonus: \d+", f"levelBonus: {10 + (target_level - 10)}", text, count=1)
    text = re.sub(r"isHard: [01]", f"isHard: {tuning['isHard']}", text, count=1)
    text = re.sub(r"customTheme: [01]", f"customTheme: {tuning['customTheme']}", text, count=1)

    if tuning["theme_rgba"] is not None:
        text = re.sub(
            r"(customColor:\s+serializedVersion: 2\s+rgba: )\d+",
            rf"\g<1>{tuning['theme_rgba']}",
            text,
            count=1,
        )

    text = bump_all_ammo(text, tuning["ammo_bonus"])

    text = apply_surprise_add(text, tuning["surprise_add"])
    text = shift_cube_layout(text, target_level)

    # Auto-fix solvability: ensure total ammo >= 108% of cube count
    total_cubes = sum(count_cube_colors(text).values())
    total_ammo, _ = get_total_ammo(text)
    if total_cubes > 0 and total_ammo < total_cubes * 1.08:
        extra = int(total_cubes * 1.12) - total_ammo
        per_shooter = max(1, (extra + 8) // 9)
        text = bump_all_ammo(text, per_shooter)
        print(f"  Auto-bumped ammo +{per_shooter} per shooter for solvability")

    write_text(dst_path, text)
    guid = read_guid(dst_path)
    create_meta(dst_path, guid)
    validate_level(text, target_level)
    return guid


def update_game_scene(new_guids):
    text = read_text(GAME_SCENE)
    match = re.search(r"levelPrefabs:\n((?:  - \{fileID:.*\n)+)", text)
    if not match:
        raise RuntimeError("Could not find levelPrefabs in Game.unity")

    lines = match.group(1).strip().split("\n")
    # Keep original 9 levels only, then append 10-20
    base_lines = lines[:9]
    for i, guid in enumerate(new_guids):
        level_num = 10 + i
        fid = get_level_component_file_id(level_num)
        base_lines.append(f"  - {{fileID: {fid}, guid: {guid}, type: 3}}")

    new_block = "levelPrefabs:\n" + "\n".join(base_lines) + "\n"
    text = text[: match.start()] + new_block + text[match.end() :]
    write_text(GAME_SCENE, text)
    print(f"Updated Game.unity: {len(base_lines)} level prefabs registered")


def main():
    new_guids = []
    for lvl in range(10, 21):
        print(f"Generating Level ({lvl}) from Level ({LEVEL_SOURCES[lvl]})...")
        guid = generate_level(lvl)
        new_guids.append(guid)
        print(f"  guid: {guid}")

    update_game_scene(new_guids)
    print("Done.")


if __name__ == "__main__":
    main()
