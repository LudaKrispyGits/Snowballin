using UnityEngine;
using UnityEngine.UI;

namespace Snowballin
{
    /// <summary>Death screen: run summary plus retry, keeping the session upgrades.</summary>
    public class RetryScreen : MonoBehaviour
    {
        Image backdrop;
        Text summary;
        Text upgradesNote;

        void Awake()
        {
            Build();
            SetVisible(false);
        }

        void Build()
        {
            backdrop = UIFactory.CreateFullScreenPanel(transform, "Backdrop", new Color(0.02f, 0.04f, 0.08f, 0.9f));

            Image card = UIFactory.CreatePanel(backdrop.transform, "Card", new Color(0.1f, 0.13f, 0.2f, 0.98f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 520f));

            UIFactory.CreateLabel(card.transform, "Title", 52, new Color(0.95f, 0.97f, 1f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(760f, 70f)).text =
                "ROUND OVER";

            summary = UIFactory.CreateLabel(card.transform, "Summary", 32, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(760f, 130f));

            upgradesNote = UIFactory.CreateLabel(card.transform, "Upgrades", 24, new Color(0.75f, 0.87f, 1f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(760f, 90f));

            UIFactory.CreateButton(card.transform, "Retry", "RETRY", 32,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(320f, 72f),
                new Color(0.62f, 0.86f, 1f), () => GameManager.Instance.Retry());
        }

        public void OnStateChanged(GameState state)
        {
            bool open = state == GameState.Dead;
            SetVisible(open);
            if (open) Refresh();
        }

        void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            summary.text = $"Distance   {gm.Score.Distance:F0} m\nScore   {gm.Score.Score}\nGold   {gm.Upgrades.Gold}";

            int purchased = 0;
            foreach (UpgradeInfo info in UpgradeService.Catalog)
            {
                purchased += gm.Upgrades.GetLevel(info.Type);
            }
            upgradesNote.text = $"Upgrades kept: {purchased} levels across {UpgradeService.Catalog.Length} buffs";
        }

        void SetVisible(bool visible)
        {
            if (backdrop != null) backdrop.gameObject.SetActive(visible);
        }
    }
}
