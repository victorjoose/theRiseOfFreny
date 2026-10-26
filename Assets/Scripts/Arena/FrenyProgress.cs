using System;
using UnityEngine;

public enum FrenyUpgrade {
    // Freny's attributes: bought with coins, valid everywhere (arena, by hand, campaign)
    MaxHealth,
    MoveSpeed,
    AttackSpeed,
    // the autopilot's brain: bought with points from levelling up, arena only
    Reaction,
    DashDodge,
    AutoPotion,
    SmartTarget,
}

// XP, levels and the two upgrade tracks from the GDD ("Arena idle"), saved with PlayerPrefs.
public static class FrenyProgress {

    public static event Action Changed;

    private const string XpKey = "freny.xp";
    private const string SpentPointsKey = "freny.spentPoints";
    private const string LevelKeyPrefix = "freny.upgrade.";

    // ---- XP and levels ----

    public static int Xp => PlayerPrefs.GetInt(XpKey, 0);

    // level 1 at 0 XP; level n -> n + 1 costs 100 * n XP
    public static int Level {
        get {
            int level = 1;
            int remaining = Xp;
            while (remaining >= XpToNext(level)) {
                remaining -= XpToNext(level);
                level++;
            }
            return level;
        }
    }

    public static int XpIntoLevel {
        get {
            int remaining = Xp;
            for (int level = 1; remaining >= XpToNext(level); level++) {
                remaining -= XpToNext(level);
            }
            return remaining;
        }
    }

    public static int XpToNext(int level) {
        return 100 * level;
    }

    // one brain point per level gained
    public static int BrainPoints => Mathf.Max(0, Level - 1 - PlayerPrefs.GetInt(SpentPointsKey, 0));

    public static void AddXp(int amount) {
        if (amount <= 0) {
            return;
        }
        PlayerPrefs.SetInt(XpKey, Xp + amount);
        Changed?.Invoke();
    }

    // ---- upgrades ----

    public static bool IsBrain(FrenyUpgrade upgrade) {
        return upgrade >= FrenyUpgrade.Reaction;
    }

    public static int GetLevel(FrenyUpgrade upgrade) {
        return PlayerPrefs.GetInt(LevelKeyPrefix + upgrade, 0);
    }

    public static int MaxLevel(FrenyUpgrade upgrade) {
        switch (upgrade) {
            case FrenyUpgrade.MaxHealth: return 4;
            case FrenyUpgrade.MoveSpeed: return 5;
            case FrenyUpgrade.AttackSpeed: return 5;
            case FrenyUpgrade.Reaction: return 5;
            default: return 1;
        }
    }

    // coins for attributes, brain points for the autopilot
    public static int Cost(FrenyUpgrade upgrade) {
        if (IsBrain(upgrade)) {
            return 1;
        }
        return 100 + 50 * GetLevel(upgrade);
    }

    public static string DisplayName(FrenyUpgrade upgrade) {
        switch (upgrade) {
            case FrenyUpgrade.MaxHealth: return "Vida máxima +1";
            case FrenyUpgrade.MoveSpeed: return "Velocidade +8%";
            case FrenyUpgrade.AttackSpeed: return "Recarga -10%";
            case FrenyUpgrade.Reaction: return "Reação mais rápida";
            case FrenyUpgrade.DashDodge: return "Esquiva com dash";
            case FrenyUpgrade.AutoPotion: return "Usar poção sozinho";
            case FrenyUpgrade.SmartTarget: return "Escolher alvo melhor";
            default: return upgrade.ToString();
        }
    }

    public static bool IsMaxed(FrenyUpgrade upgrade) {
        return GetLevel(upgrade) >= MaxLevel(upgrade);
    }

    public static bool CanBuy(FrenyUpgrade upgrade, Player player) {
        if (IsMaxed(upgrade)) {
            return false;
        }
        if (IsBrain(upgrade)) {
            return BrainPoints >= Cost(upgrade);
        }
        return player != null && player.coins >= Cost(upgrade);
    }

    public static bool TryBuy(FrenyUpgrade upgrade, Player player) {
        if (!CanBuy(upgrade, player)) {
            return false;
        }

        if (IsBrain(upgrade)) {
            PlayerPrefs.SetInt(SpentPointsKey, PlayerPrefs.GetInt(SpentPointsKey, 0) + Cost(upgrade));
        } else {
            player.AddCoins(-Cost(upgrade));
        }

        PlayerPrefs.SetInt(LevelKeyPrefix + upgrade, GetLevel(upgrade) + 1);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }

    // ---- effects ----

    public static int BonusMaxHealth => GetLevel(FrenyUpgrade.MaxHealth);
    public static float SpeedMultiplier => 1f + 0.08f * GetLevel(FrenyUpgrade.MoveSpeed);
    public static float AttackCooldownMultiplier => 1f - 0.1f * GetLevel(FrenyUpgrade.AttackSpeed);

    // seconds between autopilot decisions: 0.6 s untrained, 0.1 s maxed
    public static float AutopilotReactionTime => 0.6f - 0.1f * GetLevel(FrenyUpgrade.Reaction);
    public static bool HasDashDodge => GetLevel(FrenyUpgrade.DashDodge) > 0;
    public static bool HasAutoPotion => GetLevel(FrenyUpgrade.AutoPotion) > 0;
    public static bool HasSmartTarget => GetLevel(FrenyUpgrade.SmartTarget) > 0;

    // for testing in the Editor
    public static void ResetAll() {
        PlayerPrefs.DeleteKey(XpKey);
        PlayerPrefs.DeleteKey(SpentPointsKey);
        foreach (FrenyUpgrade upgrade in Enum.GetValues(typeof(FrenyUpgrade))) {
            PlayerPrefs.DeleteKey(LevelKeyPrefix + upgrade);
        }
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
