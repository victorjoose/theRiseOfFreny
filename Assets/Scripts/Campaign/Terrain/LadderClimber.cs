using UnityEngine;

// Lets Freny climb ladders: on a ladder, up (W, up arrow, stick) or down (S, down arrow) grabs it,
// and she then moves up and down at climbSpeed with no gravity, sideways a little, until she
// jumps, walks off its side or reaches the end. Runs after Player (execution order) so its
// vertical speed wins over the player's fall in the same physics step.
[DefaultExecutionOrder(100)]
public class LadderClimber : MonoBehaviour {

    [SerializeField] private float climbSpeed = 3.2f;
    [SerializeField] private float sideSpeed = 1.5f;
    [SerializeField] private float jumpOffSpeed = 7f;
    [SerializeField] private float topHopSpeed = 5f;

    private Player player;
    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private Ladder ladder;
    private float savedGravity;
    private float vertical;
    private float horizontal;

    public bool Climbing => ladder != null;

    private void Start() {
        player = GetComponent<Player>();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
    }

    private void Update() {
        if (player == null || body == null || player.IsDead || InputBindings.GameplayBlocked) {
            Release();
            return;
        }
        vertical = Input.GetAxisRaw("Vertical");
        horizontal = Input.GetAxisRaw("Horizontal");
        Ladder here = Ladder.At(Middle());

        if (ladder == null) {
            if (here != null && Mathf.Abs(vertical) > 0.5f && !player.IsStunned) {
                Grab(here);
            }
            return;
        }
        if (InputBindings.GetDown(GameAction.Jump)) {
            Release();
            body.SetVel(new Vector2(horizontal * 3f, jumpOffSpeed)); // jumps off the rungs
        } else if (here != ladder) {
            bool overTheTop = vertical > 0.5f && Middle().y > ladder.Area.max.y - 0.1f;
            Release();
            if (overTheTop) {
                body.SetVel(new Vector2(horizontal * sideSpeed, topHopSpeed)); // a step up onto the floor above
            }
        } else if (player.IsStunned) {
            Release();
        }
    }

    private void FixedUpdate() {
        if (ladder == null || body == null) {
            return;
        }
        // centred on the rungs, moving with the input
        float x = Mathf.MoveTowards(body.position.x, ladder.Area.center.x, sideSpeed * Time.fixedDeltaTime);
        body.position = new Vector2(Mathf.Abs(horizontal) > 0.5f ? body.position.x : x, body.position.y);
        body.SetVel(new Vector2(horizontal * sideSpeed, vertical * climbSpeed));
    }

    private void Grab(Ladder target) {
        ladder = target;
        savedGravity = body.gravityScale;
        body.gravityScale = 0f;
        body.SetVel(Vector2.zero);
    }

    private void Release() {
        if (ladder == null) {
            return;
        }
        ladder = null;
        if (body != null) {
            body.gravityScale = savedGravity;
        }
    }

    private Vector2 Middle() {
        if (bodyCollider != null) {
            return bodyCollider.bounds.center;
        }
        return transform.position;
    }

    private void OnDisable() {
        Release();
    }
}
