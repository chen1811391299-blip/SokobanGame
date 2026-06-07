# 30-Level Sokoban Design Bible

This document defines the design standard for the built-in 30-level campaign. The goal is to make the project read like a product-ready puzzle game submission, not only a collection of solvable maps.

## Design Pillars

1. Every level teaches or tests one clear idea.
2. Difficulty rises through planning depth, not through visual confusion.
3. Early levels should make the player feel smart quickly.
4. Special mechanics should be introduced in isolation before they are combined.
5. Every built-in level must have a verified solution before delivery.
6. The par value should reflect a clean solution with a little room for human inefficiency.
7. Layout shape matters: silhouettes should be readable and memorable.

## Difficulty Structure

The 30 levels are divided into five acts of six levels each.

| Act | Levels | Difficulty | Purpose |
|---|---:|---|---|
| Act 1 | 01-06 | Simple | Teach core Sokoban reading: push direction, multiple goals, wall use, one-step planning |
| Act 2 | 07-12 | Medium | Introduce controlled constraints and first special mechanics |
| Act 3 | 13-18 | Hard | Combine mechanics and require ordering decisions |
| Act 4 | 19-24 | Medium Remix | Lower pressure after the hard act, remix mechanics with cleaner layouts |
| Act 5 | 25-30 | Hard Finale | Multi-stage dependency puzzles and final mastery checks |

## Mechanic Pacing

| Mechanic | First Use | Full Test | Notes |
|---|---:|---:|---|
| Basic push | 01 | First Push | Simple | Learn direct pushing and completion | Implemented |
| 02 | Many Goals | Simple | Understand multiple boxes and goals | Implemented |
| 03 | Turnaround Room | Simple+ | Teach indirect routing around a blocker in a compact room | Implemented |
| 04 | Portal Delivery | Simple | Introduce required box delivery through a paired portal | Implemented |
| 05 | Ice Runway | Simple | Introduce box sliding across a short ice strip | Implemented |
| 06 | Pressure Gate | Simple | Introduce a required pressure plate and linked door | Implemented |
| 07 | Frozen Dock | Medium | Use an ice lane as a required warehouse delivery tool | Implemented |
| 08 | Portal Vault | Medium | Deliver one box into a sealed vault through a paired portal | Implemented |
| 09 | Portal Gate Key | Medium | Use a key box to hold a pressure gate before portal delivery | Implemented |
| 10 | Ice Portal Gate | Medium | Confirm the first combined ice, portal, and pressure-gate delivery | Implemented |
| 11 | Portal Gate Switch | Medium | Hold a pressure gate with a key box, then resolve portal delivery | Implemented |
| 12 | Ice Gate Switch | Medium | Use ice to stop a box on a pressure plate before pushing through the door | Implemented |
| 13 | Reverse Portal Key | Hard | Teach reverse portal entry: push left into a portal to land a box on the far-side goal | Implemented |
| 14 | Frozen Key Detour | Hard | Use ice to carry the key box through a gate, then solve a lower detour around a wall block | Implemented |
| 15 | Portal Ice Split | Hard | Separate the puzzle into a portal-fed ice lane and a lower delivery pocket | Implemented |
| 16 | Locked Portal Bend | Hard | Combine all three mechanics in a bent layout where direct delivery is blocked | Implemented |
| 17 | Portal Courtyard Lock | Hard | Compact courtyard puzzle where the player must unlock the center gate, reroute through a portal, and return for the final delivery | Implemented |
| 18 | Twin Room Relay | Hard | Two-room relay that chains an ice key, a center gate, and a portal-side delivery | Implemented |
| 19 | Vertical Ice Portal | Medium Remix | A narrow vertical remix where portal entry sets up an ice chute delivery | Implemented |
| 20 | Ice Gate Switchback | Medium Remix | A switchback room where the ice key opens the gate before a lower box detour | Implemented |
| 21 | Portal Gate Atrium | Medium Remix | A small atrium loop that opens a pressure gate before a reverse portal delivery | Implemented |
| 22 | Return Ferry | Medium Remix | Use the portal as a player-positioning tool, clear a blocking box, then reverse-push the ice ferry before routing the blocker to the top goal | Implemented |
| 23 | Ice Portal Loop | Medium Remix | A small loop where portal positioning feeds a short ice chute without door pressure | Implemented |
| 24 | Gatehouse Exchange | Medium Remix | Single-mechanic pressure-gate exchange with a temporary key box, a replacement key box, and three deliveries | Implemented |
| 25 | Frozen Stopper Dock | Hard Finale | Pure ice relay where one stopper box is recovered, then the stopped box and lower ice box must be rerouted into separate goals | Implemented |
| 26 | Portal Swap Vault | Hard Finale | Pure portal vault where two box transfers and a final reverse transfer all depend on the same portal pair | Implemented |
| 27 | Gate Relay Vault | Hard Finale | Pressure gate plus portal vault where opening the gate, recovering the left target, and finishing two far right goals are interdependent | Implemented |
| 28 | Fill Order | Hard Finale | Pure Sokoban 5-box target-fill sequence where wrong early deposits can block the central delivery lane | Implemented |
| 29 | Dual Warehouse | Hard Finale | Pure Sokoban 6-box dual-warehouse dispatch where both target rooms compete for parking and sealing order | Implemented |
| 30 | Split Depot | Hard Finale | Pure Sokoban 7-box split-target finale using central recirculation and one extra interference box | Implemented |

## Verification Standard

For each implemented level, maintain at least one of:

- A recorded move sequence in `docs/level_redesign_notes.md`.
- A local solver verification note.
- A manual test confirmation after Unity playtesting.

When possible, levels should be checked both structurally and by solution path:

- tile count equals `width * height`
- exactly one player
- boxes equal goals
- portal pairs reference portal tiles
- doors reference valid pressure plates
- at least one solution exists
