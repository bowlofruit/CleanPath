# CleanPath Prototype

A 3D portrait corridor game for Unity 6 / URP. Charge mass-based projectiles, clear bush obstacles through chain infections, and hit the door to win.

## Setup

1. Open the project in **Unity 6 LTS** (`6000.x`) with **URP**.
2. Confirm **Project Settings > Player > Active Input Handling** is **Input System Package (New)**.
3. Open `Assets/_Project/Scenes/Prototype.unity` and press **Play**.

## Controls

- **Hold** pointer (mouse / touch): charge a shot; mass flows from the player ball into a growing projectile preview.
- **Release**: fire toward the **door**.
- **Restart**: button on the win/lose overlay.

## Gameplay

- The player ball stays at its scene start position; clearing bushes does not move it forward yet.
- **Win**: the projectile hits the door.
- **Lose**: you spend all mass on one shot, or run out of mass before reaching the goal.

## Architecture

- **GameBootstrap** — composition root; drives gameplay from a single `Update()` / `LateUpdate()`.
- **GameFlow** — state machine: `Ready → Charging → Firing → Resolving → Won/Lost`.
- **Core/** — pure logic (`InfectionSolver`, `MassModel`, infection graph solver).
- **ObstacleField** — scene bushes, neighbour graph, projectile hit tests.
- **GameConfig** — all tunable gameplay values (`Assets/_Project/Config/GameConfig.asset`).

## Tuning

| Section | Key fields |
|---------|------------|
| `Ball` | `startMass`, `playerRadius`, `radiusPerMassCbrt`, `minCriticalMassFrac` |
| `Shot` | `chargeRatePerSec`, `minHoldDuration`, `projectileSpeed`, `projectileAcceleration` |
| `Infection` | `energyPerMass`, `blastMultiplier`, `linkCost`, `costPerMeter`, `maxLinkGap`, `chainDelayPerHop` |

Level geometry (bushes, ball, door) comes from the scene.

## What I would do next

- Player forward movement along the corridor.
- Unit tests for `InfectionSolver` and obstacle hit logic.
- Audio, juice, and additional levels.
