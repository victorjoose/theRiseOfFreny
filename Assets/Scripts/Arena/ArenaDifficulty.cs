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

    // seconds-to-peak multiplier: Easy ramps up slower
    public static float RampFactor {
        get {
            switch (Current) {
                case ArenaLevel.Easy: return 1.6f;
                case ArenaLevel.Hard: return 0.7f;
                case ArenaLevel.Freny: return 0.45f;
                default: return 1f;
            }
        }
    }

    // enemies alive at the peak
    public static float PeakFactor {
        get {
            switch (Current) {
                case ArenaLevel.Easy: return 0.6f;
                case ArenaLevel.Hard: return 1.3f;
                case ArenaLevel.Freny: return 1.6f;
                default: return 1f;
            }
        }
    }

    // where the run starts on the 0..1 difficulty curve
    public static float StartAt {
        get {
            switch (Current) {
                case ArenaLevel.Hard: return 0.15f;
                case ArenaLevel.Freny: return 0.35f;
                default: return 0f;
            }
        }
    }

    // added to every enemy's unlock point: on Easy the strong ones come much later
    public static float UnlockShift {
        get {
            switch (Current) {
                case ArenaLevel.Easy: return 0.15f;
                case ArenaLevel.Hard: return -0.1f;
                case ArenaLevel.Freny: return -0.2f;
                default: return 0f;
            }
        }
    }
}
