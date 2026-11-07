using UnityEngine;

// An invisible trigger box that does something when the player walks into it.
// We use it for two things:
//  - the Relic in the secret wall niche, which gives you an extra heart
//  - the Level Exit behind the door at the end, which finishes the level
public class LevelPickup : MonoBehaviour
{
    // which of the two jobs this pickup does
    public enum Kind { Relic, Exit }

    public Kind kind;

    // something to delete when it's picked up, e.g. the glowing light
    // that shows where the relic is
    public GameObject removeOnPickup;

    void OnTriggerEnter(Collider other)
    {
        // only the player's body counts
        var lm = LevelManager.Instance;
        if (!lm || !LevelManager.IsPlayerBody(other)) return;

        if (kind == Kind.Relic)
        {
            // +1 max heart, then remove the glow and this pickup so it
            // can't be collected twice
            lm.AddHeart();
            if (removeOnPickup) Destroy(removeOnPickup);
            Destroy(gameObject);
        }
        else if (lm.IsBossDefeated)
        {
            // the exit only works once the Guardian is dead
            // (the door is shut before that anyway)
            lm.CompleteLevel();
        }
    }
}
