using UnityEngine;

namespace Snowballin
{
    /// <summary>
    /// Procedural walk cycle for the level 3 NPC obstacle: swinging legs plus a body bob and sway.
    /// No imported animation clips are involved, so the NPC reads as walking out of the box.
    /// </summary>
    public class NpcWalkAnim : MonoBehaviour
    {
        public Transform body;
        public Transform legLeft;
        public Transform legRight;

        public float swingDegrees = 26f;
        public float swingSpeed = 4.5f;
        public float bobHeight = 0.07f;
        public float swayDegrees = 4f;

        Vector3 bodyBasePosition;
        Quaternion leftBaseRotation;
        Quaternion rightBaseRotation;
        float phase;

        void Awake()
        {
            if (body == null) body = transform;

            bodyBasePosition = body.localPosition;
            if (legLeft != null) leftBaseRotation = legLeft.localRotation;
            if (legRight != null) rightBaseRotation = legRight.localRotation;

            // Stagger the phase so nearby NPCs do not march in lockstep.
            phase = Random.value * Mathf.PI * 2f;
        }

        void Update()
        {
            phase += Time.deltaTime * swingSpeed;
            float swing = Mathf.Sin(phase) * swingDegrees;

            if (legLeft != null) legLeft.localRotation = leftBaseRotation * Quaternion.Euler(swing, 0f, 0f);
            if (legRight != null) legRight.localRotation = rightBaseRotation * Quaternion.Euler(-swing, 0f, 0f);

            body.localPosition = bodyBasePosition + Vector3.up * (Mathf.Abs(Mathf.Sin(phase)) * bobHeight);
            body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 0.5f) * swayDegrees);
        }
    }
}
