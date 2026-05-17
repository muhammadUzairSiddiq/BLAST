"""
Rebuild levels 10-20 from Level 9, rebuild LevelCatalog, balance ammo.
Run after changing level prefabs outside Unity.
"""
import os
import re
import shutil
import subprocess

ROOT = r"e:\UNITY PROJECTS\BLAST"
PREFABS = os.path.join(ROOT, "Assets", "Prefabs", "Levels")
RES = os.path.join(ROOT, "Assets", "Resources", "Levels")
CATALOG = os.path.join(ROOT, "Assets", "Resources", "LevelCatalog.asset")
GAME = os.path.join(ROOT, "Assets", "Scenes", "Game.unity")
SCRIPT_GUID = "c4a8e1f23b5d4e9a8f7c6d5e4b3a2910"
SHOOTER_GUID = "1e9d9e3c27d9418479ff3b19904de09d"


def read(p):
    with open(p, encoding="utf-8") as f:
        return f.read()


def write(p, t):
    with open(p, "w", encoding="utf-8", newline="\n") as f:
        f.write(t)


def copy_l9_to(n):
    src = os.path.join(PREFABS, "Level (9).prefab")
    dst = os.path.join(PREFABS, f"Level ({n}).prefab")
    text = read(src)
    text = text.replace("m_Name: Level (9)", f"m_Name: Level ({n})", 1)
    text = re.sub(r"levelBonus: \d+", f"levelBonus: {10 + (n - 10)}", text, count=1)
    if n >= 15:
        text = re.sub(r"isHard: \d+", "isHard: 1", text, count=1)
        text = re.sub(r"customTheme: \d+", "customTheme: 1", text, count=1)
    # Slightly faster shooting on later levels (harder)
    if n >= 17:
        text = re.sub(
            r"(propertyPath: shootRate\n\s+value: )([\d.]+)",
            r"\g<1>0.08",
            text,
        )
    write(dst, text)
    if os.path.isdir(RES):
        shutil.copy2(dst, os.path.join(RES, f"Level ({n}).prefab"))


def rebuild_catalog():
    refs = []
    for n in range(1, 21):
        guid = re.search(r"guid: ([a-f0-9]+)", read(os.path.join(PREFABS, f"Level ({n}).prefab.meta"))).group(1)
        refs.append(f"  - {{fileID: 100100000, guid: {guid}, type: 3}}")
    body = (
        "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n"
        "  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n"
        "  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
        f"  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID}, type: 3}}\n"
        "  m_Name: LevelCatalog\n  m_EditorClassIdentifier: \n  levelRoots:\n"
        + "\n".join(refs)
        + "\n"
    )
    write(CATALOG, body)


def main():
    print("Building levels 10-20 from Level 9...")
    for n in range(10, 21):
        copy_l9_to(n)
        print(f"  Level ({n})")
    rebuild_catalog()
    print("Catalog: 20 entries")
    subprocess.run(["python", os.path.join(ROOT, "Tools", "balance_levels.py")], check=True)
    print("Done.")


if __name__ == "__main__":
    main()
