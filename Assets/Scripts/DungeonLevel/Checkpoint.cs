using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public string checkpointName = "Checkpoint";
    public Light beacon;

    bool reached;

    void Start()
    {
        if (beacon) beacon.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (reached) return;
        if (!LevelManager.IsPlayerBody(other) || !LevelManager.Instance) return;

        reached = true;
        Debug.Log("Reached " + checkpointName);
        if (beacon) beacon.enabled = true;
        LevelManager.Instance.SetCheckpoint(transform, checkpointName);
    }
}
