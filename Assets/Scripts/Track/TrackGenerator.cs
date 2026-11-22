using System.Collections.Generic;
using UnityEngine;

namespace Snowballin
{
    /// <summary>
    /// Endless slope: a fixed pool of chunks is laid along the straight 8 degree descent line
    /// and recycled to the far end whenever it falls behind the ball.
    /// </summary>
    public class TrackGenerator : MonoBehaviour
    {
        public GameConfig config;
        public GameObject chunkPrefab;
        public GameObject obstaclePrefab;
        public GameObject coinPrefab;
        public GameObject rampPrefab;

        readonly List<TrackChunk> chunks = new List<TrackChunk>();

        Vector3 origin;
        float nextFrontDistance;

        public Vector3 DownhillDirection => Quaternion.Euler(config.slopeAngleDegrees, 0f, 0f) * Vector3.forward;
        public Vector3 SlopeNormal => Quaternion.Euler(config.slopeAngleDegrees, 0f, 0f) * Vector3.up;
        public Quaternion TrackRotation => Quaternion.Euler(config.slopeAngleDegrees, 0f, 0f);

        /// <summary>World Y of the slope surface at a given travelled distance.</summary>
        public float SurfaceHeightAt(float distance)
        {
            return (origin + DownhillDirection * distance).y;
        }

        void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
            Reposition(gm.Score.Distance);
        }

        /// <summary>Rebuilds the whole track and returns the ball spawn point.</summary>
        public Vector3 ResetTrack()
        {
            origin = transform.position;

            while (chunks.Count < config.chunkCount)
            {
                GameObject go = Instantiate(chunkPrefab, transform);
                go.name = $"TrackChunk_{chunks.Count}";
                TrackChunk chunk = go.GetComponent<TrackChunk>();
                if (chunk == null)
                {
                    Debug.LogError("[Track] Chunk prefab is missing the TrackChunk component.");
                    break;
                }
                chunks.Add(chunk);
            }

            float distance = -config.chunkLength; // one chunk behind the spawn point
            foreach (TrackChunk chunk in chunks)
            {
                chunk.Place(origin, DownhillDirection, SlopeNormal, TrackRotation, distance, config.chunkLength);
                Populate(chunk);
                distance += config.chunkLength;
            }

            nextFrontDistance = distance;
            return origin + SlopeNormal * (config.baseRadius + 0.8f);
        }

        void Reposition(float traveledDistance)
        {
            foreach (TrackChunk chunk in chunks)
            {
                if (traveledDistance - (chunk.DistanceStart + config.chunkLength) <= config.chunkLength) continue;

                chunk.Place(origin, DownhillDirection, SlopeNormal, TrackRotation, nextFrontDistance, config.chunkLength);
                nextFrontDistance += config.chunkLength;
                Populate(chunk);
                Debug.Log($"[Track] Recycled chunk to {chunk.DistanceStart:F0} m (ball at {traveledDistance:F0} m)");
            }
        }

        void Populate(TrackChunk chunk)
        {
            chunk.ClearContent();

            if (chunk.DistanceStart < 0f) return; // keep the spawn area clean

            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            float difficulty = Mathf.Clamp01(chunk.DistanceStart / config.difficultyDistance);
            float density = Mathf.Lerp(config.obstacleCountMin, config.obstacleCountMax, difficulty) * gm.Upgrades.ObstacleRateMultiplier;
            int obstacleCount = Mathf.RoundToInt(density);

            for (int i = 0; i < obstacleCount; i++)
            {
                SpawnObstacle(chunk, gm);
            }

            SpawnCoinArcs(chunk);
            SpawnRampIfLucky(chunk, gm);
        }

        void SpawnObstacle(TrackChunk chunk, GameManager gm)
        {
            ObstacleLevelDef def = PickLevel(chunk.DistanceStart);
            if (def == null)
            {
                Debug.LogWarning($"[Track] No obstacle levels configured — nothing spawned at {chunk.DistanceStart:F0} m.");
                return;
            }

            GameObject prefab = def.prefab != null ? def.prefab : obstaclePrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"[Track] Obstacle level '{def.label}' has no prefab assigned and there is no fallback.");
                return;
            }

            // Only the hard levels can roll lucky (breakable no matter the ball size).
            bool lucky = def.level >= 5 && Random.value < gm.Upgrades.LuckyChance;

            // Spread obstacles across the widened slab, always keeping the whole obstacle inside the walls.
            float maxX = Mathf.Max(0.5f, config.trackWidth * 0.5f - config.wallThickness - def.radius - 0.5f);

            GameObject go = Instantiate(prefab, chunk.contentRoot);
            go.name = $"Obstacle_L{def.level}_{def.label.Replace(' ', '_')}";
            go.transform.localPosition = new Vector3(Random.Range(-maxX, maxX), def.radius, Random.Range(-7f, 7f));
            go.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            Obstacle obstacle = go.GetComponent<Obstacle>();
            if (obstacle == null)
            {
                Debug.LogError($"[Track] Prefab for obstacle level '{def.label}' is missing the Obstacle component.");
                return;
            }

            obstacle.config = config;
            obstacle.Initialize(def, lucky);

            Debug.Log($"[Track] Obstacle {def.label} L{def.level} r{def.radius:F2}{(lucky ? " (LUCKY/breakable)" : string.Empty)} " +
                      $"at {chunk.DistanceStart + go.transform.localPosition.z:F0} m");
        }

        /// <summary>Weighted roll over the configured levels; a level is invisible before its unlock distance.</summary>
        ObstacleLevelDef PickLevel(float distance)
        {
            if (config.obstacleLevels == null || config.obstacleLevels.Length == 0) return null;

            float total = 0f;
            foreach (ObstacleLevelDef def in config.obstacleLevels)
            {
                if (def != null) total += WeightFor(def, distance);
            }

            if (total <= 0f) return config.obstacleLevels[0];

            float roll = Random.value * total;
            ObstacleLevelDef chosen = config.obstacleLevels[0];
            float cursor = 0f;

            foreach (ObstacleLevelDef def in config.obstacleLevels)
            {
                if (def == null) continue;
                cursor += WeightFor(def, distance);
                if (roll <= cursor)
                {
                    chosen = def;
                    break;
                }
            }

            return chosen;
        }

        float WeightFor(ObstacleLevelDef def, float distance)
        {
            if (distance < def.unlockDistance) return 0f;

            // Levels available from the first chunk keep their full weight; later ones creep in slowly.
            if (def.unlockDistance <= 0f) return Mathf.Max(0.01f, def.maxWeight);

            return Mathf.Clamp01((distance - def.unlockDistance) / Mathf.Max(1f, def.weightRampDistance)) * def.maxWeight;
        }

        void SpawnCoinArcs(TrackChunk chunk)
        {
            if (coinPrefab == null) return;

            // Coins get denser the longer the run goes.
            float t = Mathf.Clamp01(chunk.DistanceStart / Mathf.Max(1f, config.coinDistanceRamp));
            int arcs = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(config.coinArcsMin, config.coinArcsMax, t)));

            float maxX = Mathf.Max(0.5f, config.trackWidth * 0.5f - config.wallThickness - 0.6f);

            for (int a = 0; a < arcs; a++)
            {
                float laneX = Random.Range(-maxX, maxX);
                float startZ = Random.Range(-8f, 4f);

                for (int i = 0; i < config.coinArcCount; i++)
                {
                    GameObject coin = Instantiate(coinPrefab, chunk.contentRoot);
                    coin.name = "Coin";
                    coin.transform.localPosition = new Vector3(laneX, 0.75f, startZ + i * 1.2f);
                }
            }

            Debug.Log($"[Track] {arcs} coin arc(s) in chunk at {chunk.DistanceStart:F0} m");
        }

        void SpawnRampIfLucky(TrackChunk chunk, GameManager gm)
        {
            if (rampPrefab == null) return;
            if (Random.value >= gm.Upgrades.RampChance) return;

            GameObject ramp = Instantiate(rampPrefab, chunk.contentRoot);
            ramp.name = "Ramp";
            float rampMaxX = Mathf.Max(0.5f, config.trackWidth * 0.5f - config.wallThickness - 2f);
            ramp.transform.localPosition = new Vector3(Random.Range(-rampMaxX, rampMaxX), 0.25f, Random.Range(-6f, 6f));
            ramp.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
            Debug.Log($"[Track] Ramp spawned at {chunk.DistanceStart + ramp.transform.localPosition.z:F0} m");
        }
    }
}
