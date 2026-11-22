# Snowballin — Wider Track, Auto-Speed and Difficulty Tuning

## Objective
Widen the track to 24 m with solid white border walls, remove Move Speed from the store in favour of a distance-based automatic speed ramp, raise the coin rate with distance, make small obstacles pass-through kills, push the large obstacle tier to 600 m, and retune steering and gravity.

## Decisions (locked)
- White walls are **solid 1.2 m colliders** on both chunk edges (the ball ricochets off them).
- Speed is **distance-based with a cap** (12 m/s → ~28 m/s).
- Track is **24 m wide**; obstacle/coin/ramp lanes re-spread across the new width.

## Changes
- `Assets/Scripts/Core/GameConfig.cs` — `trackWidth 24`, `lateralLimit 11`, new `wallHeight 1.2` / `wallThickness 0.6`, `baseLateralSpeed 6 → 9.5`, `lateralAcceleration 45 → 60`, new `gravityY -7`, `maxForwardSpeed 28`, `speedRampPerMeter 0.004`, `coinArcsMin 1` / `coinArcsMax 4` / `coinDistanceRamp 2000`, `largeObstacleUnlockDistance 600` / `largeObstacleWeightMax 0.25` / `largeWeightRampDistance 2000`, `difficultyDistance 1500 → 2500`, `obstacleCountMax 6 → 5`; delete `moveSpeedPerLevel`.
- `Assets/Scripts/Store/UpgradeService.cs` — drop `UpgradeType.MoveSpeed` and its `Catalog` entry (store becomes 6 rows), delete `ForwardSpeed`.
- `Assets/Scripts/Core/GameManager.cs` — add `CurrentForwardSpeed` (distance ramp, capped) and apply `Physics.gravity = new Vector3(0, config.gravityY, 0)` in `Awake`; update the NewRun speed log.
- `Assets/Scripts/Player/SnowballController.cs` — read `gm.CurrentForwardSpeed`; clamp to `trackWidth/2 - wallThickness - radius - 0.1` as a safety net behind the wall colliders.
- `Assets/Scripts/Gameplay/Obstacle.cs` — tier 0 becomes a **trigger** (`isTrigger`) resolved in `OnTriggerEnter`, so smalls pop with no physics response; tiers 1–2 stay solid and share one `Resolve(ball)` path.
- `Assets/Scripts/Track/TrackGenerator.cs` — lane ranges derived from `config.trackWidth`; coin arcs per chunk lerp `coinArcsMin → coinArcsMax` with distance; large-tier weight is 0 below 600 m and climbs to `largeObstacleWeightMax` over `largeWeightRampDistance`.
- `Assets/Editor/SnowballinBuilder.cs` — `BuildTrackChunkPrefab` adds `WallLeft`/`WallRight` (BoxCollider, `Wall_White` material, Track layer) and sizes ground/walls from `config.trackWidth`.
- `Assets/Materials/Wall_White.mat` (created by builder) — white ice wall material.
- `Assets/Prefabs/TrackChunk.prefab`, `Assets/Configs/GameConfig.asset`, `Assets/Scenes/SampleScene.unity` — rebuilt/rewired by re-running the builder.

## Relevant Assets and Quirks
- Unity 6000.6.4f1, URP 17.6.0, new Input System only. Config lives at `Assets/Configs/GameConfig.asset` (an existing asset — new fields fall back to their C# defaults, so they must exist before the builder/play test reads them; the builder only creates the asset when missing).
- `UpgradeType.MoveSpeed` is referenced by `UpgradeService.ForwardSpeed`, `SnowballController` (`gm.Upgrades.ForwardSpeed`) and `GameManager.NewRun`'s log — all three must change together or the project stops compiling.
- The store panel is generated from `UpgradeService.Catalog.Length`, so removing the entry shrinks the list with no layout edit.
- 3D `Rigidbody` has no per-body gravity scale, so lowering gravity means setting `Physics.gravity` globally (obstacles/coins have no Rigidbody, so only the ball is affected). Jump uses a velocity set, so the same jump velocity now arcs higher.
- Walls must be **children of the chunk root** and on the `Track` layer: `SnowballController.UpdateGroundedState` raycasts down and requires `GetComponentInParent<TrackChunk>()`.
- Tier-0 triggers still need a collider on the coin/ball side; the ball has the Rigidbody, so `OnTriggerEnter` fires on the obstacle.
- `SnowballinBuilder` is idempotent (`PrefabUtility.SaveAsPrefabAsset` overwrites) — re-running it is how the chunk prefab picks up walls and the new width. Re-run `SnowballinPolish` afterwards so `Track_Snow` keeps its albedo tiling.

## Steps
- [x] 1. Retune `Assets/Scripts/Core/GameConfig.cs` with the values above (24 m width, walls, steer 9.5 / 60, gravity -7, speed ramp + cap, coin arc curve, 600 m large-unlock and slow weight ramp), keeping every field public so the builder and inspector see it. [depends: none]
- [x] 2. Remove `UpgradeType.MoveSpeed` from `Assets/Scripts/Store/UpgradeService.cs` and add `GameManager.CurrentForwardSpeed` + `Physics.gravity` setup; switch `SnowballController` to the ramp speed and the wall-aware lateral clamp. Compile-check until clean. [depends: 1]
- [x] 3. Make tier 0 pass-through in `Assets/Scripts/Gameplay/Obstacle.cs` (`isTrigger` + `OnTriggerEnter`, shared resolve for tiers 1–2) and log `[Obstacle] Tier 0 ploughed through (no slow)` on each pop. [depends: 1]
- [x] 4. Update `Assets/Scripts/Track/TrackGenerator.cs`: width-derived lanes, distance-scaled coin arcs, and 600 m / slow-ramp large-tier weights with `Debug.Log` naming the tier, radius and distance. [depends: 3]
- [x] 5. Re-run `Assets/Editor/SnowballinBuilder.cs` (adds `Wall_White.mat` + `WallLeft`/`WallRight` to `TrackChunk.prefab`, rewrites ground width) then `SnowballinPolish.cs`, and save `Assets/Scenes/SampleScene.unity`. [depends: 4]
  - Note: the config asset already existed, so a temporary editor script pushed the new values into `GameConfig.asset` before the builder ran (builder only creates the asset when missing). `TrackChunk.prefab` bounds confirmed at 24 × 2.2 × 20 with 1.2 m walls; Track_Snow tiling raised to 8×8 to keep texel density on the wider slab.
- [x] 6. Verify the new tuning in play mode via `unity_verification_agent`. [depends: 5]
  - Note: `unity_verification_agent` returned **INCONCLUSIVE** — its autopilot never got a session answer within 60 s and reported it had played the wrong level, so it produced no verdicts. Verification was therefore done inline in play mode: ground scale 24×1×20 with `WallLeft/Right` (0.6×1.2×20 at ±11.7); ball clamped at x ±8.30 with a 3.00 m radius and stayed grounded; `Physics.gravity = -7`; speed bands 12→19 m/s logged at 0/250/…/1750 m; store showed 6 rows and a Steer purchase took 9.50→11.00 m/s; all 10 live tier-0 obstacles were triggers with “ploughed through (no slow)” logs; earliest tier 2 in a post-fix run was 666 m and one killed the round at 673 m; coin arcs logged 1 → 2 → 3 with distance.

## Verify
- Play mode: the slab is 24 m wide with white walls the ball visibly bumps off; the ball starts at 12 m/s and is measurably faster at 500 m and again at 1000 m, never above 28 m/s; A/D reach both walls and the ball stays on the slab at full size.
- Small (0.45 m) obstacles vanish on contact with a score pop and no drop below the target speed; a tier 2 obstacle ends the round; no tier 2 obstacle spawns before 600 m (check the `[Track] Obstacle tier 2 ... at Nm` logs).
- Coin arcs per chunk increase with distance; the store shows 6 rows with no Move Speed entry, and every remaining row still buys and applies.
- `Debug.Log` families remain traceable: state change, chunk recycle, obstacle tier/spawn distance, coin/gold changes, purchase, checkpoint, death cause, plus the new `[Game] Speed ...` band log.
