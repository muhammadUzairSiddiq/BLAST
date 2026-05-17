"""
1. Copy Level (10) as template for levels 11-20 (same board + colors).
2. Sync all to Resources/Levels.
3. Fix Game.unity level prefab references using meta guids.
"""
import os
import re
import shutil
import uuid

PREFABS = r"e:\UNITY PROJECTS\BLAST\Assets\Prefabs\Levels"
RESOURCES = r"e:\UNITY PROJECTS\BLAST\Assets\Resources\Levels"
GAME_SCENE = r"e:\UNITY PROJECTS\BLAST\Assets\Scenes\Game.unity"
LEVEL_SCRIPT = "69c39b02cfb948e40b4afbfaafc24419"


def read_text(p):
    with open(p, encoding="utf-8") as f:
        return f.read()


def write_text(p, t):
    with open(p, "w", encoding="utf-8", newline="\n") as f:
        f.write(t)


def get_guid(prefab_path):
    meta = prefab_path + ".meta"
    return re.search(r"guid: ([a-f0-9]+)", read_text(meta)).group(1)


def get_level_file_id(prefab_path, level_num):
    text = read_text(prefab_path)
    marker = f"m_Name: Level ({level_num})"
    pos = text.find(marker)
    segment = text[pos : pos + 1200]
    m = re.search(
        rf"--- !u!114 &(-?\d+)\nMonoBehaviour:.*?{LEVEL_SCRIPT}",
        segment,
        re.S,
    )
    return m.group(1) if m else "100100000"


def copy_level_from_template(target_num, template_num=10):
    src = os.path.join(PREFABS, f"Level ({template_num}).prefab")
    dst = os.path.join(PREFABS, f"Level ({target_num}).prefab")
    text = read_text(src)
    text = re.sub(
        rf"m_Name: Level \({template_num}\)",
        f"m_Name: Level ({target_num})",
        text,
        count=1,
    )
    text = re.sub(r"levelBonus: \d+", f"levelBonus: {10 + (target_num - 10)}", text, count=1)
    write_text(dst, text)
    print(f"  Prefab Level ({target_num}) <- template Level ({template_num})")


def sync_to_resources(level_num):
    src = os.path.join(PREFABS, f"Level ({level_num}).prefab")
    dst = os.path.join(RESOURCES, f"Level ({level_num}).prefab")
    shutil.copy2(src, dst)
    meta_dst = dst + ".meta"
    if os.path.exists(meta_dst):
        pass  # keep existing guid for Resources copy
    else:
        write_text(
            meta_dst,
            f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nPrefabImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
        )


def fix_game_scene():
    refs = []
    for n in range(1, 21):
        path = os.path.join(PREFABS, f"Level ({n}).prefab")
        fid = get_level_file_id(path, n)
        guid = get_guid(path)
        refs.append(f"  - {{fileID: {fid}, guid: {guid}, type: 3}}")

    scene = read_text(GAME_SCENE)
    m = re.search(r"levelPrefabs:\n((?:  - \{fileID:.*\n)+)", scene)
    block = "levelPrefabs:\n" + "\n".join(refs) + "\n"
    scene = scene[: m.start()] + block + scene[m.end() :]
    write_text(GAME_SCENE, scene)
    print("  Game.unity levelPrefabs updated (1-20)")


def main():
    print("Syncing levels 11-20 from Level (10) template...")
    for n in range(11, 21):
        copy_level_from_template(n)
    print("Syncing Resources/Levels 1-20 from Prefabs...")
    for n in range(1, 21):
        sync_to_resources(n)
    fix_game_scene()
    print("Done. Run balance_levels.py then open Unity.")


if __name__ == "__main__":
    main()
