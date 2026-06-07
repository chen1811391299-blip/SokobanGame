from __future__ import annotations

from collections import deque
from pathlib import Path
import re


DIRS = {
    "U": (0, -1),
    "D": (0, 1),
    "L": (-1, 0),
    "R": (1, 0),
}

TILE_NAMES = {
    "x": "Empty",
    "#": "Wall",
    " ": "Floor",
    "@": "Player",
    "$": "Box",
    ".": "Goal",
    "*": "BoxOnGoal",
    "~": "Ice",
    "p": "PressurePlate",
    "D": "DoorClosed",
    "A": "Portal",
    "B": "Portal",
    "C": "Portal",
}


class Level:
    def __init__(self, level_id: str, name: str, par: int, rows: list[str]) -> None:
        self.level_id = level_id
        self.name = name
        self.par = par
        self.rows = rows
        self.height = len(rows)
        self.width = len(rows[0])
        self.base: dict[tuple[int, int], str] = {}
        self.player: tuple[int, int] | None = None
        self.boxes: set[tuple[int, int]] = set()
        self.goals: set[tuple[int, int]] = set()
        self.portals: dict[tuple[int, int], tuple[int, int]] = {}
        self.links: list[tuple[tuple[int, int], tuple[int, int]]] = []

        portals: dict[str, list[tuple[int, int]]] = {}
        plates: list[tuple[int, int]] = []
        doors: list[tuple[int, int]] = []

        for y, row in enumerate(rows):
            if len(row) != self.width:
                raise ValueError(f"{level_id} row {y} has width {len(row)}, expected {self.width}")

            for x, c in enumerate(row):
                if c not in TILE_NAMES:
                    raise ValueError(f"{level_id} has unsupported tile {c!r}")

                pos = (x, y)
                if c == "@":
                    self.base[pos] = "Floor"
                    if self.player is not None:
                        raise ValueError(f"{level_id} has more than one player")
                    self.player = pos
                elif c == "$":
                    self.base[pos] = "Floor"
                    self.boxes.add(pos)
                elif c == ".":
                    self.base[pos] = "Goal"
                    self.goals.add(pos)
                elif c == "*":
                    self.base[pos] = "Goal"
                    self.goals.add(pos)
                    self.boxes.add(pos)
                elif c in "ABC":
                    self.base[pos] = "Portal"
                    portals.setdefault(c, []).append(pos)
                elif c == "p":
                    self.base[pos] = "PressurePlate"
                    plates.append(pos)
                elif c == "D":
                    self.base[pos] = "DoorClosed"
                    doors.append(pos)
                else:
                    self.base[pos] = TILE_NAMES[c]

        if self.player is None:
            raise ValueError(f"{level_id} has no player")

        for label, positions in portals.items():
            if len(positions) != 2:
                raise ValueError(f"{level_id} portal {label} has {len(positions)} endpoints")
            a, b = positions
            self.portals[a] = b
            self.portals[b] = a

        if len(plates) != len(doors):
            raise ValueError(f"{level_id} has {len(plates)} plates and {len(doors)} doors")
        self.links = list(zip(plates, doors))

    def required_mechanics(self) -> list[str]:
        required: list[str] = []
        if any("~" in row for row in self.rows):
            required.append("ice")
        if self.portals:
            required.append("portal")
        if self.links:
            required.append("door")
        return required

    def closed_doors(self, player: tuple[int, int], boxes: frozenset[tuple[int, int]]) -> set[tuple[int, int]]:
        return {
            door
            for plate, door in self.links
            if plate not in boxes and player != plate and player != door and door not in boxes
        }

    def is_passable(self, pos: tuple[int, int], player: tuple[int, int], boxes: frozenset[tuple[int, int]]) -> bool:
        x, y = pos
        if x < 0 or x >= self.width or y < 0 or y >= self.height:
            return False
        if self.base[pos] in ("Wall", "Empty"):
            return False
        return pos not in self.closed_doors(player, boxes)

    def teleport(self, pos: tuple[int, int], direction: tuple[int, int], flags: set[str]) -> tuple[int, int]:
        if pos in self.portals:
            flags.add("portal")
            dx, dy = direction
            exit_pos = self.portals[pos]
            return (exit_pos[0] + dx, exit_pos[1] + dy)
        return pos

    def slide(self, pos: tuple[int, int], direction: tuple[int, int], player: tuple[int, int],
              boxes: frozenset[tuple[int, int]], flags: set[str]) -> tuple[int, int]:
        while self.base.get(pos, "Wall") == "Ice":
            flags.add("ice")
            dx, dy = direction
            next_pos = self.teleport((pos[0] + dx, pos[1] + dy), direction, flags)
            if not self.is_passable(next_pos, player, boxes) or next_pos in boxes:
                break
            pos = next_pos
        return pos

    def step(self, state: tuple[tuple[int, int], frozenset[tuple[int, int]], frozenset[str]],
             command: str) -> tuple[tuple[int, int], frozenset[tuple[int, int]], frozenset[str]] | None:
        player, boxes, flags_in = state
        boxes_mut = set(boxes)
        flags = set(flags_in)
        direction = DIRS[command]
        dx, dy = direction

        dest = self.teleport((player[0] + dx, player[1] + dy), direction, flags)
        if not self.is_passable(dest, player, boxes):
            return None

        if dest in boxes_mut:
            box_dest = self.teleport((dest[0] + dx, dest[1] + dy), direction, flags)
            if not self.is_passable(box_dest, player, boxes) or box_dest in boxes_mut:
                return None
            box_dest = self.slide(box_dest, direction, player, boxes, flags)
            if box_dest == dest:
                return None
            boxes_mut.remove(dest)
            boxes_mut.add(box_dest)

        new_boxes = frozenset(boxes_mut)
        new_player = self.slide(dest, direction, player, new_boxes, flags)
        for plate, door in self.links:
            if plate in new_boxes or new_player == plate or new_player == door or door in new_boxes:
                flags.add("door")

        return new_player, new_boxes, frozenset(flags)

    def is_complete(self, boxes: frozenset[tuple[int, int]]) -> bool:
        return bool(self.goals) and self.goals.issubset(boxes)


def parse_level_setup(path: Path) -> list[Level]:
    text = path.read_text(encoding="utf-8")
    pattern = re.compile(
        r'Def\("([^"]+)",\s*"([^"]+)",\s*(\d+),\s*new\[\]\s*\{(.*?)\}\)',
        re.DOTALL,
    )
    levels: list[Level] = []
    for match in pattern.finditer(text):
        level_id, name, par_text, rows_text = match.groups()
        rows = re.findall(r'"([^"]*)"', rows_text)
        levels.append(Level(level_id, name, int(par_text), rows))
    if not levels:
        raise ValueError(f"No level definitions found in {path}")
    return levels


def solve(level: Level, max_depth: int = 260, forbidden: str | None = None) -> tuple[str | None, set[str]]:
    assert level.player is not None
    start = (level.player, frozenset(level.boxes), frozenset())
    queue = deque([(start, "")])
    seen = {(start[0], start[1])}

    while queue:
        state, path = queue.popleft()
        player, boxes, flags = state
        if level.is_complete(boxes):
            return path, set(flags)
        if len(path) >= max_depth:
            continue

        for command in "UDLR":
            next_state = level.step(state, command)
            if next_state is None:
                continue
            if forbidden is not None and forbidden in next_state[2]:
                continue
            key = (next_state[0], next_state[1])
            if key in seen:
                continue
            seen.add(key)
            queue.append((next_state, path + command))

    return None, set()


def main() -> None:
    root = Path(__file__).resolve().parents[1]
    levels = parse_level_setup(root / "Assets" / "Editor" / "LevelSetupUtility.cs")
    failures: list[str] = []

    for level in levels:
        if len(level.boxes) != len(level.goals):
            failures.append(f"{level.level_id}: box/goal count mismatch")

        solution, flags = solve(level)
        if solution is None:
            failures.append(f"{level.level_id}: no solution found")
            continue

        required = level.required_mechanics()
        for mechanic in required:
            bypass, _ = solve(level, forbidden=mechanic)
            if bypass is not None:
                failures.append(f"{level.level_id}: {mechanic} can be bypassed via {bypass}")

        flag_text = ",".join(sorted(flags)) if flags else "-"
        print(f"{level.level_id}: par={level.par:3d} best={len(solution):3d} uses={flag_text:17s} solution={solution}")

    if failures:
        print("\\nFAILURES")
        for failure in failures:
            print(f"- {failure}")
        raise SystemExit(1)

    print("\\nALL_LEVELS_OK")


if __name__ == "__main__":
    main()
