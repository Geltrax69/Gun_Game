# Low Poly Arsenal & Robot Squad

Open `Assets/FPS/Scenes/MainScene.unity` and press Play. The scene now includes all five new weapons in the player's starting inventory, plus eight robots (two of each new archetype). The existing enemies, pickups, level and objectives remain usable.

## Controls

- Left mouse: fire (hold for Pulse Blaster).
- Right mouse: aim; Longshot gives a strong sniper zoom.
- Number keys 1–6 or mouse wheel: switch weapons. Slot 1 is the original Blaster; slots 2–6 are the new weapons below.
- Weapons regenerate ammunition after a short pause. Longshot carries one shot and recharges in about 2.7 seconds.
- Existing movement and pause controls continue to apply.

## New weapons

| Slot | Weapon | Damage | Fire interval | Capacity | Role |
| --- | --- | --- | --- | --- | --- |
| 2 | Viper Pistol | 28 | 0.22 s | 12 | Accurate semiautomatic sidearm |
| 3 | Bulldog Hand Cannon | 80 | 0.65 s | 6 | Heavy pistol with stronger recoil |
| 4 | Pulse Blaster | 18 | 0.09 s | 30 | Automatic plasma fire |
| 5 | Breach Scattergun | 13 × 9 pellets | 0.85 s | 5 | Close-range spread |
| 6 | Longshot Rail Sniper | 500 | 1.6 s minimum | 1 | Zero spread; one-hit kills against all current robot types |

### Recoil and jumping accuracy

Firing kicks the actual camera aim upward with a small random sideways pull, plus a separate weapon kick. The hand cannon, scattergun and sniper kick harder than the automatic blaster. Recoil settles smoothly after a shot; mouse input still controls the view during recovery.

Moving increases spread, jumping adds a larger accuracy penalty, and repeated fire adds temporary shot bloom. Right-click aiming and grounded crouching improve accuracy. Aiming does not remove the airborne penalty, so a scoped jump shot can miss. The crosshair widens to show current spread and contracts when accuracy recovers. The sniper still deals 500 damage on a hit, and its first stationary shot has zero spread.

Per-weapon tuning is exposed in WeaponController under **Recoil and Accuracy**. Existing enemy weapons retain their own configured spread and do not receive player movement penalties.

New weapons have their own geometric models, color accents, HUD silhouettes, synthesized firing sounds and slide recoil. Damage is applied through the existing projectile and Damageable systems; invincibility still works normally.

## Robot types

| Robot | Health | Movement | Attack |
| --- | --- | --- | --- |
| Wisp Scout Drone | 55 | Fast weaving, hovering chassis, spinning rotors | Light energy bolts |
| Strider Assault Walker | 120 | Flanks and strafes, alternating leg motion | Sustained medium-range shots |
| Bastion Siege Tank | 320 | Slow armored advance | Heavy cannon |
| Needle Rail Sentry | 90 | Keeps distance and retreats when approached | 0.9-second visible charge, then precision shot |

Robots patrol around their starting positions when idle, pursue detected players, respect line of sight, recoil while firing, flash when hit and collapse when destroyed. Their agents use the existing baked NavMesh, and all eight starting positions were checked for clearance and a complete path from the player. The drones hover visually while following the ground navigation mesh; they do not fly freely over obstacles.

## Editing and rebuilding

All reusable assets are under `Assets/LowPolyExpansion/`:

- `Weapons/`: five weapon prefabs.
- `Robots/`: four enemy prefabs.
- `Projectiles/`: player and robot projectile variants.
- `Materials/`, `Icons/`, `Audio/`: supporting assets.
- `Scripts/RobotCombatBrain.cs`: movement, attack strategy and robot animation.
- `Scripts/WeaponSlideMotion.cs`: weapon slide recoil.

Tune damage and projectile speed on projectile prefabs. Tune health on the robot Health component, movement speed on NavMeshAgent, and preferred range on RobotCombatBrain.

The Unity menu **Tools → Low Poly Expansion** provides asset rebuilding, a play-mode smoke test and model preview capture. Rebuilding overwrites generated prefabs and materials; use it before making manual changes to those assets, or keep copies of customized versions. The installer runs once after the first script import, with `Installed.txt` recording completion.

The original on-disk MainScene is backed up in `Documentation/Backups/MainScene.before-expansion.unity`. The latest live test result is in `Documentation/ExpansionValidation.txt`.
