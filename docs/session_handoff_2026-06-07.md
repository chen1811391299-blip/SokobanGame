# Session Handoff - 2026-06-07

## Current Campaign State

- Levels 01-12 are the current kept baseline.
- Level 13-17 have now been redesigned again to avoid same-looking mechanism-stack layouts.
- Level 28-30 still remain for the next three-level batch.

## Redesigned Level 13-17 Data

```text
13 Reverse Portal Key
#############
#@  $ pD.   #
# #### ###  #
# A  $ .A   #
#############

14 Frozen Key Detour
############
#@ $~~pD.  #
# ### ###  #
#   $      #
#   ### .  #
############

15 Portal Ice Split
#############
#@ $ A~~~.  #
# ## ###    #
#   $ A     #
#   ### .   #
#############

16 Locked Portal Bend
##############
#@ $~~pD.    #
# ### ###    #
#  A $ #.A   #
#       ##   #
##############

17 Portal Courtyard Lock
#########
#@  #  .#
# $ #A  #
# ~ # # #
# ~pD $ #
#   # A.#
#       #
#########
```

- Files already updated:
  - `Assets/Editor/LevelSetupUtility.cs`
  - `Assets/Resources/Levels/Level_13.asset` through `Level_17.asset`
  - `docs/level_verification_summary.md`
  - `docs/level_design_bible_30.md`
  - `docs/level_redesign_notes.md`

## Batch Redesign Notes For Levels 15-30

The user asked to stop doing one level at a time and directly finish the remaining 15 levels. During validation, many pasted schematic levels were found to be unsolvable or bypassable under the current project rules.

Implemented after resuming:

- Level 13: `Level 13 - Reverse Portal Key`, par 16, solution `RRRRRRLDDLLL`.
- Level 14: `Level 14 - Frozen Key Detour`, par 34, solution `RRRRRLLLLDDRRRRRRRDRRUULDRDL`.
- Level 15: `Level 15 - Portal Ice Split`, par 22, solution `RRRRLLLLDDRRRRRDD`.
- Level 16: `Level 16 - Locked Portal Bend`, par 26, solution `RRLLDDDRRRRRULLRUURR`.
- Level 17: `Level 17 - Portal Courtyard Lock`, par 48, solution `DRURDDDRRRLDDRRUUUULLDLLULLURRRRRLDRRDD`.
- Level 18: `Level 18 - Twin Room Relay`, par 34, solution `RDLDRRRRUULLDLLLRUURRRURD`.
- Level 19: `Level 19 - Vertical Ice Portal`, par 22, solution `DDDDUUUULLDDDDDLL`.
- Level 20: `Level 20 - Ice Gate Switchback`, par 40, solution `RRRDDDRRULDLULLLLUURRRDDRDLUUURR`.
- Level 21: `Level 21 - Portal Gate Atrium`, par 22, solution `RRRRLLLLDDLLLULL`.
- Level 22: `Level 22 - Return Ferry`, par 48, solution `RRURUURRDDDLLULLLDDRRUDLLUURRDRUUDLLUURR`.
- Level 23: `Level 23 - Ice Portal Loop`, par 27, solution `DRLDDRRDRUUDDRURLDRUU`.
- Level 24: `Level 24 - Gatehouse Exchange`, par 58, solution `DDRRRLLLUURRRRRRDDLLLLLDDRRRURURUULLLLLLDDRRRRRR`.

Implemented in the 25-30 batch:
- Level 25: `Level 25 - Frozen Stopper Dock`, par 76, solution `DRRRURRDDDDDURDRDDLLLLLLRRUUUULDRRURDDDDLLLLULLDDRULURRRRRRRDD`.
- Level 26: `Level 26 - Portal Swap Vault`, par 90, solution `DDRUURRDDRUULDDLLUURLDDRRUURDDDRDLUULUULLDLDRRLDDRRUURDDDLDRRRURDUULULLLL`.
- Level 27: `Level 27 - Gate Relay Vault`, par 74, solution `RRRRRDURRDDDDDLLUUDRRUULLUURRDLULDLDRURRDDLLDLURURRUURDDDDLDR`.
- Level 28: `Level 28 - Fill Order`, par 160, pure Sokoban 5-box schematic provided by user.
- Level 29: `Level 29 - Dual Warehouse`, par 220, pure Sokoban 6-box schematic provided by user.
- Level 30: `Level 30 - Split Depot`, par 300, pure Sokoban 7-box schematic provided by user.

Still needs redesign or stricter validation:

- None in the current 30-level set.

## Technical Constraint Discovered

The current level generator supports portal pairs `A`, `B`, and `C`, but `BuildDoorLinks` in `Assets/Editor/LevelSetupUtility.cs` currently expects only one pressure plate and one door. Some later pasted schematics use multiple pressure plates and doors.

Recommended next implementation step:

1. Update `BuildDoorLinks` to support matching multiple pressure plates and doors by row-major order.
2. Update `tools/verify_builtin_levels.py` parsing and validation to handle multiple door links.
3. Redesign levels 15-30 with a controlled lane-based structure, then run per-level verification instead of one long all-level exhaustive search.
4. Write Level_15 through Level_30 assets from the generator definitions and verify tile counts to avoid black/empty tile mistakes.

## Process Note

A long-running Python solver process from the interrupted batch search was stopped after the user paused for the night. No partial level file edits were left from that batch attempt.
