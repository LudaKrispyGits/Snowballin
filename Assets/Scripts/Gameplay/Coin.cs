using UnityEngine;

namespace Snowballin
{
    /// <summary>Gold pickup. Adds to the session wallet and the run's coin count.</summary>
    [RequireComponent(typeof(SphereCollider))]
    public class Coin : MonoBehaviour
    {
        public int value = 1;
        bool collected;

        void Start()
        {
            SphereCollider trigger = GetComponent<SphereCollider>();
            if (trigger != null) trigger.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (collected) return;

            SnowballBody ball = other.GetComponentInParent<SnowballBody>();
            if (ball == null || ball.IsBroken) return;

            collected = true;
            GameManager gm = GameManager.Instance;

            int payout = value;
            if (gm != null)
            {
                payout = Mathf.Max(1, Mathf.RoundToInt(value * gm.Upgrades.GoldMultiplier));
                gm.Upgrades.AddGold(payout);
                gm.Score.AddCoin(value);
            }

            Destroy(gameObject);
        }

        void Update()
        {
            transform.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
        }
    }
}
