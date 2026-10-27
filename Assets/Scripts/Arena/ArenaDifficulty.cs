using UnityEngine;

public enum ArenaLevel { Easy, Medium, Hard, Freny }

// The difficulty picked before an arena run (the menu sets Current, then loads the arena).
// ArenaDirector reads the numbers: how fast the run ramps up, how many enemies at the peak,
// how far into the run it starts, and how late the strong enemies (Golem, sorcerers) unlock.
public static class ArenaDifficulty {

    private const string Key = "freny.arena.difficulty";

    public static ArenaLevel Current {
        get => (ArenaLevel)Mathf.Clamp(PlayerPrefs.GetInt(Key, (int)ArenaLevel.Medium), 0, 3);
        set {
            PlayerPrefs.SetInt(Key, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static string Label(ArenaLevel level) {
        switch (level) {
            case ArenaLevel.Easy: return "Fácil";
            case ArenaLevel.Medium: return "Médio";
            case ArenaLevel.Hard: return "Difícil";
            default: return "Freny";
        }
    }

    // Medium is what José played and found "normal"; Hard and Freny add enemies and speed, never
    // health (one-hit enemies stay one-hit, the fights keep their pace)
    private static float Pick(float easy, float medium, float hard, float freny) {
        switch (Current) {
            case ArenaLevel.Easy: return easy;
            case ArenaLevel.Hard: return hard;
            case ArenaLevel.Freny: return freny;
            default: return medium;
        }
    }

    // seconds-to-peak multiplier: Easy ramps up slower
    public static float RampFactor => Pick(1.6f, 1f, 0.7f, 0.5f);

    // enemies alive at the peak
    public static float PeakFactor => Pick(0.6f, 1f, 1.5f, 2f);

    // time between spawns
    public static float SpawnIntervalFactor => Pick(1.25f, 1f, 0.8f, 0.6f);

    // where the run starts on the 0..1 difficulty curve
    public static float StartAt => Pick(0f, 0f, 0.15f, 0.3f);

    // added to every enemy's unlock point: on Easy the strong ones come much later
    public static float UnlockShift => Pick(0.15f, 0f, -0.1f, -0.2f);

    // enemy movement speed: the small one-hit enemies (Bug, Bat) speed up the most
    public static float SpeedFactor(bool small) => small ? Pick(0.85f, 1f, 1.25f, 1.45f) : Pick(0.9f, 1f, 1.1f, 1.25f);

    // time between enemy attacks and casts
    public static float CooldownFactor => Pick(1.25f, 1f, 0.9f, 0.75f);
}
