using UnityEngine;
using UnityEngine.UI;

namespace Snowballin
{
    /// <summary>Top-left run readout: distance, score, gold and current radius.</summary>
    public class HUD : MonoBehaviour
    {
        Text distanceText;
        Text scoreText;
        Text goldText;
        Text radiusText;

        void Awake()
        {
            distanceText = UIFactory.CreateLabel(transform, "Distance", 40, Color.white, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(600f, 52f));

            scoreText = UIFactory.CreateLabel(transform, "Score", 30, new Color(0.85f, 0.93f, 1f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -76f), new Vector2(600f, 44f));

            goldText = UIFactory.CreateLabel(transform, "Gold", 30, new Color(1f, 0.82f, 0.25f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -118f), new Vector2(600f, 44f));

            radiusText = UIFactory.CreateLabel(transform, "Radius", 30, new Color(0.85f, 0.93f, 1f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -160f), new Vector2(600f, 44f));

            UIFactory.CreateLabel(transform, "Hint", 24, new Color(1f, 1f, 1f, 0.75f), TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1200f, 40f)).text =
                "A / D steer     SPACE jump     P store";
        }

        void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            distanceText.text = $"{gm.Score.Distance:F0} m";
            scoreText.text = $"Score  {gm.Score.Score}";
            goldText.text = $"Gold  {gm.Upgrades.Gold}";
            radiusText.text = gm.Ball != null ? $"Size  {gm.Ball.Radius:F2} m" : "Size  -";
        }

        public void OnStateChanged(GameState state)
        {
            // HUD stays visible through every state; nothing to toggle yet.
        }
    }
}
