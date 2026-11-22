using UnityEngine;

namespace Snowballin
{
    /// <summary>
    /// Run-only scoring: distance metres, 100 m checkpoints, destroyed obstacles and airtime.
    /// </summary>
    public class ScoreSystem
    {
        readonly GameConfig config;

        public float Distance { get; private set; }
        public float BestDistance { get; private set; }
        public int Score { get; private set; }
        public int Checkpoints { get; private set; }
        public int CoinsCollected { get; private set; }

        Vector3 startPosition;
        Vector3 downhill;
        float airTime;

        public ScoreSystem(GameConfig config)
        {
            this.config = config;
        }

        public void Reset(Vector3 startPosition, Vector3 downhillDirection)
        {
            this.startPosition = startPosition;
            downhill = downhillDirection.normalized;
            BestDistance = Mathf.Max(BestDistance, Distance);
            Distance = 0f;
            Score = 0;
            Checkpoints = 0;
            CoinsCollected = 0;
            airTime = 0f;
        }

        public void Tick(float deltaTime, Vector3 ballPosition, bool grounded)
        {
            Distance = Mathf.Max(0f, Vector3.Dot(ballPosition - startPosition, downhill));

            int reached = Mathf.FloorToInt(Distance / config.checkpointInterval);
            while (Checkpoints < reached)
            {
                Checkpoints++;
                Score += config.checkpointBonus;
                Debug.Log($"[Score] Checkpoint {Checkpoints} reached at {Distance:F0} m (+{config.checkpointBonus} pts, total {Score})");
            }

            if (!grounded && Distance > 1f)
            {
                airTime += deltaTime;
            }
            else if (grounded && airTime > 0f)
            {
                if (airTime > 0.35f)
                {
                    int bonus = Mathf.RoundToInt(airTime * config.airtimePointsPerSecond);
                    Score += bonus;
                    Debug.Log($"[Score] Airtime {airTime:F2}s (+{bonus} pts, total {Score})");
                }
                airTime = 0f;
            }
        }

        public void AddObstacleDestroy(int points, int tier, float obstacleRadius, float ballRadius)
        {
            Score += points;
            Debug.Log($"[Score] Destroyed tier {tier} obstacle (r {obstacleRadius:F2}) with ball r {ballRadius:F2}: +{points} pts (total {Score})");
        }

        public void AddCoin(int value)
        {
            CoinsCollected += value;
        }
    }
}
