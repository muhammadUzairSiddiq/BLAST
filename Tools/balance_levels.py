"""
Exact balance: sum(shooter ammo per color) == cube count per color.
Supports prefab-instance levels (1-11, 15-18, 20) and embedded-tower levels (12-14, 19).
"""
import os
import re
from collections import Counter, defaultdict

LEVELS_DIR = r"e:\UNITY PROJECTS\BLAST\Assets\Prefabs\Levels"
RESOURCES_DIR = r"e:\UNITY PROJECTS\BLAST\Assets\Resources\Levels"
SHOOTER_PREFAB_GUID = "1e9d9e3c27d9418479ff3b19904de09d"
COLOR_CUBE_PREFAB_GUID = "b20b92e85a6b59f4699de6d4e5f3c538"
PLAYER_SCRIPT_GUID = "4370d2941dd6fb844bf1c060be7e42e6"
COLOR_CUBE_SCRIPT_GUID = "1bcd24f7da48f584f91a3429771dc4de"
PLAYER_FILE_ID = "2861839722562445153"
COLOR_NAMES = ["Yellow", "Red", "Blue", "Green", "Orange", "Surprise"]
PLAYABLE = [0, 1, 2, 3, 4]


def read_text(path):
    with open(path, encoding="utf-8") as f:
        return f.read()


def write_text(path, text):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def count_cubes(text):
    counts = Counter()

    for part in text.split("--- !u!1001 &")[1:]:
        if SHOOTER_PREFAB_GUID in part:
            continue
        for m in re.finditer(r"propertyPath: cubeColors\n\s+value: (\d+)", part):
            c = int(m.group(1))
            if c != 5:
                counts[c] += 1

    for part in text.split("--- !u!114 &")[1:]:
        if COLOR_CUBE_SCRIPT_GUID not in part:
            continue
        m = re.search(r"cubeColors: (\d+)", part)
        if m:
            c = int(m.group(1))
            if c != 5:
                counts[c] += 1

    return counts


def distribute(total, n):
    if n <= 0:
        return []
    q, r = divmod(total, n)
    return [q + (1 if i < r else 0) for i in range(n)]


def assign_colors(n_shooters, cube_counts):
    active = [(c, cube_counts[c]) for c in PLAYABLE if cube_counts.get(c, 0) > 0]
    if not active:
        return [0] * n_shooters

    assignment = [c for c, _ in active]
    while len(assignment) < n_shooters:
        assigned = Counter(assignment)
        pick = max(active, key=lambda x: x[1] / assigned[x[0]])[0]
        assignment.append(pick)
    return assignment[:n_shooters]


def plan_balance(cube_counts, n_shooters):
    colors = assign_colors(n_shooters, cube_counts)
    groups = defaultdict(list)
    for i, c in enumerate(colors):
        groups[c].append(i)

    plan = [None] * n_shooters
    for c, indices in groups.items():
        for idx, ammo in zip(indices, distribute(cube_counts.get(c, 0), len(indices))):
            plan[idx] = (c, max(1, ammo))
    return plan


# --- Prefab-instance shooters (Shooting Tower prefab) ---
def parse_instance_shooters(text):
    out = []
    marker = "--- !u!1001 &"
    starts = [m.start() for m in re.finditer(re.escape(marker), text)]
    starts.append(len(text))
    for i in range(len(starts) - 1):
        start, end = starts[i], starts[i + 1]
        block = text[start:end]
        if SHOOTER_PREFAB_GUID in block and "Shooting Tower" in block:
            out.append({"kind": "instance", "start": start, "end": end, "block": block})
    return out


def set_instance_mod(block, prop, value):
    pat = rf"(propertyPath: {prop}\n\s+value: )(\d+)"
    if re.search(pat, block):
        return re.sub(pat, rf"\g<1>{value}", block, count=1)
    entry = (
        f"    - target: {{fileID: {PLAYER_FILE_ID}, guid: {SHOOTER_PREFAB_GUID}, type: 3}}\n"
        f"      propertyPath: {prop}\n"
        f"      value: {value}\n"
        f"      objectReference: {{fileID: 0}}\n"
    )
    pos = block.find("m_Modifications:")
    pos = block.find("\n", pos) + 1 if pos >= 0 else 0
    return block[:pos] + entry + block[pos:]


def apply_instance_plan(text, entries, plan):
    for e, (color, ammo) in sorted(zip(entries, plan), key=lambda x: x[0]["start"], reverse=True):
        block = e["block"]
        block = set_instance_mod(block, "ammoCount", ammo)
        block = set_instance_mod(block, "cubeColors", color)
        block = set_instance_mod(block, "isSurpriseShooter", 0)
        block = re.sub(
            r"(propertyPath: m_text\n\s+value: )\d+",
            rf"\g<1>{ammo}",
            block,
            count=1,
        )
        text = text[: e["start"]] + block + text[e["end"] :]
    return text


# --- Embedded shooters (full hierarchy in level prefab) ---
def parse_embedded_shooters(text):
    out = []
    for gm in re.finditer(r"--- !u!1 &[0-9]+\nGameObject:", text):
        start = gm.start()
        end = text.find("--- !u!1 &", start + 10)
        if end < 0:
            end = len(text)
        go_block = text[start:end]
        if "m_Name: Shooting Tower" not in go_block:
            continue
        for pm in re.finditer(r"--- !u!114 &[0-9]+\nMonoBehaviour:", go_block):
            pstart = start + pm.start()
            pend = start + go_block.find("--- !u!114 &", pm.start() + 10)
            if pend < start:
                pend = end
            player_block = text[pstart:pend]
            if PLAYER_SCRIPT_GUID not in player_block:
                continue
            if not re.search(r"ammoCount: \d+", player_block):
                continue
            out.append({"kind": "embedded", "start": pstart, "end": pend, "block": player_block})
            break
    return out


def apply_embedded_plan(text, entries, plan):
    for e, (color, ammo) in sorted(zip(entries, plan), key=lambda x: x[0]["start"], reverse=True):
        block = e["block"]
        block = re.sub(r"(ammoCount: )\d+", rf"\g<1>{ammo}", block, count=1)
        block = re.sub(r"(cubeColors: )\d+", rf"\g<1>{color}", block, count=1)
        block = re.sub(r"(isSurpriseShooter: )\d+", r"\g<1>0", block, count=1)
        text = text[: e["start"]] + block + text[e["end"] :]
    return text


def measure_ammo(text, shooters, plan=None):
    ammo = Counter()
    for i, s in enumerate(shooters):
        if plan:
            c, a = plan[i]
            ammo[c] += a
        elif s["kind"] == "instance":
            block = s["block"]
            c = int(re.search(r"propertyPath: cubeColors\n\s+value: (\d+)", block).group(1))
            a = int(re.search(r"propertyPath: ammoCount\n\s+value: (\d+)", block).group(1))
            ammo[c] += a
        else:
            block = s["block"]
            c = int(re.search(r"cubeColors: (\d+)", block).group(1))
            a = int(re.search(r"ammoCount: (\d+)", block).group(1))
            ammo[c] += a
    return ammo


def balance_level(n):
    path = os.path.join(LEVELS_DIR, f"Level ({n}).prefab")
    text = read_text(path)
    cubes = count_cubes(text)
    shooters = parse_instance_shooters(text) + parse_embedded_shooters(text)
    total_cubes = sum(cubes.values())

    if not shooters or total_cubes == 0:
        print(f"  L{n}: SKIP cubes={total_cubes} shooters={len(shooters)}")
        return False

    plan = plan_balance(cubes, len(shooters))
    inst = [s for s in shooters if s["kind"] == "instance"]
    emb = [s for s in shooters if s["kind"] == "embedded"]
    inst_plan = plan[: len(inst)]
    emb_plan = plan[len(inst) :]

    text = apply_instance_plan(text, inst, inst_plan)
    text = apply_embedded_plan(text, emb, emb_plan)

    shooters2 = parse_instance_shooters(text) + parse_embedded_shooters(text)
    ammo = measure_ammo(text, shooters2)
    ok = all(cubes.get(c, 0) == ammo.get(c, 0) for c in PLAYABLE if cubes.get(c, 0))

    write_text(path, text)
    res = os.path.join(RESOURCES_DIR, f"Level ({n}).prefab")
    if os.path.exists(res):
        write_text(res, text)

    tag = "OK" if ok else "FAIL"
    print(f"  L{n}: {tag} cubes={sum(cubes.values())} {dict(cubes)} ammo={dict(ammo)} towers={len(shooters)}")
    return ok


def main():
    print("Balancing levels 10-20...")
    for n in range(10, 21):
        balance_level(n)
    print("Done.")


if __name__ == "__main__":
    main()
