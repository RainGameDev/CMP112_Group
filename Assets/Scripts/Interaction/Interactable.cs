using UnityEngine;

public class Interactable : MonoBehaviour
{
    public GameObject interactionUI;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        interactionUI.SetActive(false);
    }

    public void InteractionEntered()
    {
        if (interactionUI)
            interactionUI.SetActive(true);
    }

    public void InteractionExited()
    {
        if (interactionUI)
            interactionUI.SetActive(false);
    }

    public virtual void Interact() { }
}
