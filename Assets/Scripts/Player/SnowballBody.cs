using System.Collections;
using UnityEngine;

namespace Snowballin
{
    /// <summary>
    /// The snowball itself: radius growth, collider sync and the shatter/break state.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class SnowballBody : MonoBehaviour
    {
        public GameConfig config;

        SphereCollider sphereCollider;
        MeshRenderer meshRenderer;
        Rigidbody body;

        public float Radius { get; private set; } = 0.5f;
        public bool IsBroken { get; private set; }

        void Awake()
        {
            sphereCollider = GetComponent<SphereCollider>();
            meshRenderer = GetComponent<MeshRenderer>();
            body = GetComponent<Rigidbody>();
        }

        public void Initialize(Vector3 position, float radius)
        {
            IsBroken = false;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            SetRadius(radius);

            if (sphereCollider != null) sphereCollider.enabled = true;
            if (meshRenderer != null) meshRenderer.enabled = true;

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            Debug.Log($"[Snowball] Spawned at {position} with radius {radius:F2} m");
        }

        public void Grow(float deltaTime)
        {
            if (IsBroken) return;

            // The store's size upgrade multiplies the growth rate instead of the starting radius.
            float multiplier = GameManager.Instance != null ? GameManager.Instance.Upgrades.GrowthMultiplier : 1f;
            SetRadius(Mathf.Min(config.maxRadius, Radius + config.growthPerSecond * multiplier * deltaTime));
        }

        /// <summary>Instant size gain (ploughing a snow mound). Clamped to the config's max radius.</summary>
        public void AddRadius(float amount)
        {
            if (IsBroken || amount <= 0f) return;
            SetRadius(Mathf.Min(config.maxRadius, Radius + amount));
        }

        public void SetRadius(float radius)
        {
            Radius = radius;
            // The sphere mesh is 1 m in diameter, so local scale 2r gives radius r,
            // and the collider radius is scaled by the same transform.
            transform.localScale = Vector3.one * (radius * 2f);
            if (sphereCollider != null) sphereCollider.radius = 0.5f;
        }

        public void Break(string cause)
        {
            if (IsBroken) return;
            IsBroken = true;

            if (sphereCollider != null) sphereCollider.enabled = false;
            if (body != null) body.linearVelocity = Vector3.zero;

            Debug.Log($"[Snowball] BROKEN: {cause} at radius {Radius:F2} m");
            if (GameManager.Instance != null) GameManager.Instance.GameOver(cause);

            StartCoroutine(ShrinkRoutine(Radius));
        }

        IEnumerator ShrinkRoutine(float startRadius)
        {
            float t = 0f;
            const float duration = 0.35f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                SetRadius(Mathf.Lerp(startRadius, 0.02f, t / duration));
                yield return null;
            }

            if (meshRenderer != null) meshRenderer.enabled = false;
        }
    }
}
