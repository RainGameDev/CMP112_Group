using System.Collections.Generic;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public PlayerInputs inputs;

    public int interactionIndex;
    public List<Interactable> interactables;

    void Start()
    {
        inputs = new();
        inputs.Enable();
    }

    void Update()
    {
        if (inputs.Actions.Interact.IsPressed())
        {
            if (interactables[interactionIndex])
            {
                interactables[interactionIndex].Interact();
            }
        }
        for (int i = 0; i < interactables.Capacity; i++)
        {
            if (interactables[i] == null)
            {
                interactables.RemoveAt(i);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<Interactable>() is Interactable interactable)
        {
            interactable.InteractionEntered();
            interactables.Add(interactable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.GetComponent<Interactable>() is Interactable interactable)
        {
            interactable.InteractionExited();
            interactables.Remove(interactable);
        }
    }
}
