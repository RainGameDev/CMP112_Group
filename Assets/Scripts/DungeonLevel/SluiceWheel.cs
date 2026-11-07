using System.Collections;
using UnityEngine;

// The wheel on the platform in the Sluice Chamber.
// It's an Interactable like our chests, so the player turns it with the
// normal interact button. Turning it drains the well (so you can get down to
// the boss) and lets the two ambush skeletons out.
public class SluiceWheel : Interactable
{
    // the wheel model, so we can spin it when it's turned
    public Transform wheel;

    // so it can only be turned once
    bool turned;

    // Interactable's own Start hides the interactionUI, but the wheel doesn't
    // have one, so we replace Start with a version that checks first
    // (otherwise it would throw an error)
    void Start()
    {
        if (interactionUI) interactionUI.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        // when the player walks up to it, tell them what to do
        if (!turned && LevelManager.IsPlayerBody(other) && LevelManager.Instance)
            LevelManager.Instance.ShowMessage("Press Interact to turn the sluice wheel", 2.5f);
    }

    public override void Interact()
    {
        if (turned) return;
        turned = true;
        base.Interact();
        InteractionExited();

        // this is the important part: drain the well, wake the boss and the ambush
        if (LevelManager.Instance) LevelManager.Instance.DrainWell();

        StartCoroutine(Spin());
    }

    // spins the wheel model round for 2 seconds so you can see it was turned
    IEnumerator Spin()
    {
        for (float t = 0; t < 2f; t += Time.deltaTime)
        {
            if (wheel) wheel.Rotate(0f, 360f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }
    }
}
