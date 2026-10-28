using UnityEngine;

// Chests, vases and the stages' props have only a trigger and no moving body, so the physics paired them with the
// sword and the bullets only while those bodies were awake; Freny's sword body (a still child
// rigidbody) is usually asleep, so the swings went through. An awake kinematic body of their own
// makes every sword, bullet and grenade overlap count.
public static class HitBody {

    public static void MakeHittable(GameObject obj) {
        Rigidbody2D body = obj.GetComponent<Rigidbody2D>();
        if (body == null) {
            body = obj.AddComponent<Rigidbody2D>();
        }
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    // the same for a hitbox's own rigidbody (Freny's Sword_Hit_Box)
    public static void KeepAwake(Component hitbox) {
        Rigidbody2D body = hitbox ? hitbox.GetComponent<Rigidbody2D>() : null;
        if (body) {
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            body.WakeUp();
        }
    }
}
