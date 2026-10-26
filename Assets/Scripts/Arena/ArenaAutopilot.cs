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
    [SerializeField] private float dodgeWindow = 0.55f;        // dodge only when the projectile lands within this many seconds
    [SerializeField] private float dashCloseDistance = 3.5f;   // farther than this (same height) -> dash in
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

        // 1. dodge, only when a projectile is about to land (before, any hexball nearby kept her
        // hopping forever instead of fighting)
        HexballEffect threat = IncomingProjectile(me);
        if (threat) {
            float awayFromThreat = Mathf.Sign(me.x - threat.transform.position.x);
            bool facingAway = (awayFromThreat > 0) == player.FacingRight;
            if (FrenyProgress.HasDashDodge && facingAway && dash && dash.CanDash) {
                frame.dashPressed = true;
                CurrentAction = "Esquivando com dash";
            } else if (player.IsGrounded || player.CanAirJump) {
                frame.jumpPressed = true;
                frame.jumpHeld = true;
                CurrentAction = "Pulando o projétil";
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
        float meleeRange = Tuning.AutoMeleeRange.Value * (FrenyProgress.HasGreatSword ? 1.4f : 1f);
        if (sameHeight && Mathf.Abs(dx) <= meleeRange) {
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

        // 4b. far away on the same floor: dash in (a person would)
        if (sameHeight && facingTarget && Mathf.Abs(dx) > dashCloseDistance && player.IsGrounded && dash && dash.CanDash) {
            frame.dashPressed = true;
            frame.move = towardTarget;
            CurrentAction = "Dash até o inimigo";
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

    // a hexball that will pass within a body's width of Freny in the next dodgeWindow seconds
    private HexballEffect IncomingProjectile(Vector2 me) {
        Vector2 body = me + Vector2.up * 0.5f;
        foreach (HexballEffect projectile in projectiles) {
            if (!projectile) {
                continue;
            }
            Vector2 pos = projectile.transform.position;
            Vector2 dir = ((Vector2)projectile.Direction).normalized;
            Vector2 toMe = body - pos;
            float along = Vector2.Dot(toMe, dir);           // distance still to travel towards her
            if (along < 0f || along > Tuning.AutoDodgeRange.Value) {
                continue;                                    // moving away, or still far
            }
            float miss = Mathf.Abs(toMe.x * dir.y - toMe.y * dir.x); // closest approach to her body
            float timeToHit = along / Mathf.Max(0.1f, projectile.Speed);
            if (miss <= 0.8f && timeToHit <= dodgeWindow) {
                return projectile;
            }
        }
        return null;
    }

    // enemies on her own floor come first (another platform is expensive to reach). Smart
    // targeting also prefers enemies that are almost dead and ignores the ones she can't reach.
    private Enemy PickTarget(Vector2 me) {
        Enemy best = null;
        float bestScore = float.MaxValue;

        foreach (Enemy enemy in enemies) {
            if (!enemy || enemy.IsDead) {
                continue;
            }
            Vector2 offset = (Vector2)enemy.transform.position - me;
            float score = Mathf.Abs(offset.x) + 4f * Mathf.Abs(offset.y);

            if (FrenyProgress.HasSmartTarget) {
                if (enemy is IDamageable damageable) {
                    score += 1.5f * damageable.Health;
                }
                if (offset.y > jumpToReachHeight * 2.5f) {
                    score += 50f; // too high up, only when nothing else is around
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
