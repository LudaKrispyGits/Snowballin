using UnityEngine;

namespace Snowballin
{
    /// <summary>
    /// A themed obstacle level. The ball's radius is compared against the obstacle's radius:
    /// clearly bigger  -> ploughed through: shattered for points, no slowdown (snow mounds also feed the ball size)
    /// about the same  -> the ball is slowed for a moment and gains no size
    /// clearly smaller -> the ball breaks and the round ends
    /// Every obstacle collider is a trigger, so a plough-through never produces a physics bounce.
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        public GameConfig config;

        MaterialPropertyBlock propertyBlock;
        bool resolved;

        public ObstacleLevelDef Def { get; private set; }
        public int Level => Def != null ? Def.level : 1;
        public float Radius => Def != null ? Def.radius : 0.5f;
        public bool Lucky { get; private set; }
        public string Label => Def != null ? Def.label : "Obstacle";

        public void Initialize(ObstacleLevelDef def, bool lucky)
        {
            Def = def;
            Lucky = lucky;
            resolved = false;

            if (def == null)
            {
                Debug.LogError("[Obstacle] Initialize called without an obstacle level definition.");
                return;
            }

            // Never let a destroyed obstacle push the ball around: contact is pure gameplay, not physics.
            Collider obstacleCollider = GetComponent<Collider>();
            if (obstacleCollider == null) obstacleCollider = GetComponentInChildren<Collider>();
            if (obstacleCollider != null) obstacleCollider.isTrigger = true;

            if (!lucky) return;

            // Lucky = breakable no matter how big the ball is, so it reads in gold.
            propertyBlock ??= new MaterialPropertyBlock();
            foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
            {
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", new Color(1f, 0.82f, 0.25f));
                propertyBlock.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.1f));
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            Resolve(collision.collider.GetComponentInParent<SnowballBody>());
        }

        void OnTriggerEnter(Collider other)
        {
            Resolve(other.GetComponentInParent<SnowballBody>());
        }

        void Resolve(SnowballBody ball)
        {
            if (resolved) return;
            if (Def == null || ball == null || ball.IsBroken) return;

            resolved = true;

            GameManager gm = GameManager.Instance;
            float band = Mathf.Clamp01(config != null ? config.sameSizeBand : 0.1f);
            float ratio = ball.Radius / Mathf.Max(0.01f, Def.radius);
            float distance = gm != null ? gm.Score.Distance : 0f;

            if (Lucky || ratio > 1f + band)
            {
                Plough(ball, gm, distance);
            }
            else if (ratio >= 1f - band)
            {
                SameSize(ball, gm, distance);
            }
            else
            {
                float needed = Def.radius * (1f - band);
                ball.Break($"hit {Def.label} (level {Def.level}, needed ball radius {needed:F2} m, had {ball.Radius:F2} m)");
            }
        }

        void Plough(SnowballBody ball, GameManager gm, float distance)
        {
            gm?.Score.AddObstacleDestroy(Def.points, Def.level, Def.radius, ball.Radius);

            if (Def.grantsGrowth && Def.growthPerHit > 0f)
            {
                float before = ball.Radius;
                ball.AddRadius(Def.growthPerHit);
                Debug.Log($"[Obstacle] {Def.label} L{Def.level} ploughed through at {distance:F0} m: " +
                          $"radius {before:F2} -> {ball.Radius:F2} m (+{Def.growthPerHit:F2})");
            }
            else
            {
                Debug.Log($"[Obstacle] {Def.label} L{Def.level} shattered, no slow (ball r {ball.Radius:F2} m) at {distance:F0} m");
            }

            Destroy(gameObject);
        }

        void SameSize(SnowballBody ball, GameManager gm, float distance)
        {
            float factor = config != null ? config.slowSpeedFactor : 0.5f;
            float duration = config != null ? config.slowDuration : 0.6f;

            gm?.Score.AddObstacleDestroy(Mathf.RoundToInt(Def.points * 0.5f), Def.level, Def.radius, ball.Radius);
            gm?.ApplySlow(factor, duration);

            Debug.Log($"[Obstacle] {Def.label} L{Def.level} is the same size as the ball " +
                      $"(r {Def.radius:F2} vs {ball.Radius:F2} m) at {distance:F0} m: slowed, no growth");

            Destroy(gameObject);
        }
    }
}
