using UnityEngine;

// Live balance knobs for playtesting, edited with the sliders in the arena panel (I) and saved
// in PlayerPrefs. Once good values are found they can become the defaults below.
public static class Tuning {

    public class Knob {
        public readonly string Key;
        public readonly string Label;
        public readonly float Min, Max, Default;
        private float? cached;

        public Knob(string key, string label, float min, float max, float defaultValue) {
            Key = "freny.tuning." + key;
            Label = label;
            Min = min;
            Max = max;
            Default = defaultValue;
        }

        public float Value {
            get {
                if (cached == null) {
                    cached = PlayerPrefs.GetFloat(Key, Default);
                }
                return cached.Value;
            }
            set {
                cached = Mathf.Clamp(value, Min, Max);
                PlayerPrefs.SetFloat(Key, cached.Value);
            }
        }

        public void Reset() {
            PlayerPrefs.DeleteKey(Key);
            cached = null;
        }
    }

    // enemies
    public static readonly Knob EnemyCooldown = new Knob("enemyCooldown", "Inimigos: tempo entre ataques", 0.3f, 3f, 1f);
    public static readonly Knob EnemyWindup = new Knob("enemyWindup", "Inimigos: aviso antes do golpe", 0.3f, 3f, 1f);
    public static readonly Knob EnemySpeed = new Knob("enemySpeed", "Inimigos: velocidade", 0.3f, 2f, 1f);
    public static readonly Knob EnemyAggro = new Knob("enemyAggro", "Inimigos: raio de perseguição", 0.3f, 2f, 1f);

    // autopilot brain (multiplies the upgrade values from FrenyProgress)
    public static readonly Knob AutoReaction = new Knob("autoReaction", "Autopiloto: tempo de reação", 0.2f, 3f, 1f);
    public static readonly Knob AutoMeleeRange = new Knob("autoMelee", "Autopiloto: alcance da espada", 0.6f, 3f, 1.6f);
    public static readonly Knob AutoShootRange = new Knob("autoShoot", "Autopiloto: distância do tiro", 0f, 15f, 9f);
    public static readonly Knob AutoDodgeRange = new Knob("autoDodge", "Autopiloto: distância da esquiva", 0f, 8f, 3.5f);
    public static readonly Knob AutoKeepDistance = new Knob("autoKeep", "Autopiloto: recuar na recarga", 0f, 1f, 0.5f);

    public static readonly Knob[] All = {
        EnemyCooldown, EnemyWindup, EnemySpeed, EnemyAggro,
        AutoReaction, AutoMeleeRange, AutoShootRange, AutoDodgeRange, AutoKeepDistance,
    };

    public static void ResetAll() {
        foreach (Knob knob in All) {
            knob.Reset();
        }
    }
}
