using UnityEngine;

// Unity 6 renamed Rigidbody2D.velocity to linearVelocity and its API updater rewrites any
// `rb.velocity` it finds in the project, while the CI compile check still runs on the 2021 API.
// Every script goes through these two calls, so neither side touches the other's name.
public static class Rb2DCompat {

    public static Vector2 Vel(this Rigidbody2D rb) {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    public static void SetVel(this Rigidbody2D rb, Vector2 velocity) {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
    }
}
