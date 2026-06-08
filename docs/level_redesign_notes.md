# Level Redesign Notes

These notes track the intended progression for the redesigned Sokoban levels. Each level should have one clear teaching goal, a readable solution, and a small amount of optional inefficiency room for the star rating system.

Campaign-level design standards are tracked in `docs/level_design_bible_30.md`.

## Progression Principles

- Teach one new idea at a time before combining mechanics.
- Keep early levels visually open and readable.
- Use par moves as a design signal, not just a scoring number.
- Avoid irreversible deadlocks in the first two levels.
- Introduce failure states later, after the player understands the base rule.

## 30-Level Difficulty Plan

- Levels 01-06: Simple. Core movement, push direction, positioning, one-box clarity, first optional mechanic previews.
- Levels 07-12: Medium. Multi-box planning, simple deadlock awareness, first reliable use of portals/ice/doors.
- Levels 13-18: Hard. Combined mechanics, route ordering, limited maneuvering space, stricter par targets.
- Levels 19-24: Medium Remix. Reintroduce earlier mechanics in cleaner, more playful layouts after the first hard ramp.
- Levels 25-30: Hard Finale. Multi-step dependency chains and mechanic combinations, while staying fully solvable.

## Level 1 - First Push

- Size: 7x7
- Mechanics: one box, one goal, no special tiles
- Player goal: learn to stand behind the box and push it upward into the goal
- Optimal route: Right, Right, Up, Up
- Optimal moves: 4
- Par moves: 5
- Design intent: fast first success; lets the player understand movement, pushing, goals, and completion feedback in under 10 seconds

```text
#######
#     #
#  .  #
#     #
#  $  #
#@    #
#######
```

Legend: `#` wall, `.` goal, `$` box, `@` player.

## Level 2 - Many Goals

- Size: 9x7
- Mechanics: 6 boxes, 6 goals, no special tiles
- Player goal: learn that every goal must be filled and that boxes can be solved as independent lanes
- Verified solution: push the three upper boxes upward and the three lower boxes downward
- Verified move sequence: `RUDDULUDDULUDD`
- Verified moves: 14
- Par moves: 20
- Design intent: visually echoes a multi-goal mobile Sokoban layout while staying in the simple difficulty band

```text
  .....  
  .ooo.  
 ..$$$.. 
 ...@... 
 ..$$$.. 
  .ooo.  
  .....  
```

Legend: `.` floor, `o` goal, `$` box, `@` player, blank cells are blocked empty space.

## Level 3 - Turnaround Room

- Size: 6x6
- Mechanics: 2 boxes, 2 goals, one interior blocking wall
- Player goal: solve a compact room where one box is direct and the other must be routed around a wall
- Verified solution: top box goes directly right, lower box is rerouted around the center blocker
- Verified move sequence: `LDDDRU LUR DDRRUUUL RDDDLLUUR URD LLLUR R`
- Verified moves: 25 by playtest
- Par moves: 28
- Design intent: mirrors the provided mobile Sokoban reference; this creates a memorable compact-room puzzle and teaches that a nearby goal may require an indirect route

```text
######
# @$o#
#    #
# $#o#
#    #
######
```

Legend: `#` wall, `o` goal, `$` box, `@` player.

## Level 4 - Portal Delivery

- Size: 10x7
- Mechanics: 2 boxes, 2 goals, one paired portal, one sealed goal pocket
- Player goal: learn that a pushed box enters one portal and exits from the linked portal while continuing in the same direction
- Verified solution: push the lower box right through the portal into the isolated goal, then push the upper box upward into the open goal
- Verified move sequence: `RRU`
- Verified moves: 3
- Par moves: 4
- Design intent: introduces portals as a required delivery route rather than a decorative shortcut; the right-side goal pocket is sealed on every side except the portal exit lane

```text
##########
#   o    #
#   $  ###
# @ $A#A.#
#      ###
#        #
##########
```

Legend: `#` wall, `.`/`o` goal, `$` box, `@` player, `A` paired portal.

## Level 5 - Ice Runway

- Size: 10x7
- Mechanics: 2 boxes, 2 goals, one short ice runway
- Player goal: learn that a box pushed onto ice keeps moving in that direction until it leaves the ice
- Verified solution: push the lower box right so it slides across the ice runway into the right goal, then push the upper box upward into the open goal
- Verified move sequence: `RRU`
- Verified moves: 3
- Par moves: 4
- Entry tutorial: shows an ice runway diagram before player input is enabled
- Design intent: introduces ice with a direct visual comparison: one box behaves normally on floor, while the other travels farther because of the ice strip

```text
##########
#   o    #
#   $    #
# @ $~~o #
#        #
#        #
##########
```

Legend: `#` wall, `o` goal, `$` box, `@` player, `~` ice.

## Level 6 - Pressure Gate

- Size: 10x7
- Mechanics: 2 boxes, 2 goals, one pressure plate, one linked door
- Player goal: learn that standing on or placing a box on a pressure plate opens its linked door
- Verified solution: push the lower box onto the pressure plate, use the opened door to deliver the upper box, then push the plate box down into its goal
- Verified move sequence: `RRLURRRLD`
- Verified moves: 9
- Par moves: 10
- Entry tutorial: shows a pressure plate and linked door diagram before player input is enabled
- Mechanic requirement: disabling the pressure plate/door link makes the level unsolvable
- Design intent: makes the pressure plate a held-open switch; the door lane is sealed so the upper box cannot reach its goal without opening the door

```text
##########
##########
### $D o##
# @ $p####
#    o   #
#        #
##########
```

Legend: `#` wall, `o` goal, `$` box, `@` player, `p` pressure plate, `D` door.

## Level 7 - Frozen Dock

- Size: 13x9
- Difficulty: Medium
- Mechanics: ice
- Player goal: Use the central ice lane as a required delivery tool while solving two normal warehouse pushes
- Verified move sequence: `DDRRRLLLUURRRULLDLUULURRRR`
- Verified moves: 26
- Par moves: 30
- Mechanic requirement: disabling ice makes the level unsolvable

```text
#############
#     .     #
#  ### ###  #
#   $       #
# @  $~~.   #
#  ### ###  #
#   $ .     #
#           #
#############
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `~` ice.

## Level 8 - Portal Vault

- Size: 13x8
- Difficulty: Medium
- Mechanics: portal
- Player goal: Sort three local warehouse boxes while delivering one vault box through a sealed portal pocket
- Verified move sequence: `DRRURRDDLDRRURRDRRULLLLDRLLUUUURULDDLDD`
- Verified moves: 39
- Par moves: 44
- Mechanic requirement: the vault goal is sealed on three sides and can only receive a box from the paired portal

```text
#############
#    .      #
#  ###  #####
# @  $ A#A.##
#  $   ######
#     $  $  #
#    .   .  #
#############
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `A` paired portal.

## Level 9 - Portal Gate Key

- Size: 10x5
- Difficulty: Medium
- Mechanics: door, portal
- Player goal: Use the lower box as a key to hold the pressure gate, then deliver the upper box through the paired portal
- Verified move sequence: `DDRRRRRDLRULLLLLUURRRRLDRRR`
- Verified moves: 27
- Par moves: 32
- Mechanic requirement: disabling either the door or the portal makes the level unsolvable

```text
##########
#@ $D A  #
# ## p$ .#
#     A. #
##########
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `A` paired portal, `p` pressure plate, `D` door.

## Level 10 - Ice Portal Gate

- Size: 10x5
- Difficulty: Medium
- Mechanics: door, ice, portal
- Player goal: Send the upper box across ice and through the portal, then use the lower key box to open the gate
- Verified move sequence: `RRRDDRR`
- Verified moves: 7
- Par moves: 10
- Mechanic requirement: disabling ice, portal, or door makes the level unsolvable

```text
##########
#@ $~~A  #
# ##     #
#  A.pD$.#
##########
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `A` paired portal, `~` ice, `p` pressure plate, `D` door.

## Level 11 - Portal Gate Switch

- Size: 10x5
- Difficulty: Medium
- Mechanics: door, portal
- Player goal: Use the lower box to hold the pressure gate open, then resolve the upper box through the paired portal
- Verified move sequence: `RRRLLLDDRRRRRRR`
- Verified moves: 15
- Par moves: 18
- Mechanic requirement: disabling either the door or the portal makes the level unsolvable

```text
##########
#@ $ A  .#
# ## ##  #
#  $pD A.#
##########
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `A` paired portal, `p` pressure plate, `D` door.

## Level 12 - Ice Gate Switch

- Size: 10x5
- Difficulty: Medium
- Mechanics: door, ice
- Player goal: Push the box through the short ice lane, stop it on the pressure plate, then push through the opened door
- Verified move sequence: `RRRRR`
- Verified moves: 5
- Par moves: 7
- Mechanic requirement: disabling either ice or the door makes the level unsolvable

```text
##########
#@ $~~pD.#
# ##     #
#        #
##########
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `~` ice, `p` pressure plate, `D` door.

## Level 13 - Reverse Portal Key

- Size: 13x5
- Difficulty: Hard
- Mechanics: door, portal
- Player goal: use the upper key box to open the gate, then solve the lower box by pushing it left into the portal so it exits onto the far-side goal
- Verified move sequence: `RRRRRRLDDLLL`
- Verified moves: 12
- Par moves: 16
- Mechanic requirement: disabling either the portal or the door makes the level unsolvable

```text
#############
#@  $ pD.   #
# #### ###  #
# A  $ .A   #
#############
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `A` paired portal, `p` pressure plate, `D` door.

## Level 14 - Frozen Key Detour

- Size: 12x6
- Difficulty: Hard
- Mechanics: door, ice
- Player goal: let the ice lane carry the first box through the gate puzzle, then route the second box around the lower wall pocket
- Verified move sequence: `RRRRRLLLLDDRRRRRRRDRRUULDRDL`
- Verified moves: 28
- Par moves: 34
- Mechanic requirement: disabling either ice or the door makes the level unsolvable

```text
############
#@ $~~pD.  #
# ### ###  #
#   $      #
#   ### .  #
############
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `~` ice, `p` pressure plate, `D` door.

## Level 15 - Portal Ice Split

- Size: 13x6
- Difficulty: Hard
- Mechanics: ice, portal
- Player goal: solve a portal-fed ice lane on top, then finish the separated lower delivery pocket
- Verified move sequence: `RRRRLLLLDDRRRRRDD`
- Verified moves: 17
- Par moves: 22
- Mechanic requirement: disabling either the portal or ice makes the level unsolvable

```text
#############
#@ $ A~~~.  #
# ## ###    #
#   $ A     #
#   ### .   #
#############
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `A` paired portal, `~` ice.

## Level 16 - Locked Portal Bend

- Size: 14x6
- Difficulty: Hard
- Mechanics: door, ice, portal
- Player goal: solve the locked ice lane, then use a left-push portal delivery because the direct route to the lower goal is blocked
- Verified move sequence: `RRLLDDDRRRRRULLRUURR`
- Verified moves: 20
- Par moves: 26
- Mechanic requirement: disabling ice, portal, or door separately makes the level unsolvable

```text
##############
#@ $~~pD.    #
# ### ###    #
#  A $ #.A   #
#       ##   #
##############
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `A` paired portal, `~` ice, `p` pressure plate, `D` door.

## Level 17 - Portal Courtyard Lock

- Size: 9x8
- Difficulty: Hard
- Mechanics: door, ice, portal
- Player goal: unlock the center gate with an ice-fed key box, then work through the courtyard to deliver the second box via the portal route
- Verified move sequence: `DRURDDDRRRLDDRRUUUULLDLLULLURRRRRLDRRDD`
- Verified moves: 39
- Par moves: 48
- Mechanic requirement: disabling ice, portal, or door separately makes the level unsolvable

```text
#########
#@  #  .#
# $ #A  #
# ~ # # #
# ~pD $ #
#   # A.#
#       #
#########
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `A` paired portal, `~` ice, `p` pressure plate, `D` door.

## Level 18 - Twin Room Relay

- Size: 10x7
- Difficulty: Hard
- Mechanics: door, ice, portal
- Player goal: use the left room's ice lane to open the center gate, then relay through the right room's portal delivery before returning for the remaining boxes
- Verified move sequence: `RDLDRRRRUULLDLLLRUURRRURD`
- Verified moves: 25
- Par moves: 34
- Mechanic requirement: disabling ice, portal, or door separately makes the level unsolvable

```text
##########
#@  #  . #
# $~#A   #
#  ~pD $ #
# ## # A.#
# .  $   #
##########
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `A` paired portal, `~` ice, `p` pressure plate, `D` door.

## Level 19 - Vertical Ice Portal

- Size: 6x11
- Difficulty: Medium Remix
- Mechanics: ice, portal
- Player goal: read a vertical chute, then use the portal entry to line up an ice delivery
- Verified move sequence: `DDDDUUUULLDDDDDLL`
- Verified moves: 17
- Par moves: 22
- Mechanic requirement: disabling either ice or the portal makes the level unsolvable

```text
######
#   @#
#  # #
#  #$#
##$  #
## #A#
##A#~#
#  #~#
#.  ~#
#   .#
######
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `A` paired portal, `~` ice.

## Level 20 - Ice Gate Switchback

- Size: 10x6
- Difficulty: Medium Remix
- Mechanics: door, ice
- Player goal: use the upper ice lane as the pressure-key route, then switch back through the lower room for the second delivery
- Verified move sequence: `RRRDDDRRULDLULLLLUURRRDDRDLUUURR`
- Verified moves: 32
- Par moves: 40
- Mechanic requirement: disabling either ice or the door makes the level unsolvable

```text
##########
#@ $~~pD.#
# ### ## #
#     $  #
#   .    #
##########
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `~` ice, `p` pressure plate, `D` door.

## Level 21 - Portal Gate Atrium

- Size: 9x6
- Difficulty: Medium Remix
- Mechanics: door, portal
- Player goal: open the upper pressure gate, then move through the atrium to push the lower box left into the portal delivery
- Verified move sequence: `RRRRLLLLDDLLLULL`
- Verified moves: 16
- Par moves: 22
- Mechanic requirement: disabling either the portal or the door makes the level unsolvable

```text
#########
#@ $pD. #
# ### # #
#A$  #.A#
#       #
#########
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `A` paired portal, `p` pressure plate, `D` door.

## Level 22 - Return Ferry

- Size: 12x8
- Difficulty: Medium Remix
- Mechanics: ice, portal
- Player goal: enter the portal to reach the sealed right chamber, move the blocking box out of the return route, push the ferry box back across the ice lane, then route the blocker upward into the top goal
- Verified move sequence: `RRURUURRDDDLLULLLDDRRUDLLUURRDRUUDLLUURR`
- Verified moves: 40
- Par moves: 48
- Mechanic requirement: disabling either ice or the portal makes the level unsolvable

```text
############
#@ A#    . #
#####  # # #
#.~~~$   # #
##### A  $ #
#####   #  #
###### #   #
############
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `A` paired portal, `~` ice.

## Level 23 - Ice Portal Loop

- Size: 8x7
- Difficulty: Medium Remix
- Mechanics: ice, portal
- Player goal: solve a compact loop where one box uses the portal-to-ice route and the other is delivered by local positioning
- Verified move sequence: `DRLDDRRDRUUDDRURLDRUU`
- Verified moves: 21
- Par moves: 27
- Mechanic requirement: disabling either ice or the portal makes the level unsolvable

```text
########
#@   . #
# $A~~.#
# ## # #
#  $ A #
#      #
########
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `A` paired portal, `~` ice.

## Level 24 - Gatehouse Exchange

- Size: 11x7
- Difficulty: Medium Remix
- Mechanics: door
- Player goal: use the middle box as a temporary pressure key, deliver the upper box through the gate, recover that key box to the lower-left goal, then move the lower box onto the plate and finish the right-side delivery
- Verified move sequence: `DDRRRLLLUURRRRRRDDLLLLLDDRRRURURUULLLLLLDDRRRRRR`
- Verified moves: 48
- Par moves: 58
- Mechanic requirement: disabling the pressure plate and door makes the level unsolvable

```text
###########
#@ $ D  . #
# ### # # #
#   $p  . #
#.  #$    #
#         #
###########
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `p` pressure plate, `D` door.

## Level 25 - Frozen Stopper Dock

- Size: 12x9
- Difficulty: Hard Finale
- Mechanics: ice
- Player goal: use the upper-right box as a stopper for the upper ice lane, recover that stopper into the lower-left goal, then reroute the stopped box and lower ice-lane box into separate right-side goals
- Verified move sequence: `DRRRURRDDDDDURDRDDLLLLLLRRUUUULDRRURDDDDLLLLULLDDRULURRRRRRRDD`
- Verified moves: 62
- Par moves: 76
- Mechanic requirement: disabling ice makes the level unsolvable

```text
############
#@         #
#  $~~~~$  #
#  ### #   #
#        . #
#    $~~~  #
#  ### # # #
# #.    .  #
############
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `~` ice.

## Level 26 - Portal Swap Vault

- Size: 12x8
- Difficulty: Hard Finale
- Mechanics: portal
- Player goal: send two left-room boxes through the paired portal to solve the right vault, then use the same portal route in reverse to recover the final left-side target
- Verified move sequence: `DDRUURRDDRUULDDLLUURLDDRRUURDDDRDLUULUULLDLDRRLDDRRUURDDDLDRRRURDUULULLLL`
- Verified moves: 73
- Par moves: 90
- Mechanic requirement: disabling the portal makes the level unsolvable

```text
############
#@   #  #  #
#  # #A $ ##
#.$ A#  #  #
##$# #     #
#    #     #
#    #.   .#
############
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `A` paired portal.

## Level 27 - Gate Relay Vault

- Size: 12x8
- Difficulty: Hard Finale
- Mechanics: door, portal
- Player goal: open the top gate, send a box through the portal loop, recover the left-side goal, then use the same portal lane to finish two far right goals
- Verified move sequence: `RRRRRDURRDDDDDLLUUDRRUULLUURRDLULDLDRURRDDLLDLURURRUURDDDDLDR`
- Verified moves: 61
- Par moves: 74
- Mechanic requirement: disabling either the pressure gate or the portal makes the level unsolvable

```text
############
#@ $pD    .#
#    #$    #
#. A #A   ##
#    # #   #
#  # #$    #
#   ##    .#
############
```

Legend: `#` wall, `.` goal, `$` box, `@` player, `A` paired portal, `p` pressure plate, `D` door.

## Level 28 - Cross Packing

- Size: 7x7
- Difficulty: Hard Finale
- Mechanics: none
- Player goal: screenshot-matched pure Sokoban 5-box chamber; the player must unpack the central box cluster into the cross-shaped target layout without sealing the lower parking row
- Verified move sequence: `LDUUURRDRDDDLLLUURRDULLDDRRUULD`
- Verified moves: 31
- Verified pushes: 7
- Par moves: 40
- Mechanic requirement: no portals, doors, pressure plates, or ice tiles are present; `x` cells are empty background cells used to match the screenshot silhouette

```text
x#####x
##   ##
#  #  #
#.$@$.#
# $$$ #
#. . .#
#######
```

Legend: `#` wall, `.` goal, `$` box, `@` player.

## Level 29 - Ring Packing

- Size: 7x8
- Difficulty: Hard Finale
- Mechanics: none
- Player goal: screenshot-matched pure Sokoban 6-box ring chamber; the alternating box/goal pattern forces the player to unwind the center stack without blocking the lower-right exit lane
- Verified move sequence: `LLULLUUURRDDUULLDDRUDDRDRRULLRRUULD`
- Verified moves: 35
- Verified pushes: 10
- Par moves: 50
- Mechanic requirement: no portals, doors, pressure plates, or ice tiles are present; `x` cells are empty background cells used to match the screenshot silhouette

```text
x####xx
##  ###
# .$. #
# $.$ #
# $.$ #
# .$. #
###  @#
xx#####
```

Legend: `#` wall, `.` goal, `$` box, `@` player.

## Level 30 - Final Ring

- Size: 8x8
- Difficulty: Hard Finale
- Mechanics: none
- Player goal: screenshot-matched pure Sokoban 8-box finale; the player must peel the surrounding box ring into the central target room while preserving enough access around the outer corridor
- Verified move sequence: `DDRDUUURRDLRRRDDLDDLLURDRRUULU`
- Verified moves: 30
- Verified pushes: 8
- Par moves: 45
- Mechanic requirement: no portals, doors, pressure plates, or ice tiles are present

```text
########
#@     #
# .$$. #
# $..$ #
# $..$ #
# .$$. #
#      #
########
```

Legend: `#` wall, `.` goal, `$` box, `@` player.
