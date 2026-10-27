using UnityEngine;

// Sits on Freny's sprite object in every scene. She fires on input (Player.Shoot), not on an
// animation event, so the clips no longer carry events for this to receive; the component stays
// so those scenes don't end up with a missing script.
public class PlayerAnimationEvent : MonoBehaviour {
}
