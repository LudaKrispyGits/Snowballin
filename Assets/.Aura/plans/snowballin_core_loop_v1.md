# Snowballin — Endless Downhill Core Loop

## Objective
Build the full playable loop: an auto-forward rolling snowball on an endless icy slope that steers left/right, jumps, grows while grounded, smashes weaker obstacles for points, dies on stronger ones, collects coins, scores distance/checkpoints/airtime, and spends gold in a P-paused store of 7 stacking session-permanent upgrades, with a retry screen that keeps those buffs.

## Decisions (locked)
- Forward motion: rigidbody on an inclined track, driven to a **target speed** so Move Speed / ramps / difficulty stay tunable.
- Art: Unity primitives now + a texture/lighting polish pass in the final step.
- Persistence: **session-only** — upgrades survive death/retry, reset when the application restarts (no PlayerPrefs).

## Changes
- `Assets/Scripts/Core/GameConfig.cs` (create) — ScriptableObject: slope angle 8°, base forward speed 12, steer strength, jump impulse, growth rate/cap, difficulty curves, per-upgrade step values + cost curve, checkpoint interval 100 m.
- `Assets/Scripts/Core/GameManager.cs` (create) — states Playing/Paused/Dead, P toggles pause, `GameOver()`/`Retry()`/`NewRun()`, C# events for UI.
- `Assets/Scripts/Core/CameraRig.cs` (create) — smooth slope-aligned follow behind/above the ball.
- `Assets/Scripts/Player/SnowballController.cs` (create) — Rigidbody control: forward speed, lateral steer, Space jump, ground check, reads stats from UpgradeService.
- `Assets/Scripts/Player/SnowballBody.cs` (create) — radius growth while grounded, collider radius sync, `Break()` shatter, radius query for obstacle checks.
- `Assets/Scripts/Track/TrackGenerator.cs` + `TrackChunk.cs` (create) — pooled chunk recycling along the slope line; spawn obstacles/coins/ramps from density curves.
- `Assets/Scripts/Gameplay/Obstacle.cs`, `Coin.cs`, `Ramp.cs` (create) — tiered obstacle break/kill logic, gold pickup, launch ramp.
- `Assets/Scripts/Score/ScoreSystem.cs` (create) — distance metres, 100 m checkpoints, destroy points, airtime points, gold.
- `Assets/Scripts/Store/UpgradeService.cs` + `UpgradeDefinition` (create) — 7 upgrades, level/stacking, cost curve, purchase validation.
- `Assets/Scripts/UI/HUD.cs`, `StoreUI.cs`, `RetryScreen.cs` (create) — UGUI built from C# onto one Canvas.
- `Assets/InputSystem_Actions.inputactions` (edit) — add `Pause` action to the Player map bound to `<Keyboard>/p`.
- `Assets/Scenes/SampleScene.unity` (edit) — managers, canvas, spawn points wired.
- `Assets/Prefabs/TrackChunk.prefab`, `Obstacle_*.prefab`, `Coin.prefab`, `Ramp.prefab`, `Assets/Materials/*.mat` (create).

## Relevant Assets and Quirks
- Unity **6000.6.4f1**, URP **17.6.0**; active pipeline is `Assets/Settings/PC_RPAsset.asset` assigned per quality level (`GraphicsSettings.m_CustomRenderPipeline` is empty — normal for the URP template). SampleScene already has Main Camera, Directional Light, Global Volume (DefaultVolumeProfile).
- `activeInputHandler: 1` → **new Input System only**; legacy `Input.GetKey` throws at runtime. Use InputActionAsset references.
- `Assets/InputSystem_Actions.inputactions` already defines Player/`Move` (A/D, arrows, leftStick) and Player/`Jump` (`<Keyboard>/space`, gamepad south), plus a UI map. Only `Pause` is missing.
- **Cinemachine is not installed** (package list confirmed) — camera follow must be a custom script.
- `Assets/Prefabs/Snowball.prefab`: Sphere mesh (515 verts), `SphereCollider` radius 0.5, `Assets/Materials/Snowball_Snow.mat` (URP/Lit, base (0.93,0.96,1), smoothness 0.15). **No Rigidbody yet.**
- No tags/layers defined beyond defaults — create tags `Player`/`Obstacle`/`Coin` and layers `Player`(8)/`Track`(9)/`Obstacle`(10).
- Growth must sync `SphereCollider.radius = 0.5 * transform.localScale.x` explicitly; obstacle checks compare `SnowballBody.Radius` against the obstacle's hardness radius, not raw scale.
- Endless slope = chunk positions follow the straight line `origin + forward*d - up*(d*tan 8°)`; recycle a chunk to the far line end when it passes the ball, so drift never accumulates.

## Steps
- [x] 1. Foundation: create `GameConfig` SO asset, `UpgradeService` with the 7 upgrade definitions (Move Speed, Size, Steer Control, Jump Velocity, Lucky Obstacle chance, Obstacle Rate, Ramp Rate), add `Pause` action to the Player map, create tags + `Player`/`Track`/`Obstacle` layers. [depends: none]
- [x] 2. Player + camera: add Rigidbody to `Snowball.prefab` (mass 1, linear drag 0.1, angular drag 0.05, interpolate, freeze rotation X/Z), write `SnowballController` (target forward speed along slope, A/D steer, Space jump with ground check) and `CameraRig`; `GameManager` state machine with `Debug.Log` on every state change. [depends: 1]
- [x] 3. Track: build `TrackChunk` prefab (20 m × 10 m inclined snow plane, Track layer) and `TrackGenerator` pooling 6 chunks along the 8° line; place ramps in Ramp-Rate-driven slots and log chunk recycling. [depends: 2]
- [x] 4. Obstacles: three tiers (0.6 m breakable, 1.2 m conditional, 2.0 m lethal), spawn density and tier-weight lerp with distance, Lucky-chance upgrade converts lethal→breakable (gold-tinted), collision → shatter + tier points or `SnowballBody.Break()` → `GameManager.GameOver()`. [depends: 3]
- [x] 5. Coins + score: `Coin` pickup with collection SFX hook, coin arcs per chunk, `ScoreSystem` (100 m checkpoints, destroyed-obstacle points, airtime accrual while `!isGrounded` after jump/ramp), `HUD` showing distance/score/gold/radius. [depends: 4]
- [x] 6. Pause-store + retry: single Canvas with HUD, Store panel toggled on P (7 rows: name, level, cost, Buy; gold deducted; applies live to the ball), Retry panel on death where Retry keeps UpgradeService levels and resets run score/gold. [depends: 5]
- [x] 7. Polish + verification pass: generate snow/ice textures (`manage_texture`) and PBR materials (`create_pbr_material`), emissive coin material, fog + Global Volume bloom, then run `unity_verification_agent` over the full loop. [depends: 6]
  - Note: ball rotation is left free (not frozen on X/Z) so it visibly rolls; the SphereCollider stays at radius 0.5 and the transform scales to 2r, which keeps the effective radius exact. `Player/Pause` was added to Assets/InputSystem_Actions.inputactions by editing the JSON directly, because the Input System API refuses asset edits while its actions are enabled. Scene, prefabs and materials are authored by Assets/Editor/SnowballinBuilder.cs + SnowballinPolish.cs (re-runnable).

## Verify
- Play mode: ball auto-rolls downhill, A/D steers inside the lane limits, Space jumps, radius visibly grows while grounded and shrinks on death, small obstacles shatter for points, large ones end the round, retry restores upgrade levels, P freezes the game and shows the store, buying Move Speed measurably raises forward speed, a 100 m checkpoint fires.
- `Debug.Log` lines present for: state change, chunk recycle, obstacle spawn tier, purchase, checkpoint, death cause — so failures can be traced from the console.
