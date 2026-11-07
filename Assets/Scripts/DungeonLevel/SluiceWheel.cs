using System.Collections;
using UnityEngine;

public class SluiceWheel : Interactable
{
    public Transform wheel;

    bool turned;

    void Start()
    {
        if (interactionUI) interactionUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!turned && LevelManager.IsPlayerBody(other) && LevelManager.Instance)
            LevelManager.Instance.ShowMessage("Press Interact to turn the sluice wheel", 2.5f);
    }

    public override void Interact()
    {
        if (turned) return;
        turned = true;
        Debug.Log("Turned the sluice wheel");
        base.Interact();
        InteractionExited();

        if (LevelManager.Instance) LevelManager.Instance.DrainWell();
        StartCoroutine(Spin());
    }

    IEnumerator Spin()
    {
        for (float t = 0; t < 2f; t += Time.deltaTime)
        {
            if (wheel) wheel.Rotate(0f, 360f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }
    }
}
