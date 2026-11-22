using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Snowballin
{
    /// <summary>
    /// The pause store. Opened with P, lists every stacking upgrade (no Move Speed — forward
    /// speed now ramps with distance on its own) with level, cost and a buy button that spends gold immediately.
    /// </summary>
    public class StoreUI : MonoBehaviour
    {
        class Row
        {
            public UpgradeInfo Info;
            public Text Level;
            public Text Cost;
            public Button Buy;
        }

        Image backdrop;
        Text goldText;
        readonly List<Row> rows = new List<Row>();

        void Awake()
        {
            Build();
            SetVisible(false);
        }

        void Build()
        {
            backdrop = UIFactory.CreateFullScreenPanel(transform, "Backdrop", new Color(0.02f, 0.04f, 0.08f, 0.9f));

            Image card = UIFactory.CreatePanel(backdrop.transform, "Card", new Color(0.09f, 0.15f, 0.24f, 0.98f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 780f));

            UIFactory.CreateLabel(card.transform, "Title", 44, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(1100f, 60f)).text =
                "STORE   (P to resume)";

            goldText = UIFactory.CreateLabel(card.transform, "Gold", 34, new Color(1f, 0.82f, 0.25f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(1100f, 50f));

            UIFactory.CreateLabel(card.transform, "Blurb", 22, new Color(0.75f, 0.85f, 1f, 0.9f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(1100f, 34f)).text =
                "Buffs are permanent for this session and stack with every purchase.";

            for (int i = 0; i < UpgradeService.Catalog.Length; i++)
            {
                UpgradeInfo info = UpgradeService.Catalog[i];
                float y = -206f - i * 74f;

                UIFactory.CreateLabel(card.transform, $"Name{i}", 26, Color.white, TextAnchor.MiddleLeft,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-430f, y), new Vector2(400f, 56f)).text = info.Name;

                Text level = UIFactory.CreateLabel(card.transform, $"Level{i}", 24, new Color(0.8f, 0.9f, 1f), TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-90f, y), new Vector2(220f, 56f));

                Text cost = UIFactory.CreateLabel(card.transform, $"Cost{i}", 24, new Color(1f, 0.82f, 0.25f), TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(120f, y), new Vector2(180f, 56f));

                UpgradeType captured = info.Type;
                Button buy = UIFactory.CreateButton(card.transform, $"Buy{i}", "BUY", 24,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(400f, y), new Vector2(170f, 56f),
                    new Color(0.62f, 0.86f, 1f), () => Purchase(captured));

                rows.Add(new Row { Info = info, Level = level, Cost = cost, Buy = buy });
            }

            UIFactory.CreateButton(card.transform, "Resume", "RESUME", 26,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(240f, 56f),
                new Color(0.75f, 0.9f, 1f), () => GameManager.Instance.Resume());
        }

        void Purchase(UpgradeType type)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.Upgrades.TryPurchase(type))
            {
                Refresh();
            }
            else
            {
                Refresh();
            }
        }

        public void OnStateChanged(GameState state)
        {
            bool open = state == GameState.Paused;
            SetVisible(open);
            if (open) Refresh();
        }

        void SetVisible(bool visible)
        {
            if (backdrop != null) backdrop.gameObject.SetActive(visible);
        }

        void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            goldText.text = $"GOLD  {gm.Upgrades.Gold}";

            foreach (Row row in rows)
            {
                int level = gm.Upgrades.GetLevel(row.Info.Type);
                bool maxed = gm.Upgrades.IsMaxed(row.Info.Type);
                int cost = gm.Upgrades.GetCost(row.Info.Type);

                row.Level.text = $"Lv {level}/{UpgradeService.MaxLevel}";
                row.Cost.text = maxed ? "MAX" : $"{cost} g";
                row.Buy.interactable = !maxed && gm.Upgrades.Gold >= cost;
            }
        }
    }
}
