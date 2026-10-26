using System.Collections.Generic;
using UnityEngine;

// Plays Freny in the arena. It only produces a PlayerInputFrame, the same thing the keyboard
// produces, so it moves, jumps and attacks with the player's physics and cooldowns. It re-thinks
// every FrenyProgress.AutopilotReactionTime seconds, which the brain upgrades shorten.
//
// Priorities (GDD, "Arena idle"):
// 1. dodge an incoming projectile  2. enemy in sword range -> sword  3. enemy aligned and far -> shot
// 4. low health and a potion -> drink it  5. otherwise walk to the target
[RequireComponent(typeof(Player))]
public class ArenaAutopilot : MonoBehaviour, IPlayerInputSource {

    // sword, gun and dodge distances come from the tuning sliders (Tuning.Auto*), so they can be tried live
    [SerializeField] private float verticalTolerance = 1.2f;   // "same height" for sword and gun
    [SerializeField] private float jumpToReachHeight = 1.5f;   // target this much higher -> jump
    [SerializeField] private int lowHealth = 1;
    [SerializeField] private float scanInterval = 0.25f;

    private Player player;
    private Dash dash;
    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly List<HexballEffect> projectiles = new List<HexballEffect>();
    private float nextScanTime;
    private float nextDecisionTime;
    private PlayerInputFrame plan;
    private bool planIsFresh;

    public string CurrentAction { get; private set; } = "";

    private void Awake() {
        player = GetComponent<Player>();
        dash = GetComponent<Dash>();
    }

    public PlayerInputFrame ReadInput() {
        if (player == null || player.IsDead) {
            CurrentAction = "";
            return default;
        }

        if (Time.time >= nextScanTime) {
            Scan();
            nextScanTime = Time.time + scanInterval;
        }

        if (Time.time >= nextDecisionTime) {
            plan = Decide();
            planIsFresh = true;
            nextDecisionTime = Time.time + FrenyProgress.AutopilotReactionTime * Tuning.AutoReaction.Value;
        }

        PlayerInputFrame frame = plan;
        if (!planIsFresh) {
            // buttons are pressed once per decision, like a person tapping; movement and held jump continue
            frame.jumpPressed = false;
            frame.meleePressed = false;
            frame.rangedPressed = false;
            frame.dashPressed = false;
            frame.potionPressed = false;
        }
        planIsFresh = false;
        return frame;
    }

    private void Scan() {
        enemies.Clear();
        foreach (Enemy enemy in Enemy.Active) {
            if (enemy && !enemy.IsDead) {
                enemies.Add(enemy);
            }
        }

        projectiles.Clear();
        projectiles.AddRange(HexballEffect.Active);
    }

    private PlayerInputFrame Decide() {
        PlayerInputFrame frame = new PlayerInputFrame();
        Vector2 me = transform.position;

        // 1. dodge
        HexballEffect threat = IncomingProjectile(me);
        if (threat) {
            float awayFromThreat = Mathf.Sign(me.x - threat.transform.position.x);
            bool facingAway = (awayFromThreat > 0) == player.FacingRight;
            if (FrenyProgress.HasDashDodge && facingAway && dash && !dash.IsDashing) {
                frame.dashPressed = true;
                CurrentAction = "Esquivando com dash";
            } else if (player.IsGrounded) {
                frame.jumpPressed = true;
                frame.jumpHeld = true;
                CurrentAction = "Pulando o projétil";
            } else if (FrenyProgress.HasDashDodge) {
                // in the air: turn away now, dash on the next decision
                frame.move = awayFromThreat;
                frame.jumpHeld = true;
                CurrentAction = "Virando para esquivar";
            }
            if (!frame.IsEmpty) {
                return frame;
            }
        }

        Enemy target = PickTarget(me);
        if (target == null) {
            CurrentAction = "Esperando inimigos";
            return frame;
        }

        Vector2 targetPos = target.transform.position;
        float dx = targetPos.x - me.x;
        float dy = targetPos.y - me.y;
        float towardTarget = Mathf.Sign(dx);
        bool facingTarget = (dx > 0) == player.FacingRight;
        bool sameHeight = Mathf.Abs(dy) <= verticalTolerance;

        // 2. sword
        if (sameHeight && Mathf.Abs(dx) <= Tuning.AutoMeleeRange.Value) {
            if (!player.AttackReady && Random.value < Tuning.AutoKeepDistance.Value) {
                // step back while the sword recharges instead of trading hits
                frame.move = -towardTarget;
                CurrentAction = "Recuando na recarga";
            } else if (!facingTarget) {
                frame.move = towardTarget; // a short step turns Freny around
                CurrentAction = "Virando para o inimigo";
            } else if (player.IsGrounded && player.AttackReady) {
                frame.meleePressed = true;
                CurrentAction = "Golpe de espada";
            } else {
                CurrentAction = "Esperando a recarga";
            }
            return frame;
        }

        // 3. gun
        if (sameHeight && Mathf.Abs(dx) <= Tuning.AutoShootRange.Value && player.AttackReady) {
            if (!facingTarget) {
                frame.move = towardTarget;
                CurrentAction = "Mirando";
            } else {
                frame.rangedPressed = true;
                CurrentAction = "Tiro";
            }
            return frame;
        }

        // 4. potion
        if (FrenyProgress.HasAutoPotion && player.Health <= lowHealth && player.HasPotion) {
            frame.potionPressed = true;
            CurrentAction = "Bebendo poção";
            return frame;
        }

        // 5. walk to the target, jumping up to it when it is above
        frame.move = towardTarget;
        if (dy > jumpToReachHeight && player.IsGrounded) {
            frame.jumpPressed = true;
            frame.jumpHeld = true;
        } else if (dy > jumpToReachHeight) {
            frame.jumpHeld = true;
        }
        CurrentAction = "Indo até o inimigo";
        return frame;
    }

    private HexballEffect IncomingProjectile(Vector2 me) {
        foreach (HexballEffect projectile in projectiles) {
            if (!projectile) {
                continue;
            }
            Vector2 pos = projectile.transform.position;
            float dx = me.x - pos.x;
            bool headingAtMe = Mathf.Sign(dx) == Mathf.Sign(projectile.Direction.x);
            if (headingAtMe && Mathf.Abs(dx) <= Tuning.AutoDodgeRange.Value && Mathf.Abs(me.y - pos.y) <= verticalTolerance) {
                return projectile;
            }
        }
        return null;
    }

    // untrained: the nearest enemy. Smart targeting also weighs height (enemies on another
    // platform are expensive to reach) and prefers enemies that are almost dead.
    private Enemy PickTarget(Vector2 me) {
        Enemy best = null;
        float bestScore = float.MaxValue;

        foreach (Enemy enemy in enemies) {
            if (!enemy || enemy.IsDead) {
                continue;
            }
            Vector2 offset = (Vector2)enemy.transform.position - me;
            float score = offset.magnitude;

            if (FrenyProgress.HasSmartTarget) {
                score = Mathf.Abs(offset.x) + 3f * Mathf.Abs(offset.y);
                if (enemy is IDamageable damageable) {
                    score += 1.5f * damageable.Health;
                }
            }

            if (score < bestScore) {
                bestScore = score;
                best = enemy;
            }
        }
        return best;
    }
}
