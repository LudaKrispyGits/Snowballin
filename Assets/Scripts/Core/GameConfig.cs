using System;
using UnityEngine;

namespace Snowballin
{
    /// <summary>
    /// One themed obstacle level. Levels are the hardness scale: a level only starts spawning
    /// after unlockDistance and its share of the roll climbs slowly toward maxWeight.
    /// </summary>
    [Serializable]
    public class ObstacleLevelDef
    {
        public string label = "Snow Mound";
        public int level = 1;
        public float radius = 0.5f;

        [Tooltip("Never spawns before this distance down the track (m).")]
        public float unlockDistance = 0f;

        [Tooltip("Distance over which this level's roll weight ramps from 0 up to maxWeight.")]
        public float weightRampDistance = 400f;

        [Tooltip("Relative roll weight once fully unlocked.")]
        public float maxWeight = 1f;

        public int points = 25;

        [Tooltip("True for snow mounds: ploughing this adds radius to the ball.")]
        public bool grantsGrowth;

        public float growthPerHit;

        [Tooltip("Prefab instantiated for this level (root: SphereCollider + Obstacle + 'Visual' child).")]
        public GameObject prefab;
    }

    /// <summary>
    /// Central tuning asset. One instance lives at Assets/Configs/GameConfig.asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Snowballin/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Track")]
        public float slopeAngleDegrees = 8f;
        public float trackWidth = 72f;
        public float chunkLength = 40f;
        public int chunkCount = 6;
        public float lateralLimit = 35f;
        public float wallHeight = 1.2f;
        public float wallThickness = 0.6f;

        [Header("Player")]
        public float baseForwardSpeed = 12f;

        /// <summary>Forward speed gained per metre travelled.</summary>
        public float speedRampPerMeter = 0.004f;

        /// <summary>Forward speed gained per metre of ball radius — the ball rolls faster as it grows.</summary>
        public float speedPerRadius = 3.5f;
        public float maxForwardSpeed = 34f;
        public float forwardAcceleration = 30f;
        public float baseLateralSpeed = 9.5f;
        public float lateralAcceleration = 60f;

        /// <summary>Global gravity applied every run (m/s^2 on Y). Only the ball has a Rigidbody.</summary>
        public float gravityY = -3.2f;

        /// <summary>Jump is authored as a height in metres and converted to launch velocity with the run's gravity.</summary>
        public float baseJumpHeight = 3.2f;
        public float baseRadius = 0.5f;
        public float growthPerSecond = 0.015f;
        public float maxRadius = 3f;

        [Header("Economy")]
        // Passive gold per second per metre of ball radius (1 = one coin a second at 1 m radius).
        public float goldPerSecondPerRadius = 0.35f;

        [Header("Camera")]
        // Extra view distance (as a fraction of the base offset) at max radius.
        public float cameraZoomOut = 0.9f;

        [Header("Obstacles")]
        public float obstacleCountMin = 4f;
        public float obstacleCountMax = 10f;
        public float difficultyDistance = 2500f;

        /// <summary>
        /// +/- fraction of the ball radius that counts as "same size": the ball is slowed and gains no growth.
        /// Below (1 - band) the obstacle is larger (run ends); above (1 + band) it is smaller (plough through).
        /// </summary>
        public float sameSizeBand = 0.1f;

        /// <summary>Forward-speed multiplier applied by a same-size impact (1 = no penalty).</summary>
        public float slowSpeedFactor = 0.5f;

        /// <summary>Seconds a same-size impact keeps the ball slowed.</summary>
        public float slowDuration = 0.6f;

        /// <summary>Themed obstacle levels, level 1 (softest) first.</summary>
        public ObstacleLevelDef[] obstacleLevels = new ObstacleLevelDef[0];

        public float luckyChancePerLevel = 0.05f;
        public int coinArcCount = 5;

        /// <summary>Coin arcs per chunk at the start of a run.</summary>
        public float coinArcsMin = 3f;

        /// <summary>Coin arcs per chunk once the run reaches coinDistanceRamp metres.</summary>
        public float coinArcsMax = 9f;
        public float coinDistanceRamp = 2000f;

        [Header("Ramps")]
        /// <summary>Chance per chunk that a jump ramp spawns.</summary>
        public float rampChanceBase = 0.45f;

        [Header("Score")]
        public float checkpointInterval = 100f;
        public int checkpointBonus = 100;
        public float airtimePointsPerSecond = 50f;

        [Header("Upgrade Costs")]
        public int baseUpgradeCost = 5;
        public float upgradeCostGrowth = 1.6f;

        [Header("Upgrade Effects (per level)")]
        /// <summary>Multiplicative bonus to the ball's growth rate per level (0.15 = +15% of base growth).</summary>
        public float growthMultiplierPerLevel = 0.15f;
        public float steerPerLevel = 1.5f;
        public float jumpHeightPerLevel = 0.3f;
        public float obstacleRateReductionPerLevel = 0.08f;
        public float goldMultiplierPerLevel = 0.25f;
    }
}
