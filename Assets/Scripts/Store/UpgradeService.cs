using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snowballin
{
    public enum UpgradeType
    {
        SizeGrowth,
        SteerControl,
        JumpHeight,
        LuckyObstacle,
        GoldMultiplier,
        ObstacleRate
    }

    public class UpgradeInfo
    {
        public UpgradeType Type;
        public string Name;
        public string Description;
    }

    /// <summary>
    /// Session-only wallet + stacking upgrade levels. Lives for the whole app run,
    /// so upgrades and gold survive death/retry but reset when the game restarts.
    /// </summary>
    public class UpgradeService
    {
        public const int MaxLevel = 20;

        public static readonly UpgradeInfo[] Catalog =
        {
            new UpgradeInfo { Type = UpgradeType.SizeGrowth, Name = "Size Growth", Description = "+15% ball growth rate" },
            new UpgradeInfo { Type = UpgradeType.SteerControl, Name = "Steer Control", Description = "+1.5 m/s steering speed" },
            new UpgradeInfo { Type = UpgradeType.JumpHeight, Name = "Jump Height", Description = "+0.3 m jump height" },
            new UpgradeInfo { Type = UpgradeType.LuckyObstacle, Name = "Lucky Obstacles", Description = "+5% chance a hard obstacle is breakable" },
            new UpgradeInfo { Type = UpgradeType.GoldMultiplier, Name = "Gold Multiplier", Description = "+25% gold from coins and passive income" },
            new UpgradeInfo { Type = UpgradeType.ObstacleRate, Name = "Obstacle Rate", Description = "-8% obstacle density growth" }
        };

        readonly GameConfig config;
        readonly Dictionary<UpgradeType, int> levels = new Dictionary<UpgradeType, int>();

        public int Gold { get; private set; }
        public event Action Changed;

        public UpgradeService(GameConfig config)
        {
            this.config = config;
            foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
            {
                levels[type] = 0;
            }
        }

        public int GetLevel(UpgradeType type)
        {
            return levels.TryGetValue(type, out int level) ? level : 0;
        }

        public bool IsMaxed(UpgradeType type)
        {
            return GetLevel(type) >= MaxLevel;
        }

        public int GetCost(UpgradeType type)
        {
            return Mathf.RoundToInt(config.baseUpgradeCost * Mathf.Pow(config.upgradeCostGrowth, GetLevel(type)));
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            Changed?.Invoke();
        }

        public bool TryPurchase(UpgradeType type)
        {
            if (IsMaxed(type))
            {
                Debug.LogWarning($"[Store] {type} is already at max level {MaxLevel}");
                return false;
            }

            int cost = GetCost(type);
            if (Gold < cost)
            {
                Debug.LogWarning($"[Store] Not enough gold for {type}: have {Gold}, need {cost}");
                return false;
            }

            Gold -= cost;
            levels[type] = GetLevel(type) + 1;
            Debug.Log($"[Store] Purchased {type} -> level {levels[type]} for {cost} gold (gold left {Gold})");
            Changed?.Invoke();
            return true;
        }

        public float LateralSpeed => config.baseLateralSpeed + GetLevel(UpgradeType.SteerControl) * config.steerPerLevel;
        public float JumpHeight => config.baseJumpHeight + GetLevel(UpgradeType.JumpHeight) * config.jumpHeightPerLevel;

        /// <summary>Starting radius is fixed; the size upgrade scales how fast the ball grows instead.</summary>
        public float StartRadius => config.baseRadius;

        /// <summary>Multiplier applied to the ball's per-second growth (1 = base rate).</summary>
        public float GrowthMultiplier => 1f + GetLevel(UpgradeType.SizeGrowth) * config.growthMultiplierPerLevel;

        /// <summary>Multiplier applied to coin pickups and passive gold (1 = base rate).</summary>
        public float GoldMultiplier => 1f + GetLevel(UpgradeType.GoldMultiplier) * config.goldMultiplierPerLevel;

        public float LuckyChance => GetLevel(UpgradeType.LuckyObstacle) * config.luckyChancePerLevel;
        public float ObstacleRateMultiplier => Mathf.Max(0.25f, 1f - GetLevel(UpgradeType.ObstacleRate) * config.obstacleRateReductionPerLevel);
        public float RampChance => config.rampChanceBase;
    }
}
