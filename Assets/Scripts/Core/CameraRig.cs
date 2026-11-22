using UnityEngine;

namespace Snowballin
{
    /// <summary>
    /// Smooth follow camera. Cinemachine is not installed in this project, so the rig is hand-rolled.
    /// The rig pulls back as the snowball grows so the wider, faster late game stays readable.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public GameConfig config;
        public Transform target;
        public Vector3 offset = new Vector3(0f, 5f, -11f);
        public float followSharpness = 6f;

        /// <summary>How quickly the rig settles on a new zoom level (per second).</summary>
        public float zoomSharpness = 1.5f;

        Vector3 currentOffset;
        float zoom = 1f;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            UpdateZoom(true);
            if (target == null) return;

            transform.position = target.position + currentOffset;
            Vector3 look = target.position + Vector3.forward * (8f * zoom) + Vector3.up * 0.5f;
            transform.rotation = Quaternion.LookRotation(look - transform.position);
        }

        void LateUpdate()
        {
            if (target == null) return;

            UpdateZoom(false);

            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target.position + currentOffset, t);

            Vector3 look = target.position + Vector3.forward * (8f * zoom) + Vector3.up * 0.5f;
            Vector3 dir = look - transform.position;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), t);
            }
        }

        void UpdateZoom(bool immediate)
        {
            float radius = 0.5f;
            float minRadius = 0.5f;
            float maxRadius = 3f;
            float zoomOut = 0.9f;

            if (config != null)
            {
                minRadius = config.baseRadius;
                maxRadius = config.maxRadius;
                zoomOut = config.cameraZoomOut;
            }

            if (GameManager.Instance != null && GameManager.Instance.Ball != null)
            {
                radius = GameManager.Instance.Ball.Radius;
            }

            float span = Mathf.Max(0.01f, maxRadius - minRadius);
            float growth = Mathf.Clamp01((radius - minRadius) / span);
            float targetZoom = 1f + growth * zoomOut;

            zoom = immediate ? targetZoom : Mathf.Lerp(zoom, targetZoom, 1f - Mathf.Exp(-zoomSharpness * Time.deltaTime));
            currentOffset = offset * zoom;
        }
    }
}
