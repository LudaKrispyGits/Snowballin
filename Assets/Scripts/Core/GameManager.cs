using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Snowballin
{
    public enum GameState
    {
        Playing,
        Paused,
        Dead
    }

    /// <summary>
    /// Owns game state, the run lifecycle (spawn / death / retry), the session-persistent
    /// upgrade wallet and the runtime-built UI canvas.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Wiring")]
        public GameConfig config;
        public InputActionAsset inputActions;
        public GameObject snowballPrefab;
        public TrackGenerator trackGenerator;
        public CameraRig cameraRig;

        public UpgradeService Upgrades { get; private set; }
        public ScoreSystem Score { get; private set; }
        public SnowballBody Ball { get; private set; }
        public SnowballController Controller { get; private set; }
        public GameState State { get; private set; } = GameState.Playing;

        /// <summary>Forward target speed for this run: base + distance ramp + size bonus, capped, then slowed.</summary>
        public float CurrentForwardSpeed =>
            Mathf.Min(config.maxForwardSpeed,
                config.baseForwardSpeed
                + Score.Distance * config.speedRampPerMeter
                + BallRadius * config.speedPerRadius)
            * SpeedMultiplier;

        /// <summary>1 while running freely; drops to the same-size impact factor for a moment after a hit.</summary>
        public float SpeedMultiplier => slowTimer > 0f ? slowFactor : 1f;

        public float BallRadius => Ball != null ? Ball.Radius : Upgrades.StartRadius;

        public event Action<GameState> StateChanged;
        public event Action RunStarted;

        InputAction pauseAction;
        HUD hud;
        StoreUI storeUI;
        RetryScreen retryScreen;
        int lastSpeedBand = -1;
        float goldAccumulator;
        int passiveGoldEarned;
        float slowTimer;
        float slowFactor = 1f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (config == null)
            {
                Debug.LogError("[Game] GameManager has no GameConfig assigned.");
                enabled = false;
                return;
            }

            Upgrades = new UpgradeService(config);
            Score = new ScoreSystem(config);

            Physics.gravity = new Vector3(0f, config.gravityY, 0f);
            Debug.Log($"[Game] Physics gravity set to {Physics.gravity.y:F1} m/s^2");

            if (inputActions != null)
            {
                InputActionMap map = inputActions.FindActionMap("Player", true);
                pauseAction = map.FindAction("Pause", false);
                if (pauseAction == null)
                {
                    Debug.LogWarning("[Game] No 'Pause' action found on the Player map — P will not pause.");
                }
                else
                {
                    pauseAction.Enable();
                }
            }
            else
            {
                Debug.LogError("[Game] GameManager has no InputActionAsset assigned.");
            }

            BuildUI();
        }

        void Start()
        {
            NewRun();
        }

        void Update()
        {
            if (slowTimer > 0f) slowTimer -= Time.deltaTime;

            if (pauseAction != null && pauseAction.WasPressedThisFrame())
            {
                TogglePause();
            }

            if (State == GameState.Playing && Ball != null && Controller != null)
            {
                Score.Tick(Time.deltaTime, Ball.transform.position, Controller.IsGrounded);

                // Passive gold: the bigger the ball, the faster it earns (and the store's gold upgrade stacks on top).
                goldAccumulator += BallRadius * config.goldPerSecondPerRadius * Upgrades.GoldMultiplier * Time.deltaTime;
                if (goldAccumulator >= 1f)
                {
                    int earned = Mathf.FloorToInt(goldAccumulator);
                    goldAccumulator -= earned;
                    Upgrades.AddGold(earned);
                    passiveGoldEarned += earned;

                    if (passiveGoldEarned >= 5)
                    {
                        Debug.Log($"[Economy] Passive +{passiveGoldEarned} gold (radius {BallRadius:F2} m at {Score.Distance:F0} m)");
                        passiveGoldEarned = 0;
                    }
                }

                int speedBand = Mathf.FloorToInt(CurrentForwardSpeed);
                if (speedBand != lastSpeedBand)
                {
                    lastSpeedBand = speedBand;
                    Debug.Log($"[Game] Speed band -> {speedBand} m/s at {Score.Distance:F0} m");
                }

                // Safety net: if the ball leaves the slope surface entirely, end the round instead of falling forever.
                if (trackGenerator != null)
                {
                    float surfaceY = trackGenerator.SurfaceHeightAt(Score.Distance);
                    if (Ball.transform.position.y < surfaceY - 4f)
                    {
                        Ball.Break("fell off the track");
                    }
                }
            }
        }

        void BuildUI()
        {
            Canvas canvas = UIFactory.CreateCanvas("Snowballin UI");
            hud = UIFactory.CreateComponent<HUD>(canvas.transform, "HUD");
            storeUI = UIFactory.CreateComponent<StoreUI>(canvas.transform, "Store Panel");
            retryScreen = UIFactory.CreateComponent<RetryScreen>(canvas.transform, "Retry Panel");

            if (hud != null) StateChanged += hud.OnStateChanged;
            if (storeUI != null) StateChanged += storeUI.OnStateChanged;
            if (retryScreen != null) StateChanged += retryScreen.OnStateChanged;
        }

        public void NewRun()
        {
            Time.timeScale = 1f;

            if (Ball != null)
            {
                Destroy(Ball.gameObject);
                Ball = null;
                Controller = null;
            }

            if (snowballPrefab == null || trackGenerator == null || cameraRig == null)
            {
                Debug.LogError("[Game] Missing snowballPrefab / trackGenerator / cameraRig reference.");
                return;
            }

            Vector3 spawnPoint = trackGenerator.ResetTrack();

            GameObject ballObject = Instantiate(snowballPrefab);
            ballObject.name = "Snowball";

            Ball = ballObject.GetComponent<SnowballBody>();
            Controller = ballObject.GetComponent<SnowballController>();

            if (Ball == null || Controller == null)
            {
                Debug.LogError("[Game] Snowball prefab is missing SnowballBody / SnowballController.");
                return;
            }

            Ball.Initialize(spawnPoint, Upgrades.StartRadius);
            Controller.DownhillDirection = trackGenerator.DownhillDirection;
            cameraRig.SetTarget(ballObject.transform);

            Score.Reset(spawnPoint, trackGenerator.DownhillDirection);
            lastSpeedBand = -1;
            goldAccumulator = 0f;
            passiveGoldEarned = 0;
            slowTimer = 0f;
            slowFactor = 1f;
            SetState(GameState.Playing);
            RunStarted?.Invoke();

            Debug.Log($"[Game] New run started — speed {CurrentForwardSpeed:F1} m/s (+{config.speedPerRadius:F1} per radius m, cap {config.maxForwardSpeed:F0}), start radius {Upgrades.StartRadius:F2} m, jump height {Upgrades.JumpHeight:F2} m, gold {Upgrades.Gold}");
        }

        public void Retry()
        {
            Debug.Log("[Game] Retry pressed — upgrades and gold kept.");
            NewRun();
        }

        public void TogglePause()
        {
            if (State == GameState.Playing)
            {
                Time.timeScale = 0f;
                SetState(GameState.Paused);
            }
            else if (State == GameState.Paused)
            {
                Time.timeScale = 1f;
                SetState(GameState.Playing);
            }
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        /// <summary>
        /// Timed forward-speed penalty from hitting an obstacle the ball is the same size as.
        /// The longer of the two durations wins so overlapping hits cannot shorten the slow.
        /// </summary>
        public void ApplySlow(float factor, float duration)
        {
            slowFactor = Mathf.Clamp(factor, 0.05f, 1f);
            slowTimer = Mathf.Max(slowTimer, duration);
            Debug.Log($"[Game] Slowed to x{slowFactor:F2} speed for {slowTimer:F2}s at {Score.Distance:F0} m");
        }

        public void GameOver(string cause)
        {
            if (State == GameState.Dead) return;

            Debug.Log($"[Game] Round over — {cause} after {Score.Distance:F0} m, score {Score.Score}, gold {Upgrades.Gold}");
            Time.timeScale = 0f;
            SetState(GameState.Dead);
        }

        void SetState(GameState state)
        {
            if (State == state) return;
            Debug.Log($"[Game] State: {State} -> {state}");
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
