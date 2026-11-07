using UnityEngine;

// This goes on an invisible trigger box at each checkpoint.
// The first time the player walks into it, it becomes the place they respawn
// if they die or fall, and its beacon light switches on so you can see you've
// reached it.
public class Checkpoint : MonoBehaviour
{
    // the name shown on screen when you reach it, e.g. "Checkpoint 2"
    public string checkpointName = "Checkpoint";

    // the light above the checkpoint that turns on once it's been reached
    public Light beacon;

    // remembers if the player has already touched this checkpoint,
    // so it only triggers once
    bool reached;

    void Start()
    {
        // the beacon starts off, it only lights up when you get here
        if (beacon) beacon.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        // ignore it if we've already been reached, if it wasn't the player's
        // body that walked in (enemies and the player's own trigger colliders
        // don't count), or if there's no level manager in the scene
        if (reached || !LevelManager.IsPlayerBody(other) || !LevelManager.Instance) return;

        reached = true;
        if (beacon) beacon.enabled = true;

        // tell the level manager to respawn the player here from now on
        LevelManager.Instance.SetCheckpoint(transform, checkpointName);
    }
}
