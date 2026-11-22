# Snowballin — Obstacle Levels, Jump Tuning and Store Rework

## Objective
Replace the 3 generic obstacle tiers with 7 themed, distance-gated obstacle levels (snow mound → skyscraper) each with its own size rule, tune jump/gravity so the ball hangs longer, and rework the store (Size Growth multiplier + Gold Multiplier, Ramp Rate removed).

## Changes
- `Assets/Scripts/Core/GameConfig.cs` — add `[Serializable] ObstacleLevelDef[] obstacleLevels` (label, level, radius, unlockDistance, weightRampDistance, maxWeight, points, grantsGrowth, growthPerHit) + `sameSizeBand 0.10`, `slowSpeedFactor 0.5`, `slowDuration 0.6`, `growthMultiplierPerLevel 0.15`, `goldMultiplierPerLevel 0.25`; delete `small/medium/largeObstacleRadius`, `largeObstacleUnlockDistance`, `largeWeightRampDistance`, `largeObstacleWeightMax`, `breakMargin`, `small/medium/largePoints`, `rampChancePerLevel`; retune `gravityY -3.2`, `baseJumpHeight 3.2`.
- `Assets/Scripts/Gameplay/Obstacle.cs` — (rewrite) level-def driven, all colliders triggers, three outcomes (break / slow / plough+growth), footprint-scaled visual child.
- `Assets/Scripts/Gameplay/NpcWalkAnim.cs` — (create) procedural bob + sway + lean so the level-3 NPC reads as walking.
- `Assets/Scripts/Track/TrackGenerator.cs` — weighted per-level spawn using `obstacleLevels` + each def's prefab; `obstaclePrefab` field becomes level-1 fallback.
- `Assets/Scripts/Player/SnowballBody.cs` — `Grow` multiplies by `gm.Upgrades.GrowthMultiplier`; new `AddRadius(float)` for snow-mound plough bumps.
- `Assets/Scripts/Store/UpgradeService.cs` — enum `GoldMultiplier` added, `RampRate` removed; `Size` → `SizeGrowth`; new `GrowthMultiplier`, `GoldMultiplier`; `RampChance` now just `config.rampChanceBase`.
- `Assets/Scripts/Gameplay/Coin.cs` + `Assets/Scripts/Core/GameManager.cs` — gold multiplier applied to pickups and passive gold; `ApplySlow()` timed forward-speed penalty folded into `CurrentForwardSpeed`.
- `Assets/Editor/SnowballinBuilder.cs` — build 7 obstacle prefabs, populate `obstacleLevels`, write retuned config, re-save scene (idempotent, safe to re-run).
- New model assets under `Assets/Generated_Models/`: `SnowMound`, `SmallRock`, `Npc`, `TallTree`, `SnowMound2`, `House`, `Skyscraper`.
- New prefabs under `Assets/Prefabs/Obstacles/`: `Obstacle_SnowMound`, `_Rock`, `_Npc`, `_Tree`, `_SnowMound2`, `_House`, `_Skyscraper`.

## Relevant Assets and Quirks
- Unity 6000.6.4f1, URP 17.6.0, Input System 1.20.0. Scene `Assets/Scenes/SampleScene.unity`; `Game` → `Track` holds `TrackGenerator`. Layers `Obstacle 10`, `Track 9`.
- `GameConfig.asset` already exists, so C# default changes never reach it — values must be written by the builder pass (or `manage_scriptable_object` patches) or set to zero/null fields silently (this is what previously zeroed `obstacleLevels`-shaped data).
- Ball scaling convention: `localScale = 2r`, `SphereCollider.radius` pinned at `0.5`; gameplay compares `SnowballBody.Radius`, never scale.
- Obstacle prefabs are currently a scaled cube (`Obstacle.Initialize` sets `localScale = radius*2`). For models this squashes a 6 m tree — so the new prefab root keeps scale 1 with a `SphereCollider` of `radius`, and the `Visual` child is scaled so its XZ bounds = `2*radius` with `bounds.min.y` pinned to the root's bottom.
- Because every obstacle becomes a trigger, the old tier-0 trigger hack disappears and there is never a physics bounce; the "slow" outcome is a scripted speed penalty (`MoveTowards` at `forwardAcceleration 30` would otherwise erase a one-off velocity cut in ~0.3 s).
- Generated models ship with their own material; only the snow mounds need the existing `Track_Snow`-style snow look.
- `StoreUI` enumerates `UpgradeService.Catalog`, so the row list follows automatically (card is sized for 6 rows — the new catalog is also 6).
- Existing user-edited config values (`trackWidth 80`, `slopeAngleDegrees 10`, `chunkLength 40`, `maxRadius 10.88`, coin-arc values) must be preserved.
- Session-only gold/levels rule unchanged: nothing may touch `PlayerPrefs`.

## Steps
- [x] 1. Rewrite `GameConfig.cs` with `ObstacleLevelDef` + the 7 patterned levels (radii 0.5 / 0.9 / 1.3 / 1.7 / 2.2 / 3.0 / 4.5; unlocks 0 / 0 / 150 / 300 / 350 / 450 / 600 m; maxWeights 1.0 / 0.7 / 0.5 / 0.35 / 0.3 / 0.2 / 0.12) and the new slow/growth/gold tunables; set `gravityY -3.2`, `baseJumpHeight 3.2`, snow-mound `grantsGrowth=true, growthPerHit=0.35` [depends: none]
- [x] 2. Generate the 7 obstacle models with `generate_model_from_text` (snow drift, ice-streaked boulder, bundled-up snow hiker, snow-laden pine, large snow drift, timber chalet, glass skyscraper) into `Assets/Generated_Models/<Name>/` [depends: none]
  - Note: 3D model generation is not included in this project's plan, so the seven themed obstacles were authored from Unity primitives plus two generated cone meshes (8-seg pine, 4-seg roof) inside `Assets/Editor/SnowballinObstacleBuilder.cs` instead. The NPC walks via the new procedural `NpcWalkAnim.cs` (swinging legs, body bob and sway).
- [x] 3. Rewrite `Obstacle.cs`: `Initialize(ObstacleLevelDef, lucky)`, trigger-only, ratio test with `sameSizeBand 0.10` → break / `ApplySlow`+points / plough+points+`AddRadius(growthPerHit)` when `grantsGrowth`; add `NpcWalkAnim.cs` and `Debug.Log("[Obstacle] …")` outcomes [depends: 1]
- [x] 4. Update `TrackGenerator.SpawnObstacle` to roll a weighted level from `obstacleLevels` (weight = 0 before `unlockDistance`, then ramps to `maxWeight`), instantiate that level's prefab and ground-align it [depends: 1,3]
- [x] 5. Store + body: `SnowballBody.Grow` × `GrowthMultiplier`, `AddRadius()`; `UpgradeService` catalog → Size Growth / Steer Control / Jump Height / Lucky Obstacles / Gold Multiplier / Obstacle Rate (`RampRate` gone); `Coin` + passive gold use `GoldMultiplier`; `GameManager.ApplySlow` + penalised `CurrentForwardSpeed` [depends: 1]
- [x] 6. Run a `SnowballinBuilder` pass: create the 7 `Assets/Prefabs/Obstacles/*.prefab` (root + `SphereCollider` trigger + `Obstacle` + scaled `Visual`), fill `GameConfig.obstacleLevels` with radii/prefabs/points, wire `TrackGenerator`, save `SampleScene` [depends: 2,4,5]
  - Note: implemented as `SnowballinObstacleBuilder.Execute()` — an idempotent pass that builds the seven prefabs, fills `obstacleLevels`, writes gravity -3.2 / jump 3.2 and the new band/slow/growth/gold values, points `TrackGenerator.obstaclePrefab` at the snow mound and saves the scene. Snow Mound radius trimmed 0.5 -> 0.4 m after the first play test, so the starting 0.5 m ball always ploughs it for growth instead of registering as "same size".
- [x] 7. Verify: `check_compile_errors` clean, then `unity_verification_agent` play-mode run — confirm launch ≈ 4.53 m/s with ~2.8 s airtime, a snow mound grants radius, a same-size hit slows, a bigger obstacle ends the round, and Store shows 6 rows incl. Gold Multiplier [depends: 6]
  - Note: the first verification run flagged that the jump barely lifted the ball at speed. Root cause: the slope's downhill component (`forward` on a 13° descent) was re-added to the vertical velocity every FixedUpdate, cancelling the launch. Fixed in `SnowballController.FixedUpdate` by using a flat horizontal basis while airborne so the ballistic Y is left to physics. Measured in play: launch v.y 4.53 m/s at 10.54 m/s forward, peak rise 3.15 m, airtime 4.27 s, `[Score] Airtime 4.27s (+213 pts)`.

## Verify
- Compile clean via `check_compile_errors`.
- Play `SampleScene`: console shows `[Player] Jump 3.20 m high -> launch velocity 4.53 m/s (gravity -3.2)` and the ball stays airborne ~2.8 s while forward speed keeps climbing.
- `[Track] Obstacle <label> L<level> r<radius> at <m> m` lines show only snow mounds/rocks early, trees ~300 m, houses ~450 m, skyscrapers past 600 m.
- Ploughing a snow mound logs `+0.35 m` and the ball visibly grows; an obstacle within ±10 % logs `slowed` and drops `[Game] Speed band`; a larger one ends the run with the retry screen and buffs intact.
- Store (P) lists 6 rows with Gold Multiplier present and Ramp Rate gone; a coin is worth ×`GoldMultiplier`.
