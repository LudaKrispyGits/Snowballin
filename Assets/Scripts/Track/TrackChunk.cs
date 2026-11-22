using UnityEngine;

namespace Snowballin
{
    /// <summary>
    /// One recycled piece of the endless slope. Root scale stays at 1 so spawned
    /// content can be positioned in metres; the visible ground is a scaled child.
    /// </summary>
    public class TrackChunk : MonoBehaviour
    {
        public float DistanceStart;
        public float Length = 20f;
        public Transform contentRoot;

        void Awake()
        {
            EnsureContentRoot();
        }

        void EnsureContentRoot()
        {
            if (contentRoot != null) return;

            Transform existing = transform.Find("Content");
            if (existing != null)
            {
                contentRoot = existing;
                return;
            }

            GameObject go = new GameObject("Content");
            go.transform.SetParent(transform, false);
            contentRoot = go.transform;
        }

        public void Place(Vector3 origin, Vector3 downhill, Vector3 slopeNormal, Quaternion rotation, float distanceStart, float length)
        {
            EnsureContentRoot();

            DistanceStart = distanceStart;
            Length = length;

            Vector3 surfacePoint = origin + downhill * (distanceStart + length * 0.5f);
            transform.position = surfacePoint - slopeNormal * 0.5f;
            transform.rotation = rotation;
        }

        public void ClearContent()
        {
            EnsureContentRoot();

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }
        }
    }
}
