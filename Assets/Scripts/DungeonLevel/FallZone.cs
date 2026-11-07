using UnityEngine;

// This goes on an invisible trigger box at the bottom of a pit the player
// can't climb back out of (like the pit in the Crumbling Gallery).
// When the player lands in it, they get sent back up so they aren't stuck.
public class FallZone : MonoBehaviour
{
    // how much damage falling in does (0 = no damage, just sends you back)
    public float damage;

    // where to send the player. If this is left empty they go back to
    // their last checkpoint instead
    public Transform respawnAt;

    void OnTriggerEnter(Collider other)
    {
        // only react to the player's actual body, not enemies or the
        // player's own trigger colliders
        if (LevelManager.IsPlayerBody(other) && LevelManager.Instance)
            LevelManager.Instance.Fall(damage, respawnAt);
    }
}
