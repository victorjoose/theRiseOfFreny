using System.Collections.Generic;
using UnityEngine;

// Plays Freny in the arena. It only produces a PlayerInputFrame, the same thing the keyboard
// produces, so it moves, jumps and attacks with the player's physics and cooldowns. It re-thinks
// every FrenyProgress.AutopilotReactionTime seconds, which the brain upgrades shorten.
//
// Priorities (GDD, "Arena idle"):
// 1. dodge an incoming projectile or a wind-up next to her
// 2. enemy in sword range -> sword (or step back while it recharges)
// 3. grenade at a group or at an enemy on a platform above
// 4. enemy aligned and far -> shot
// 5. low health and a potion -> drink it
// 6. coins and chests nearby when nobody is close
// 7. otherwise walk to the target, climbing platforms by their edges
[RequireComponent(typeof(Player))]
public class ArenaAutopilot : MonoBehaviour, IPlayerInputSource {

    [SerializeField] private float verticalTolerance = 1.2f;   // "same floor" for sword and gun
    [SerializeField] private float jumpToReachHeight = 1.0f;   // target this much higher -> needs a jump
    [SerializeField] private float maxJumpHeight = 2.4f;       // what one jump can climb
    [SerializeField] private float dodgeWindow = 0.55f;        // dodge only when the projectile lands within this many seconds
    [SerializeField] private float dashCloseDistance = 3.5f;   // farther than this (same floor) -> dash in
    [SerializeField] private float pickupRange = 7f;           // coins/chests this far away are worth a detour
    [SerializeField] private float grabRange = 2.5f;           // coins this close are grabbed even mid-fight
    [SerializeField] private float dangerRange = 3f;           // an enemy this close means no detours
    [SerializeField] private float jumpCooldown = 0.5f;        // no hopping: one jump, then land and think
    [SerializeField] private float unreachableMemory = 4f;     // seconds an enemy with no route is ignored
    [SerializeField] private int lowHealth = 1;
    [SerializeField] private float scanInterval = 0.25f;

    private Player player;
    private Dash dash;
    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly List<HexballEffect> projectiles = new List<HexballEffect>();
    private readonly Dictionary<Enemy, float> unreachableUntil = new Dictionary<Enemy, float>();
    private float nextScanTime;
    private float nextDecisionTime;
    private float nextJumpTime;
    private PlayerInputFrame plan;
    private bool planIsFresh;
    private bool planTurnsOnly;      // the move in the plan is a one-frame tap to turn around
    private Enemy currentTarget;     // kept between decisions, so she doesn't flip-flop between two enemies
    private bool retreating;         // decided once per sword cooldown
    private LayerMask ground;

    public string CurrentAction { get; private set; } = "";

    private void Awake() {
        player = GetComponent<Player>();
        dash = GetComponent<Dash>();
    }

    private void Start() {
        ground = player.whatIsGround;
        if (ground.value == 0) {
            ground = LayerMask.GetMask("Ground");
        }
    }

    public PlayerInputFrame ReadInput() {
        if (player == null || player.IsDead) {
            CurrentAction = "";
            return default;
        }

        if (dash == null) {
            dash = GetComponent<Dash>();
        }

        if (Time.time >= nextScanTime) {
            Scan();
            nextScanTime = Time.time + scanInterval;
        }

        if (Time.time >= nextDecisionTime) {
            planTurnsOnly = false;
            plan = Decide();
            if (plan.jumpPressed) {
                if (Time.time < nextJumpTime) {
                    plan.jumpPressed = false; // landed a moment ago: walk this tick, jump on the next
                } else {
                    nextJumpTime = Time.time + jumpCooldown;
                }
            }
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
            frame.grenadePressed = false;
            if (planTurnsOnly) {
                // turning around takes one frame; keeping the stick held walked her past the enemy
                frame.move = 0f;
            }
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
        float myFeet = player.feetPos ? player.feetPos.position.y : me.y;

        // 1. dodge, only when a projectile is about to land (before, any hexball nearby kept her
        // hopping forever instead of fighting)
        HexballEffect threat = IncomingProjectile(me);
        if (threat) {
            float awayFromThreat = Mathf.Sign(me.x - threat.transform.position.x);
            if (Dodge(ref frame, awayFromThreat, "o projétil")) {
                return frame;
            }
        }

        Enemy target = PickTarget(me, myFeet);
        if (target == null) {
            if (GoToPickup(ref frame, me, myFeet, pickupRange * 3f)) {
                return frame;
            }
            CurrentAction = enemies.Count > 0 ? "Sem caminho até os inimigos" : "Esperando inimigos";
            return frame;
        }

        Vector2 targetPos = target.transform.position;
        float dx = targetPos.x - me.x;
        float dy = target.Feet.y - myFeet;
        float towardTarget = Mathf.Sign(dx);
        bool facingTarget = (dx > 0) == player.FacingRight;
        bool sameFloor = Mathf.Abs(dy) <= verticalTolerance;
        float meleeRange = Tuning.AutoMeleeRange.Value * (FrenyProgress.HasGreatSword ? 1.4f : 1f);
        float nearestEnemy = NearestEnemyDistance(me);

        // 1b. an enemy next to her is winding up a hit: get out of the way (a person would)
        Enemy striker = WindingUpNearby(me, myFeet, meleeRange + 0.6f);
        if (striker && (!player.AttackReady || player.Health <= lowHealth)) {
            float away = Mathf.Sign(me.x - striker.transform.position.x);
            if (Dodge(ref frame, away, "o golpe")) {
                return frame;
            }
        }

        // 2. sword
        if (sameFloor && Mathf.Abs(dx) <= meleeRange) {
            if (player.AttackReady) {
                retreating = false;
                if (!facingTarget) {
                    Turn(ref frame, towardTarget);
                } else if (player.IsGrounded) {
                    frame.meleePressed = true;
                    CurrentAction = "Golpe de espada";
                } else {
                    CurrentAction = "Esperando aterrissar";
                }
                return frame;
            }

            // the sword is recharging: decide once whether to step back or hold ground
            if (!retreating && Random.value < Tuning.AutoKeepDistance.Value) {
                retreating = true;
            }
            if (retreating || player.Health <= lowHealth) {
                frame.move = -towardTarget;
                CurrentAction = "Recuando na recarga";
            } else {
                CurrentAction = "Esperando a recarga";
            }
            return frame;
        }
        retreating = false;

        // 3. grenade: a group, or an enemy on a platform above, lobbed with the charge that lands there
        if (FrenyProgress.HasGrenade && player.GrenadeReady && Mathf.Abs(dx) >= 1.5f && Mathf.Abs(dx) <= 8f) {
            bool group = CountEnemiesNear(targetPos, 2.5f) >= 2;
            bool above = dy > jumpToReachHeight;
            float charge = player.GrenadeChargeFor(dx, target.Center.y - (me.y + 0.6f));
            if ((group || above) && charge >= 0f) {
                if (!facingTarget) {
                    Turn(ref frame, towardTarget);
                } else {
                    frame.grenadePressed = true;
                    frame.grenadeCharge = charge;
                    CurrentAction = group ? "Granada no grupo" : "Granada na plataforma";
                }
                return frame;
            }
        }

        // 4. gun
        if (sameFloor && Mathf.Abs(dx) <= Tuning.AutoShootRange.Value && player.AttackReady) {
            if (!facingTarget) {
                Turn(ref frame, towardTarget);
            } else {
                frame.rangedPressed = true;
                CurrentAction = player.UsingShotgun ? "Tiro de escopeta" : "Tiro";
            }
            return frame;
        }

        // 5. potion
        if (FrenyProgress.HasAutoPotion && player.Health <= lowHealth && player.HasPotion) {
            frame.potionPressed = true;
            CurrentAction = "Bebendo poção";
            return frame;
        }

        // 6. loot: a coin within arm's reach is grabbed even mid-fight; farther loot only when nobody is close
        if (nearestEnemy > dangerRange ? GoToPickup(ref frame, me, myFeet, pickupRange) : GoToPickup(ref frame, me, myFeet, grabRange)) {
            return frame;
        }

        // 7. go to the target
        if (sameFloor) {
            bool farForGun = Mathf.Abs(dx) > Tuning.AutoShootRange.Value;
            if ((farForGun || !player.AttackReady) && Mathf.Abs(dx) > dashCloseDistance && player.IsGrounded && dash && dash.CanDash) {
                if (!facingTarget) {
                    Turn(ref frame, towardTarget);
                } else {
                    frame.dashPressed = true;
                    frame.move = towardTarget;
                    CurrentAction = "Dash até o inimigo";
                }
                return frame;
            }
            frame.move = towardTarget;
            CurrentAction = "Indo até o inimigo";
            return frame;
        }

        bool routed = dy > jumpToReachHeight
            ? Climb(ref frame, me, myFeet, target.Feet.y, towardTarget)
            : Descend(ref frame, me, myFeet, towardTarget);
        if (!routed) {
            // no platform edge leads there: forget this enemy for a while and shoot whoever else is around
            unreachableUntil[target] = Time.time + unreachableMemory;
            currentTarget = null;
            CurrentAction = "Sem caminho, trocando de alvo";
        }
        return frame;
    }

    // ---- actions ----

    private void Turn(ref PlayerInputFrame frame, float direction) {
        frame.move = direction;
        planTurnsOnly = true;
        CurrentAction = "Virando";
    }

    private bool Dodge(ref PlayerInputFrame frame, float away, string what) {
        bool facingAway = (away > 0) == player.FacingRight;
        if (FrenyProgress.HasDashDodge && facingAway && dash && dash.CanDash && player.IsGrounded) {
            frame.dashPressed = true;
            CurrentAction = "Esquivando com dash de " + what;
        } else if ((player.IsGrounded && Time.time >= nextJumpTime) || player.CanAirJump) {
            frame.jumpPressed = true;
            frame.jumpHeld = true;
            frame.move = away;
            CurrentAction = "Pulando " + what;
        } else {
            return false;
        }
        return true;
    }

    // Walks to a spot just outside the edge of the platform at `floorY` and jumps from there,
    // never from underneath it (the ceiling blocked the jump and she hopped in place). A platform
    // too high for one jump is reached through the one in between. False when no edge is found.
    private bool Climb(ref PlayerInputFrame frame, Vector2 me, float myFeet, float floorY, float toward) {
        float level = floorY;
        float reach = maxJumpHeight + (FrenyProgress.HasDoubleJump ? 1.5f : 0f);
        while (level - myFeet > reach && level - 2f > myFeet + 0.5f) {
            level -= 2f; // platforms in the arena are about two units apart
        }

        if (!FindJumpSpot(me.x, level, toward, out float standX, out float jumpDir)) {
            return false;
        }

        float toSpot = standX - me.x;
        if (Mathf.Abs(toSpot) <= 0.45f) {
            // at the spot: jump towards the platform, pushing the stick that way
            frame.move = jumpDir;
            if (player.IsGrounded) {
                frame.jumpPressed = true;
            } else if (player.CanAirJump && level - myFeet > maxJumpHeight) {
                frame.jumpPressed = true;
            }
            frame.jumpHeld = true;
            CurrentAction = "Subindo a plataforma";
        } else {
            frame.move = Mathf.Sign(toSpot);
            CurrentAction = "Indo até a beirada";
        }
        return true;
    }

    // walks off the edge of the platform she is standing on, towards the target
    private bool Descend(ref PlayerInputFrame frame, Vector2 me, float myFeet, float toward) {
        if (FindDrop(me.x, myFeet, toward, out float edgeX) || FindDrop(me.x, myFeet, -toward, out edgeX)) {
            float d = edgeX - me.x;
            frame.move = Mathf.Sign(d == 0f ? toward : d);
            CurrentAction = "Descendo";
            return true;
        }
        return false;
    }

    private bool GoToPickup(ref PlayerInputFrame frame, Vector2 me, float myFeet, float range) {
        Vector2 best = Vector2.zero;
        float bestDist = float.MaxValue;
        string what = "";
        foreach (Coin coin in Coin.Active) {
            Consider(coin ? coin.transform : null, me, myFeet, range, ref best, ref bestDist, ref what, "a moeda");
        }
        foreach (ArenaChest chest in ArenaChest.Active) {
            Consider(chest ? chest.transform : null, me, myFeet, range, ref best, ref bestDist, ref what, "o baú");
        }
        if (bestDist == float.MaxValue) {
            return false;
        }

        float dx = best.x - me.x;
        float dy = best.y - myFeet;
        bool routed;
        if (dy > jumpToReachHeight) {
            routed = Climb(ref frame, me, myFeet, best.y, Mathf.Sign(dx));
        } else if (dy < -verticalTolerance) {
            routed = Descend(ref frame, me, myFeet, Mathf.Sign(dx));
        } else {
            if (Mathf.Abs(dx) < 0.15f) {
                return false;
            }
            frame.move = Mathf.Sign(dx);
            routed = true;
        }
        if (routed) {
            CurrentAction = "Pegando " + what;
        }
        return routed;
    }

    private void Consider(Transform item, Vector2 me, float myFeet, float range, ref Vector2 best, ref float bestDist, ref string what, string name) {
        if (item == null) {
            return;
        }
        Vector2 pos = item.position;
        float dist = Mathf.Abs(pos.x - me.x) + 2f * Mathf.Abs(pos.y - myFeet);
        if (dist <= range && dist < bestDist) {
            bestDist = dist;
            best = pos;
            what = name;
        }
    }

    // ---- perception ----

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

    private Enemy WindingUpNearby(Vector2 me, float myFeet, float range) {
        foreach (Enemy enemy in enemies) {
            if (!enemy || !enemy.IsAttacking) {
                continue;
            }
            if (Mathf.Abs(enemy.transform.position.x - me.x) <= range && Mathf.Abs(enemy.Feet.y - myFeet) <= verticalTolerance) {
                return enemy;
            }
        }
        return null;
    }

    private float NearestEnemyDistance(Vector2 me) {
        float nearest = float.MaxValue;
        foreach (Enemy enemy in enemies) {
            if (enemy) {
                nearest = Mathf.Min(nearest, Vector2.Distance(enemy.transform.position, me));
            }
        }
        return nearest;
    }

    private int CountEnemiesNear(Vector2 point, float range) {
        int count = 0;
        foreach (Enemy enemy in enemies) {
            if (enemy && Vector2.Distance(enemy.transform.position, point) <= range) {
                count++;
            }
        }
        return count;
    }

    // enemies on her own floor come first (another platform is expensive to reach). The current
    // target is kept unless another is clearly better, so she doesn't flip-flop between two.
    // Enemies recently found unreachable are skipped. Smart targeting also prefers enemies that
    // are almost dead.
    private Enemy PickTarget(Vector2 me, float myFeet) {
        Enemy best = null;
        float bestScore = float.MaxValue;
        float currentScore = float.MaxValue;

        foreach (Enemy enemy in enemies) {
            if (!enemy || enemy.IsDead) {
                continue;
            }
            if (unreachableUntil.TryGetValue(enemy, out float until) && Time.time < until) {
                continue;
            }
            Vector2 offset = (Vector2)enemy.transform.position - me;
            float score = Mathf.Abs(offset.x) + 4f * Mathf.Abs(enemy.Feet.y - myFeet);

            if (FrenyProgress.HasSmartTarget && enemy is IDamageable damageable) {
                score += 1.5f * damageable.Health;
            }
            if (enemy.Feet.y - myFeet > maxJumpHeight * 2f) {
                score += 50f; // too high up, only when nothing else is around
            }

            if (enemy == currentTarget) {
                currentScore = score;
            }
            if (score < bestScore) {
                bestScore = score;
                best = enemy;
            }
        }

        if (currentTarget != null && !currentTarget.IsDead && currentScore < float.MaxValue && currentScore <= bestScore + 2f) {
            return currentTarget;
        }
        currentTarget = best;
        return best;
    }

    // ---- platform probes (ground layer only) ----

    private bool GroundAt(float x, float floorY) {
        return Physics2D.OverlapBox(new Vector2(x, floorY - 0.3f), new Vector2(0.4f, 0.5f), 0f, ground) != null;
    }

    // Where to stand to jump onto the platform at `floorY`: just outside its nearest edge.
    // Under the platform, the nearest edge in either direction is used; beside it, the edge is
    // the first ground found going towards `direction` (then the other way).
    private bool FindJumpSpot(float fromX, float floorY, float direction, out float standX, out float jumpDir) {
        const float step = 0.5f;
        const float reach = 10f;
        const float outside = 0.8f; // how far from the edge she stands before jumping

        if (GroundAt(fromX, floorY)) {
            // underneath: walk out from under it
            float bestD = float.MaxValue;
            standX = fromX;
            jumpDir = direction;
            for (int pass = 0; pass < 2; pass++) {
                float dir = pass == 0 ? direction : -direction;
                for (float d = step; d <= reach; d += step) {
                    if (!GroundAt(fromX + dir * d, floorY)) {
                        if (d < bestD) {
                            bestD = d;
                            standX = fromX + dir * (d + outside);
                            jumpDir = -dir;
                        }
                        break;
                    }
                }
            }
            return bestD < float.MaxValue;
        }

        for (int pass = 0; pass < 2; pass++) {
            float dir = pass == 0 ? direction : -direction;
            for (float d = step; d <= reach; d += step) {
                if (GroundAt(fromX + dir * d, floorY)) {
                    standX = fromX + dir * Mathf.Max(0f, d - outside);
                    jumpDir = dir;
                    return true;
                }
            }
        }
        standX = fromX;
        jumpDir = direction;
        return false;
    }

    // first x in `direction` where the platform under her ends
    private bool FindDrop(float fromX, float myFeet, float direction, out float edgeX) {
        for (float d = 0.5f; d <= 10f; d += 0.5f) {
            float x = fromX + direction * d;
            if (!GroundAt(x, myFeet)) {
                edgeX = x + direction * 0.6f;
                return true;
            }
        }
        edgeX = fromX;
        return false;
    }
}
