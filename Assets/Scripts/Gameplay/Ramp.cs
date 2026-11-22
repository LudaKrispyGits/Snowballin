using UnityEngine;

namespace Snowballin
{
    /// <summary>A launch ramp that kicks the ball into the air (airtime = points).</summary>
    public class Ramp : MonoBehaviour
    {
        public float launchBoost = 5.5f;
        bool used;

        void OnCollisionEnter(Collision collision)
        {
            if (used) return;

            SnowballBody ball = collision.collider.GetComponentInParent<SnowballBody>();
            if (ball == null || ball.IsBroken) return;

            Rigidbody body = ball.GetComponent<Rigidbody>();
            if (body == null) return;

            used = true;
            Vector3 velocity = body.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, 0f) + launchBoost;
            body.linearVelocity = velocity;

            Debug.Log($"[Ramp] Launched the ball with +{launchBoost:F1} m/s vertical");
            Invoke(nameof(Rearm), 1.5f);
        }

        void Rearm()
        {
            used = false;
        }
    }
}
